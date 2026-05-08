using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LLPlayer.Controls;

/// <summary>
/// Learning Item Card control - displays a single learning item with actions
/// </summary>
public partial class LearningItemCard : UserControl
{
    private bool _isExpanded = false;

    public LearningItemCard()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets whether this card is expanded
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            _isExpanded = value;
            UpdateExpandState();
        }
    }

    private void ExpandToggleBtn_Click(object sender, RoutedEventArgs e)
    {
        _isExpanded = !_isExpanded;
        UpdateExpandState();
        e.Handled = true;
    }

    private void MoreActionsBtn_Click(object sender, RoutedEventArgs e)
    {
        // Future: Show context menu with additional actions
        e.Handled = true;
    }

    private void UpdateExpandState()
    {
        if (_isExpanded)
        {
            DetailsBorder.Visibility = Visibility.Visible;
            ExpandIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.ChevronUp;
            ExpandToggleBtn.ToolTip = "Hide details";
        }
        else
        {
            DetailsBorder.Visibility = Visibility.Collapsed;
            ExpandIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.ChevronDown;
            ExpandToggleBtn.ToolTip = "Show details";
        }
    }
}
