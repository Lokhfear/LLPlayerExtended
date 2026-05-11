using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LLPlayer.Controls;

public partial class DictionaryControl : UserControl
{
    public DictionaryControl()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // Find the parent window and close it
        var window = Window.GetWindow(this);
        if (window != null)
        {
            window.Close();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        // Find the parent window and minimize it
        var window = Window.GetWindow(this);
        if (window != null)
        {
            window.WindowState = WindowState.Minimized;
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Find the parent window and initiate drag
        var window = Window.GetWindow(this);
        if (window != null && window.WindowState != WindowState.Maximized)
        {
            window.DragMove();
        }
    }
}
