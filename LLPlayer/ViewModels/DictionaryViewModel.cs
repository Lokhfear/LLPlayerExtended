using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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

    // ─── Команды ──────────────────────────────────────────────────────────────

    public ICommand DeleteCommand { get; }
    public ICommand RefreshCommand { get; }

    // ─── Методы ───────────────────────────────────────────────────────────────

    public async Task LoadEntriesAsync()
    {
        IsLoading = true;
        try
        {
            var all = await _service.ListAsync();
            TotalCount = all.Count;

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? all
                : all.Where(e => e.Word.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

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

    private async void OnDelete(WordEntry? entry)
    {
        if (entry == null) return;
        await _service.RemoveAsync(entry.Id);
        Entries.Remove(entry);
        TotalCount--;
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
