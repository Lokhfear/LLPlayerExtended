using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LLPlayer.Models;
using LLPlayer.Services;
using Microsoft.Win32;

namespace LLPlayer.ViewModels;

/// <summary>
/// Главный ViewModel окна библиотеки обучения.
/// </summary>
public class LearningLibraryViewModel : INotifyPropertyChanged
{
    private readonly LearningItemService _service;
    private readonly ImportExportService _importExport;
    private CancellationTokenSource? _searchDebounce;

    public LearningLibraryViewModel(LearningItemService svc, ImportExportService ie)
    {
        _service = svc;
        _importExport = ie;

        DeleteCommand         = new RelayCommand<LearningItem>(OnDelete);
        ToggleFavoriteCommand = new RelayCommand<LearningItem>(OnToggleFavorite);
        ArchiveCommand        = new RelayCommand<LearningItem>(OnArchive);
        RestoreCommand        = new RelayCommand<LearningItem>(OnRestore);
        OpenVideoCommand      = new RelayCommand<LearningItem>(OnOpenVideo);
        ExportCommand         = new RelayCommand(async _ => await OnExport());
        ImportCommand         = new RelayCommand(async _ => await OnImport());
        ClearFiltersCommand   = new RelayCommand(_ => ClearFilters());
        FavoritesOnlyCommand  = new RelayCommand(_ => ToggleFavoritesOnly());
    }

    // ─── Коллекции ─────────────────────────────────────────────────────────

    private ObservableCollection<LearningItem> _items = new();
    public ObservableCollection<LearningItem> Items
    {
        get => _items;
        private set { _items = value; OnPropertyChanged(); }
    }

    // ─── Счётчики ──────────────────────────────────────────────────────────

    private int _totalCount;
    public int TotalCount { get => _totalCount; set { _totalCount = value; OnPropertyChanged(); } }

    private int _shownCount;
    public int ShownCount { get => _shownCount; set { _shownCount = value; OnPropertyChanged(); } }

