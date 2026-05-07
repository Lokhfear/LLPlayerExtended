using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LLPlayer.Controls;

/// <summary>
/// Empty State View control - displays empty state with optional action
/// </summary>
public partial class EmptyStateView : UserControl
{
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(string), typeof(EmptyStateView), new PropertyMetadata("📭"));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(EmptyStateView), new PropertyMetadata("Nothing here yet"));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(EmptyStateView), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ActionButtonTextProperty =
        DependencyProperty.Register(nameof(ActionButtonText), typeof(string), typeof(EmptyStateView), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ActionCommandProperty =
        DependencyProperty.Register(nameof(ActionCommand), typeof(ICommand), typeof(EmptyStateView), new PropertyMetadata(null));

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string ActionButtonText
    {
        get => (string)GetValue(ActionButtonTextProperty);
        set => SetValue(ActionButtonTextProperty, value);
    }

    public ICommand? ActionCommand
    {
        get => (ICommand?)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public EmptyStateView()
    {
        InitializeComponent();
    }
}
