using System.Windows;
using System.Windows.Input;

namespace LLPlayer.Views;

/// <summary>
/// Окно библиотеки обучения.
/// </summary>
public partial class LearningLibraryWindow : Window
{
    public LearningLibraryWindow(ViewModels.LearningLibraryViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Разрешить перетаскивание окна за titlebar
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}
