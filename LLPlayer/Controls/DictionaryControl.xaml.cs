using System.Windows;
using System.Windows.Controls;

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

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        // Toggle pin state - this will be handled by the parent window
        // The actual pinning logic should be implemented in the window that hosts this control
        var window = Window.GetWindow(this);
        if (window != null)
        {
            // Toggle between floating and pinned mode
            // This is a placeholder - actual implementation depends on how the window manages pinning
            MessageBox.Show("Pin functionality: In full implementation, this would toggle between floating window and left-side panel that hides during fullscreen.", "Pin Mode", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
