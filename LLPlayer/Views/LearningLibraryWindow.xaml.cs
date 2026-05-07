using System.Windows;

namespace LLPlayer.Views;

/// <summary>
/// Learning Library Window - Main UI for managing learning items
/// </summary>
public partial class LearningLibraryWindow : Window
{
    public LearningLibraryWindow(ViewModels.LearningLibraryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // Handle closing - just hide instead of close (singleton behavior)
        Closing += (s, e) =>
        {
            e.Cancel = true;
            Hide();
        };
    }
}
