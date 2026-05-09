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
}
