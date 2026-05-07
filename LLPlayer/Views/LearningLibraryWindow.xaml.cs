using System.Windows;

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
}
