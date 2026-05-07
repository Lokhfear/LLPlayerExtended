using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using FlyleafLib;
using FlyleafLib.MediaPlayer;
using FlyleafLib.MediaPlayer.Translation;
using FlyleafLib.MediaPlayer.Translation.Services;
using LLPlayer.Extensions;
using LLPlayer.Models;
using LLPlayer.Services;

namespace LLPlayer.Controls;

public partial class WordPopup : UserControl, INotifyPropertyChanged
{
    public FlyleafManager FL { get; }
    private ITranslateService? _translateService;
    private readonly TranslateServiceFactory _translateServiceFactory;
    private PDICSender? _pdicSender;
    private readonly Lock _locker = new();

    private string _clickedWords = string.Empty;
    private string _clickedText = string.Empty;

    private CancellationTokenSource? _cts;
    private readonly Dictionary<string, string> _translateCache = new();
    private string? _lastSearchActionUrl;

    public WordPopup()
    {
        InitializeComponent();

        FL = ((App)Application.Current).Container.Resolve<FlyleafManager>();

        FL.Player.PropertyChanged += Player_OnPropertyChanged;

        _translateServiceFactory = new TranslateServiceFactory(FL.PlayerConfig.Subtitles);

        FL.PlayerConfig.Subtitles.PropertyChanged += SubtitlesOnPropertyChanged;
        FL.PlayerConfig.Subtitles.TranslateChatConfig.PropertyChanged += ChatConfigOnPropertyChanged;
        FL.Player.SubtitlesManager[0].PropertyChanged += SubManagerOnPropertyChanged;
        FL.Player.SubtitlesManager[1].PropertyChanged += SubManagerOnPropertyChanged;
        FL.Config.Subs.PropertyChanged += SubsOnPropertyChanged;

        InitializeContextMenu();
    }

    public bool IsLoading { get; set => Set(ref field, value); }

    public bool IsOpen { get; set => Set(ref field, value); }

    public UIElement? PopupPlacementTarget { get; set => Set(ref field, value); }

    public double PopupHorizontalOffset { get; set => Set(ref field, value); }

    public double PopupVerticalOffset { get; set => Set(ref field, value); }

    public ContextMenu? PopupContextMenu { get; set => Set(ref field, value); }
    public ContextMenu? WordContextMenu { get; set => Set(ref field, value); }

    public bool IsSidebar { get; set; }

