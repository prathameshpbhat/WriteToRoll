using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using App.Core.Models;

namespace App.UI.Controls;

public class IndexCardControl : Control, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(IndexCardControl), 
            new PropertyMetadata(string.Empty, OnTitleChanged));

    public static readonly DependencyProperty SummaryProperty =
        DependencyProperty.Register(nameof(Summary), typeof(string), typeof(IndexCardControl), 
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty CardColorProperty =
        DependencyProperty.Register(nameof(CardColor), typeof(Color), typeof(IndexCardControl), 
            new PropertyMetadata(Colors.White));

    public static readonly DependencyProperty SceneIdProperty =
        DependencyProperty.Register(nameof(SceneId), typeof(Guid), typeof(IndexCardControl), 
            new PropertyMetadata(Guid.Empty));

    static IndexCardControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(IndexCardControl), 
            new FrameworkPropertyMetadata(typeof(IndexCardControl)));
        
        KeyboardNavigation.TabNavigationProperty.OverrideMetadata(
            typeof(IndexCardControl), 
            new FrameworkPropertyMetadata(KeyboardNavigationMode.Once));

        KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(
            typeof(IndexCardControl), 
            new FrameworkPropertyMetadata(KeyboardNavigationMode.Cycle));

        FocusableProperty.OverrideMetadata(
            typeof(IndexCardControl), 
            new FrameworkPropertyMetadata(true));
    }

    public IndexCardControl()
    {
        // Set up accessibility
        AccessibilityHelper.SetupAccessibility(
            this,
            "Scene Card",
            "Drag to reorder scenes. Press Enter to edit, Space to select."
        );

        this.PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                // TODO: Implement edit mode
                e.Handled = true;
            }
            else if (e.Key == Key.Space)
            {
                CardSelected?.Invoke(this, new IndexCardEventArgs(SceneId));
                e.Handled = true;
            }
        };

        // Update accessible name when title changes
        this.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Title))
            {
                AccessibilityHelper.SetAccessibleLabel(this, $"Scene Card: {Title}");
            }
        };
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Summary
    {
        get => (string)GetValue(SummaryProperty);
        set => SetValue(SummaryProperty, value);
    }

    public Color CardColor
    {
        get => (Color)GetValue(CardColorProperty);
        set => SetValue(CardColorProperty, value);
    }

    public Guid SceneId
    {
        get => (Guid)GetValue(SceneIdProperty);
        set => SetValue(SceneIdProperty, value);
    }

    public event EventHandler<IndexCardEventArgs>? CardMoved;
    public event EventHandler<IndexCardEventArgs>? CardSelected;

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is IndexCardControl card)
        {
            card.PropertyChanged?.Invoke(card, new PropertyChangedEventArgs(nameof(Title)));
        }
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        CardSelected?.Invoke(this, new IndexCardEventArgs(SceneId));
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragDrop.DoDragDrop(this, SceneId, DragDropEffects.Move);
        }
    }
}

public class IndexCardEventArgs : EventArgs
{
    public Guid SceneId { get; }

    public IndexCardEventArgs(Guid sceneId)
    {
        SceneId = sceneId;
    }
}