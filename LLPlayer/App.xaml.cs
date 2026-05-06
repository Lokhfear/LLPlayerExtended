using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using FlyleafLib;
using FlyleafLib.MediaPlayer;
using LLPlayer.Extensions;
using LLPlayer.Services;
using LLPlayer.Views;

namespace LLPlayer;

public partial class App : PrismApplication
{
    public static string Name => "LLPlayer";
    public static string? CmdUrl { get; private set; } = null;
    public static string PlayerConfigPath { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LLPlayer.PlayerConfig.json");
    public static string EngineConfigPath { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LLPlayer.Engine.json");
    public static string AppConfigPath { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LLPlayer.Config.json");
    public static string CrashLogPath { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");

    private readonly LogHandler Log;

    // Dictionary window singleton
    private static Window? _dictionaryWindow;

    public App()
    {
        Log = new LogHandler("[App] [MainApp       ] ");
    }

    /// <summary>
    /// Открыть/показать окно словаря (синглтон).
    /// </summary>
    public static void ShowDictionaryWindow()
    {
        if (_dictionaryWindow == null)
        {
            var service = ((App)Current).Container.Resolve<DictionaryService>();
            var vm = new ViewModels.DictionaryViewModel(service);
            
            _dictionaryWindow = new Window
            {
                Title = "Dictionary",
                Width = 720,
                Height = 550,
                MinWidth = 500,
                MinHeight = 350,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = (Brush?)Application.Current.FindResource("MaterialDesignPaper"),
                Foreground = (Brush?)Application.Current.FindResource("MaterialDesignBody"),
                ResizeMode = ResizeMode.CanResize,
                ShowInTaskbar = false,
                Content = new Controls.DictionaryControl { DataContext = vm }
            };

            // Окно не закрывается — только скрывается
            _dictionaryWindow.Closing += (s, e) =>
            {
                e.Cancel = true;
                _dictionaryWindow?.Hide();
            };
        }
        
        _dictionaryWindow.Owner = Current.MainWindow;
        _dictionaryWindow.Show();
        _dictionaryWindow.Activate();
    }

    static App()
    {
        // Set thread culture to English and error messages to English
        Utils.SaveOriginalCulture();

        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry
            .Register<Player>(FlyleafLoader.CreateFlyleafPlayer)
            .RegisterSingleton<FlyleafManager>()
            .RegisterSingleton<IDialogService, ExtendedDialogService>()
            .RegisterSingleton<DictionaryService>();

        containerRegistry.RegisterDialogWindow<MyDialogWindow>();

        containerRegistry.RegisterDialog<SettingsDialog>();
        containerRegistry.RegisterDialog<SelectLanguageDialog>();
        containerRegistry.RegisterDialog<SubtitlesDownloaderDialog>();
        containerRegistry.RegisterDialog<SubtitlesExportDialog>();
        containerRegistry.RegisterDialog<CheatSheetDialog>();
        containerRegistry.RegisterDialog<WhisperModelDownloadDialog>();
        containerRegistry.RegisterDialog<WhisperEngineDownloadDialog>();
        containerRegistry.RegisterDialog<TesseractDownloadDialog>();
        containerRegistry.RegisterDialog<ErrorDialog>();
    }

    protected override Window CreateShell()
    {
        return Container.Resolve<MainWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        if (e.Args.Length == 1)
        {
            CmdUrl = e.Args[0];
        }

        // TODO: L: customizable?
        // Ensures that we have enough worker threads to avoid the UI from freezing or not updating on time
        ThreadPool.GetMinThreads(out int workers, out int ports);
        ThreadPool.SetMinThreads(workers + 6, ports + 6);

        // Start flyleaf engine
        FlyleafLoader.StartEngine();

        base.OnStartup(e);
    }

    private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Ignore WPF Clipboard exception
        if (e.Exception is COMException { ErrorCode: -2147221040 })
        {
            e.Handled = true;
        }

        if (!e.Handled)
        {
            Log.Error($"Unknown error occurred in App: {e.Exception}");
            Logger.ForceFlush();

            ErrorDialogHelper.ShowUnknownErrorPopup($"Unhandled Exception: {e.Exception.Message}", "Global", e.Exception);
            e.Handled = true;
        }
    }

    #region App Version
    public static string OSArchitecture => RuntimeInformation.OSArchitecture.ToString().ToLowerFirstChar();
    public static string ProcessArchitecture => RuntimeInformation.ProcessArchitecture.ToString().ToLowerFirstChar();

    private static string? _version;
    public static string Version
    {
        get
        {
            if (_version == null)
            {
                (_version, _commitHash) = GetVersion();
            }

            return _version;
        }
    }

    private static string? _commitHash;
    public static string CommitHash
    {
        get
        {
            if (_commitHash == null)
            {
                (_version, _commitHash) = GetVersion();
            }

            return _commitHash;
        }
    }

    private static (string version, string commitHash) GetVersion()
    {
        string exeLocation = Process.GetCurrentProcess().MainModule!.FileName;
        FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(exeLocation);

        Guards.ThrowIfNull(fvi.ProductVersion);

        var version = fvi.ProductVersion.Split("+");
        if (version.Length != 2)
        {
            throw new InvalidOperationException($"ProductVersion is invalid: {fvi.ProductVersion}");
        }

        return (version[0], version[1]);
    }
    #endregion
}