    // ─── Фильтры ───────────────────────────────────────────────────────────

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { _searchText = value; OnPropertyChanged(); OnSearchTextChanged(); }
    }

    private bool _showArchived;
    public bool ShowArchived
    {
        get => _showArchived;
        set { _showArchived = value; OnPropertyChanged(); _ = RefreshAsync(); }
    }

    private bool _favoritesOnly;
    public bool FavoritesOnly
    {
        get => _favoritesOnly;
        set { _favoritesOnly = value; OnPropertyChanged(); _ = RefreshAsync(); }
    }

    private ItemType? _filterType;
    public ItemType? FilterType
    {
        get => _filterType;
        set { _filterType = value; OnPropertyChanged(); _ = RefreshAsync(); }
    }

    private LearningStatus? _filterStatus;
    public LearningStatus? FilterStatus
    {
        get => _filterStatus;
        set { _filterStatus = value; OnPropertyChanged(); _ = RefreshAsync(); }
    }

    private bool _hasMediaOnly;
    public bool HasMediaOnly
    {
        get => _hasMediaOnly;
        set { _hasMediaOnly = value; OnPropertyChanged(); _ = RefreshAsync(); }
    }

    private SortMode _sortMode = SortMode.Newest;
    public SortMode SortMode
    {
        get => _sortMode;
        set { _sortMode = value; OnPropertyChanged(); _ = RefreshAsync(); }
    }

    // ─── Состояния UI ──────────────────────────────────────────────────────

    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }

    private bool _isEmpty;
    public bool IsEmpty { get => _isEmpty; set { _isEmpty = value; OnPropertyChanged(); } }

    private string? _statusMessage;
    public string? StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }

    // ─── Команды ───────────────────────────────────────────────────────────

    public ICommand DeleteCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand ArchiveCommand { get; }
    public ICommand RestoreCommand { get; }
    public ICommand OpenVideoCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand ClearFiltersCommand { get; }
    public ICommand FavoritesOnlyCommand { get; }

    // ─── Методы ────────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        var all = await _service.GetAllAsync();
        TotalCount = all.Count;
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        IsLoading = true;
        try
        {
            var filter = BuildFilter();
            var results = await _service.QueryAsync(filter);

            App.Current.Dispatcher.Invoke(() =>
            {
                Items = new ObservableCollection<LearningItem>(results);
                ShownCount = results.Count;
                IsEmpty = results.Count == 0;
            });
        }
        finally { IsLoading = false; }
    }

    private LibraryFilter BuildFilter() => new()
    {
        SearchText    = SearchText,
        ShowArchived  = ShowArchived,
        FavoritesOnly = FavoritesOnly,
        Type          = FilterType,
        Status        = FilterStatus,
        HasMediaOnly  = HasMediaOnly,
        SortBy        = SortMode
    };

    private void OnSearchTextChanged()
    {
        _searchDebounce?.Cancel();
        _searchDebounce = new CancellationTokenSource();
        var token = _searchDebounce.Token;
        Task.Delay(300, token)
            .ContinueWith(t => { if (!t.IsCanceled) _ = RefreshAsync(); },
                TaskScheduler.Default);
    }

    private void ClearFilters()
    {
        _searchText     = string.Empty; OnPropertyChanged(nameof(SearchText));
        _favoritesOnly  = false;        OnPropertyChanged(nameof(FavoritesOnly));
        _filterType     = null;         OnPropertyChanged(nameof(FilterType));
        _filterStatus   = null;         OnPropertyChanged(nameof(FilterStatus));
        _hasMediaOnly   = false;        OnPropertyChanged(nameof(HasMediaOnly));
        _showArchived   = false;        OnPropertyChanged(nameof(ShowArchived));
        _ = RefreshAsync();
    }

    private void ToggleFavoritesOnly()
    {
        _favoritesOnly = !_favoritesOnly;
        OnPropertyChanged(nameof(FavoritesOnly));
        _showArchived = false;
        OnPropertyChanged(nameof(ShowArchived));
        _ = RefreshAsync();
    }

    private async void OnDelete(LearningItem? item)
    {
        if (item == null) return;
        var r = MessageBox.Show($"Delete \"{item.Text}\"?", "Confirm",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r != MessageBoxResult.Yes) return;
        await _service.RemoveAsync(item.Id);
        Items.Remove(item);
        TotalCount--;
        ShownCount--;
        IsEmpty = Items.Count == 0;
    }

    private async void OnToggleFavorite(LearningItem? item)
    {
        if (item == null) return;
        item.IsFavorite = !item.IsFavorite;
        await _service.UpdateAsync(item);
        // Обновить только этот элемент в списке (без полной перезагрузки)
        var idx = Items.IndexOf(item);
        if (idx >= 0) { Items.RemoveAt(idx); Items.Insert(idx, item); }
    }

    private async void OnArchive(LearningItem? item)
    {
        if (item == null) return;
        item.Status = LearningStatus.Archived;
        await _service.UpdateAsync(item);
        Items.Remove(item);
        ShownCount--;
        IsEmpty = Items.Count == 0;
    }

    private async void OnRestore(LearningItem? item)
    {
        if (item == null) return;
        item.Status = LearningStatus.New;
        await _service.UpdateAsync(item);
        await RefreshAsync();
        var all = await _service.GetAllAsync();
        TotalCount = all.Count;
    }

    private void OnOpenVideo(LearningItem? item)
    {
        var media = item?.Media;
        if (media?.HasMedia != true) return;

        if (!File.Exists(media.FilePath))
        {
            MessageBox.Show(
                $"Video file not found:\n{media.FilePath}",
                "File Not Found",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // Открыть видео в LLPlayer и перейти к timestamp
        // Интеграция с FlyleafLib Player
        try
        {
            // Вариант: передать путь + timestamp через аргументы
            // MainViewModel.OpenFile(media.FilePath, media.TimestampSeconds);

            // Временно: просто открыть файл
            Process.Start(new ProcessStartInfo(media.FilePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Cannot open video: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task OnExport()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            FileName = $"llplayer_dictionary_{DateTime.Now:yyyyMMdd}.json",
            Title = "Export dictionary"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            await _importExport.ExportAsync(dlg.FileName);
            StatusMessage = $"✓ Exported to {Path.GetFileName(dlg.FileName)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Export Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task OnImport()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            Title = "Import dictionary"
        };
        if (dlg.ShowDialog() != true) return;

        // Спросить режим
        var overwrite = MessageBox.Show(
            "Overwrite existing items with the same text?\n\n" +
            "Yes = Overwrite\nNo = Skip duplicates",
            "Import Mode",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        var mode = overwrite == MessageBoxResult.Yes
            ? ImportMode.Overwrite
            : ImportMode.Skip;

        var result = await _importExport.ImportAsync(dlg.FileName, mode);

        if (!result.Success)
        {
            MessageBox.Show($"Import failed:\n{result.Error}", "Import Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        MessageBox.Show(result.Summary, "Import Complete",
            MessageBoxButton.OK, MessageBoxImage.Information);

        await RefreshAsync();
        var all = await _service.GetAllAsync();
        TotalCount = all.Count;
    }

    // ─── INotifyPropertyChanged ────────────────────────────────────────────
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
