using System.Windows;
using System.Windows.Controls;

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
        
        // Subscribe to expand/collapse all events
        viewModel.ExpandAllRequested += OnExpandAllRequested;
        
        // Handle closing - just hide instead of close (singleton behavior)
        Closing += (s, e) =>
        {
            e.Cancel = true;
            Hide();
        };
    }

    private void OnExpandAllRequested(bool expand)
    {
        // Find the ItemsControl and iterate through all LearningItemCard controls
        if (FindName("ItemsControl") is not ItemsControl itemsControl)
        {
            // Try to find it in the visual tree
            itemsControl = FindVisualChild<ItemsControl>(this);
        }

        if (itemsControl == null) return;

        // Force container generation and update all cards
        itemsControl.UpdateLayout();

        foreach (var item in itemsControl.Items)
        {
            var container = itemsControl.ItemContainerGenerator.ContainerFromItem(item);
            if (container == null) continue;

            var cards = FindVisualChildren<Controls.LearningItemCard>(container);
            foreach (var card in cards)
            {
                card.IsExpanded = expand;
            }
        }
    }

    private static T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
            if (child is T t) return t;
            var childOfChild = FindVisualChild<T>(child);
            if (childOfChild != null) return childOfChild;
        }
        return null;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject obj) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
            if (child is T t) yield return t;
            foreach (var childOfChild in FindVisualChildren<T>(child))
                yield return childOfChild;
        }
    }
}
