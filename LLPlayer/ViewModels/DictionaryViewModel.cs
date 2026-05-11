using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LLPlayer.Models;
using LLPlayer.Services;

namespace LLPlayer.ViewModels;

public class DictionaryViewModel : INotifyPropertyChanged
{
    private readonly DictionaryService _service;

    // Debounce для поиска
    private CancellationTokenSource? _searchCts;

    public DictionaryViewModel(DictionaryService service)
    {
        _service = service;
        DeleteCommand = new RelayCommand<WordEntry>(OnDelete);
        RefreshCommand = new RelayCommand(async _ => await LoadEntriesAsync());
        PlayAtTimestampCommand = new RelayCommand<WordEntry>(OnPlayAtTimestamp);
    }

    // ─── Свойства ─────────────────────────────────────────────────────────────

    private ObservableCollection<WordEntry> _entries = new();
    public ObservableCollection<WordEntry> Entries
    {
        get => _entries;
        private set { _entries = value; OnPropertyChanged(); }
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged();
            OnSearchChanged();
        }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        set { _totalCount = value; OnPropertyChanged(); }
    }

    private bool _isPinned;
    public bool IsPinned
    {
        get => _isPinned;
        set { _isPinned = value; OnPropertyChanged(); }
    }

    private string _sortMode = "Newest";
    public string SortMode
    {
        get => _sortMode;
        set 
        { 
            _sortMode = value; 
            OnPropertyChanged();
            OnSortModeChanged();
        }
    }

    // ─── Команды ──────────────────────────────────────────────────────────────

    public ICommand DeleteCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand PlayAtTimestampCommand { get; }

    // ─── Методы ───────────────────────────────────────────────────────────────

    public async Task LoadEntriesAsync()
    {
        IsLoading = true;
        try
        {
            var all = await _service.ListAsync();
            TotalCount = all.Count;

            // Apply search filter
            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? all
                : all.Where(e => e.Word.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            // Apply sort
            filtered = SortMode switch
            {
                "Alphabetical" => filtered.OrderBy(e => e.Word),
                _ => filtered.OrderByDescending(e => e.CreatedAtDateTime) // Newest by default
            };

            Entries = new ObservableCollection<WordEntry>(filtered);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnSearchChanged()
    {
        // Debounce 300 мс
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        Task.Delay(300, token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
                App.Current.Dispatcher.Invoke(async () => await LoadEntriesAsync());
        }, TaskScheduler.Default);
    }

    private void OnSortModeChanged()
    {
        // Reload entries with new sort order
        _ = LoadEntriesAsync();
    }

    private async void OnDelete(WordEntry? entry)
    {
        if (entry == null) return;
        
        // Show confirmation dialog
        var result = MessageBox.Show(
            $"Are you sure you want to delete \"{entry.Word}\"?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);
        
        if (result != MessageBoxResult.Yes) return;
        
        await _service.RemoveAsync(entry.Id);
        Entries.Remove(entry);
        TotalCount--;
    }

    private void OnPlayAtTimestamp(WordEntry? entry)
    {
        if (entry?.Timestamp == null || string.IsNullOrEmpty(entry.VideoId)) return;
        
        // Попытка получить доступ к плееру через FlyleafManager
        try
        {
            var flManager = ((App)App.Current).Container.Resolve<FlyleafManager>();
            if (flManager?.Player != null)
            {
                // Конвертируем секунды в тики (1 секунда = 10,000,000 тиков)
                long ticks = (long)(entry.Timestamp.Value * 10_000_000);
                flManager.Player.CurTime = ticks;
                return;
            }
        }
        catch
        {
            // Игнорируем ошибки доступа к плееру
        }
        
        // Если не удалось выполнить seek — показываем информационное сообщение
        MessageBox.Show(
            $"Would play video at {TimeSpan.FromSeconds(entry.Timestamp.Value):hh\\:mm\\:ss}\n\nIntegration with main player needed.",
            "Play at Timestamp",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    // ─── INotifyPropertyChanged ───────────────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Простая реализация ICommand
/// </summary>
public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Predicate<T?>? _canExecute;

    public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute == null || _canExecute((T?)parameter);
    public void Execute(object? parameter) => _execute((T?)parameter);
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
    public void Execute(object? parameter) => _execute(parameter);
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