    private void SubtitlesOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Config.SubtitlesConfig.TranslateWordServiceType):
            case nameof(Config.SubtitlesConfig.TranslateTargetLanguage):
            case nameof(Config.SubtitlesConfig.LanguageFallbackPrimary):
                // Apply translating settings changes
                Clear();
                break;
        }
    }

    private void ChatConfigOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Apply translating settings changes
        Clear();
    }

    private void SubManagerOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SubManager.Language))
        {
            // Apply source language changes
            Clear();
        }
    }

    private void SubsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppConfigSubs.WordMenuActions))
        {
            // Apply word action settings changes
            InitializeContextMenu();
        }
    }

    private void Clear()
    {
        _translateService?.Dispose();
        _translateService = null;
        // clear cache
        _translateCache.Clear();
    }

    private void InitializeContextMenu()
    {
        var contextMenuStyle = (Style)FindResource("FlyleafContextMenu");

        ContextMenu popupMenu = new()
        {
            Placement = PlacementMode.Mouse,
            Style = contextMenuStyle
        };

        SetupContextMenu(popupMenu);
        PopupContextMenu = popupMenu;

        ContextMenu wordMenu = new()
        {
            Placement = PlacementMode.Mouse,
            Style = contextMenuStyle
        };

        SetupContextMenu(wordMenu);
        WordContextMenu = wordMenu;
    }

    private void SetupContextMenu(ContextMenu contextMenu)
    {
        IEnumerable<IMenuAction> actions = FL.Config.Subs.WordMenuActions.Where(a => a.IsEnabled);
        foreach (IMenuAction action in actions)
        {
            MenuItem menuItem = new() { Header = action.Title };

            // Initialize default action at the top
            // TODO: L: want to make the default action bold.
            if (_lastSearchActionUrl == null && action is SearchMenuAction sa)
            {
                // Only word search available
                if (sa.Url.Contains("%w") || sa.Url.Contains("%lw"))
                {
                    _lastSearchActionUrl = sa.Url;
                }
            }

            menuItem.Click += (o, args) =>
            {
                if (action is SearchMenuAction searchAction)
                {
                    // Only word search available
                    if (searchAction.Url.Contains("%w") || searchAction.Url.Contains("%lw"))
                    {
                        _lastSearchActionUrl = searchAction.Url;
                    }

                    OpenWeb(searchAction.Url, _clickedWords, _clickedText);
                }
                else if (action is ClipboardMenuAction clipboardAction)
                {
                    CopyToClipboard(_clickedWords, clipboardAction.ToLower);
                }
                else if (action is ClipboardAllMenuAction clipboardAllAction)
                {
                    CopyToClipboard(_clickedText, clipboardAllAction.ToLower);
                }
            };
            contextMenu.Items.Add(menuItem);
        }

        // Add separator and "Add to Dictionary" item
        if (actions.Any())
        {
            contextMenu.Items.Add(new Separator());
        }

        var addToDictItem = new MenuItem { Header = "📖 Add to Dictionary" };
        addToDictItem.Click += async (o, args) =>
        {
            await AddWordToDictionaryAsync(_clickedWords, _clickedText);
        };
        contextMenu.Items.Add(addToDictItem);
    }

    // Click on video screen to close pop-up
    private void Player_OnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(FL.Player.Status) && FL.Player.Status == Status.Playing)
        {
            IsOpen = false;
        }
    }

    private async ValueTask<string> TranslateWithCache(string text, WordClickedEventArgs e, CancellationToken token)
    {
        // already translated by translator
        if (e.IsTranslated)
        {
            return text;
        }

        var srcLang = FL.Player.SubtitlesManager[e.SubIndex].Language;
        var targetLang = FL.PlayerConfig.Subtitles.TranslateTargetLanguage;

        // Not set language or same language
        if (srcLang == null || srcLang.ISO6391 == targetLang.ToISO6391())
        {
            return text;
        }

        string lower = text.ToLower();
        if (_translateCache.TryGetValue(lower, out var cache))
        {
            return cache;
        }

        if (_translateService == null)
        {
            try
            {
                var service = _translateServiceFactory.GetService(FL.PlayerConfig.Subtitles.TranslateWordServiceType, true);
                service.Initialize(srcLang, targetLang);
                _translateService = service;
            }
            catch (TranslationConfigException ex)
            {
                Clear();
                ErrorDialogHelper.ShowKnownErrorPopup(ex.Message, KnownErrorType.Configuration);

                return text;
            }
        }

        try
        {
            string result = await _translateService.TranslateAsync(text, token);
            _translateCache.TryAdd(lower, result);

            return result;
        }
        catch (TranslationException ex)
        {
            ErrorDialogHelper.ShowUnknownErrorPopup(ex.Message, UnknownErrorType.Translation, ex);

            return text;
        }
    }

    public async Task OnWordClicked(WordClickedEventArgs e)
    {
        _clickedWords = e.Words;
        _clickedText = e.Text;

        if (FL.Player.Status == Status.Playing)
        {
            FL.Player.Pause();
        }

        if (e.Mouse is MouseClick.Left or MouseClick.Middle)
        {
            switch (FL.Config.Subs.WordClickActionMethod)
            {
                case WordClickAction.Clipboard:
                    CopyToClipboard(e.Words);
                    break;

                case WordClickAction.ClipboardAll:
                    CopyToClipboard(e.Text);
                    break;

                default:
                    await Popup(e);
                    break;
            }
        }
        else if (e.Mouse == MouseClick.Right)
        {
            WordContextMenu!.IsOpen = true;
        }
    }

    private async Task Popup(WordClickedEventArgs e)
    {
        if (FL.Config.Subs.WordCopyOnSelected)
        {
            CopyToClipboard(e.Words);
        }

        if (FL.Config.Subs.WordClickActionMethod == WordClickAction.PDIC && e.IsWord)
        {
            if (_pdicSender == null)
            {
                // Initialize PDIC lazily
                lock (_locker)
                {
                    _pdicSender ??= ((App)Application.Current).Container.Resolve<PDICSender>();
                }
            }

            _ = _pdicSender.SendWithPipe(e.Text, e.WordOffset + 1);
            return;
        }

        if (_cts != null)
        {
            // Canceled if running ahead
            _cts.Cancel();
            _cts.Dispose();
        }

        _cts = new CancellationTokenSource();

        string source = e.Words;

        IsLoading = true;

        SourceText.Text = source;
        TranslationText.Text = "";

        if (IsSidebar && e.Sender is SelectableTextBox)
        {
            var listBoxItem = UIHelper.FindParent<ListBoxItem>(e.Sender);
            if (listBoxItem != null)
            {
                PopupPlacementTarget = listBoxItem;
            }
        }

        if (FL.Config.Subs.WordLastSearchOnSelected)
        {
            if (Keyboard.Modifiers == FL.Config.Subs.WordLastSearchOnSelectedModifier)
            {
                if (_lastSearchActionUrl != null)
                {
                    OpenWeb(_lastSearchActionUrl, source);
                }
            }
        }

        IsOpen = true;

        await UpdatePosition();

        try
        {
            string result = await TranslateWithCache(source, e, _cts.Token);
            TranslationText.Text = result;
            IsLoading = false;
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await UpdatePosition();

        return;

        async Task UpdatePosition()
        {
            // ActualWidth is updated asynchronously, so it needs to be offloaded in the Dispatcher.
            await Dispatcher.BeginInvoke(() =>
            {
                if (IsSidebar && PopupPlacementTarget != null)
                {
                    // for sidebar
                    PopupVerticalOffset = (((ListBoxItem)PopupPlacementTarget).ActualHeight - ActualHeight) / 2;
                    PopupHorizontalOffset = -ActualWidth - 10;
                }
                else
                {
                    // for subtitle
                    PopupHorizontalOffset = e.WordsX + ((e.WordsWidth - ActualWidth) / 2);
                    PopupVerticalOffset = -ActualHeight;
                }

            }, DispatcherPriority.Background);
        }
    }

    private void CloseButton_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        IsOpen = false;
    }

    private void SourceText_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_lastSearchActionUrl != null)
        {
            OpenWeb(_lastSearchActionUrl, SourceText.Text);
        }
    }

    private static void CopyToClipboard(string text, bool toLower = false)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (toLower)
        {
            text = text.ToLower();
        }

        // copy word
        try
        {
            // slow (10ms)
            //Clipboard.SetText(text);

            WindowsClipboard.SetText(text);
        }
        catch
        {
            // ignored
        }
    }

    private static void OpenWeb(string url, string words, string sentence = "")
    {
        if (url.Contains("%lw"))
        {
            url = url.Replace("%lw", Uri.EscapeDataString(words.ToLower()));
        }

        if (url.Contains("%w"))
        {
            url = url.Replace("%w", Uri.EscapeDataString(words));
        }

        if (url.Contains("%s"))
        {
            url = url.Replace("%s", Uri.EscapeDataString(sentence));
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    /// <summary>
    /// Добавить слово/фразу/предложение в библиотеку обучения.
    /// </summary>
    private async Task AddWordToDictionaryAsync(string rawWord, string sentence)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[Dictionary] Adding: raw='{rawWord}', sentence='{sentence}'");

            // 1. Нормализация текста
            var text = NormalizeWord(rawWord);
            if (string.IsNullOrWhiteSpace(text))
            {
                System.Diagnostics.Debug.WriteLine("[Dictionary] Skipped: empty text after normalization");
                return;
            }

            // 2. Определить тип автоматически по количеству слов
            var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            var type = wordCount == 1 ? ItemType.Word
                     : wordCount <= 5 ? ItemType.Phrase
                     : ItemType.Sentence;

            // 3. Получить timestamp и информацию о видео
            var timestamp = FL.Player?.CurTime != null
                ? TimeSpan.FromTicks(FL.Player.CurTime).TotalSeconds
                : (double?)null;

            var videoPath = FL.Player?.Playlist?.Selected?.Url;
            var videoTitle = !string.IsNullOrEmpty(videoPath)
                ? System.IO.Path.GetFileNameWithoutExtension(videoPath)
                : null;

            // 4. Создать запись с медиа-контекстом
            var item = new Models.LearningItem
            {
                Type = type,
                Text = text,
                ContextSentence = sentence,
                Media = new Models.MediaContext
                {
                    VideoTitle = videoTitle,
                    FilePath = videoPath,
                    TimestampSeconds = timestamp
                }
            };

            System.Diagnostics.Debug.WriteLine($"[Dictionary] Item created: Type={type}, Text='{text}', Video='{videoTitle}'");

            // 5. Сохранить немедленно (UI не блокируется)
            var (savedItem, isNew) = await App.LearningItemService.AddAsync(item);
            
            System.Diagnostics.Debug.WriteLine($"[Dictionary] Save result: isNew={isNew}, Id={savedItem.Id}");

            if (!isNew)
            {
                System.Diagnostics.Debug.WriteLine("[Dictionary] Duplicate detected - item already exists");
                return; // уже было в библиотеке
            }

            // Показать уведомление об успешном добавлении (через статус в VM библиотеки)
            System.Diagnostics.Debug.WriteLine($"[Dictionary] ✓ Successfully added '{text}' to dictionary");

            // 6. Асинхронный перевод — не ждём
            _ = Task.Run(async () =>
            {
                try
                {
                    // Перевод текста (слова/фразы)
                    var translation = await TranslateWordAsync(text);

                    // Перевод предложения (если есть)
                    string? sentenceTranslation = null;
                    if (!string.IsNullOrWhiteSpace(sentence))
                        sentenceTranslation = await TranslateSentenceAsync(sentence);

                    savedItem.Translation = translation;
                    savedItem.ContextSentenceTranslation = sentenceTranslation;

                    await App.LearningItemService.UpdateAsync(savedItem);
                    
                    System.Diagnostics.Debug.WriteLine($"[Dictionary] Translation completed: '{translation}'");
                }
                catch (Exception ex)
                {
                    // Ошибка перевода — запись остаётся без перевода, не критично
                    System.Diagnostics.Debug.WriteLine($"[Dictionary] Translation failed: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Dictionary] ERROR: {ex}");
            ErrorDialogHelper.ShowUnknownErrorPopup($"Failed to add to dictionary: {ex.Message}", "Dictionary", ex);
        }
    }

    /// <summary>
    /// Нормализация: lowercase + удалить пунктуацию по краям.
    /// </summary>
    private static string NormalizeWord(string word)
    {
        var chars = word.Trim().ToLowerInvariant();
        return chars.Trim('.', ',', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']', '`', '«', '»');
    }

    /// <summary>
    /// Перевод слова — вызов существующего механизма перевода проекта.
    /// </summary>
    private async Task<string?> TranslateWordAsync(string word)
    {
        try
        {
            var srcLang = FL.Player.SubtitlesManager[0].Language;
            var targetLang = FL.PlayerConfig.Subtitles.TranslateTargetLanguage;

            if (srcLang == null || srcLang.ISO6391 == targetLang.ToISO6391())
                return null;

            if (_translateService == null)
            {
                var service = _translateServiceFactory.GetService(FL.PlayerConfig.Subtitles.TranslateWordServiceType, true);
                service.Initialize(srcLang, targetLang);
                _translateService = service;
            }

            return await _translateService.TranslateAsync(word, CancellationToken.None);
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> TranslateSentenceAsync(string sentence)
    {
        try
        {
            var srcLang = FL.Player.SubtitlesManager[0].Language;
            var targetLang = FL.PlayerConfig.Subtitles.TranslateTargetLanguage;

            if (srcLang == null || srcLang.ISO6391 == targetLang.ToISO6391())
                return null;

            if (_translateService == null)
            {
                var service = _translateServiceFactory.GetService(FL.PlayerConfig.Subtitles.TranslateWordServiceType, true);
                service.Initialize(srcLang, targetLang);
                _translateService = service;
            }

            return await _translateService.TranslateAsync(sentence, CancellationToken.None);
        }
        catch
        {
            return null;
        }
    }

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    #endregion
}
