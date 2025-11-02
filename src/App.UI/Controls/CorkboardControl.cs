using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace App.UI.Controls;

public class CorkboardControl : Control
{
    private readonly Canvas _canvas;
    public static readonly DependencyProperty ZoomLevelProperty =
        DependencyProperty.Register(nameof(ZoomLevel), typeof(double), typeof(CorkboardControl), 
            new PropertyMetadata(1.0, OnZoomLevelChanged));

    public static readonly DependencyProperty GridEnabledProperty =
        DependencyProperty.Register(nameof(GridEnabled), typeof(bool), typeof(CorkboardControl), 
            new PropertyMetadata(true));

    public static readonly DependencyProperty GridSizeProperty =
        DependencyProperty.Register(nameof(GridSize), typeof(double), typeof(CorkboardControl), 
            new PropertyMetadata(20.0));

    private Point? _lastDragPoint;
    private const double MinZoom = 0.5;
    private const double MaxZoom = 2.0;

    public event EventHandler<IndexCardEventArgs>? CardMoved;

    static CorkboardControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CorkboardControl), 
            new FrameworkPropertyMetadata(typeof(CorkboardControl)));
    }

    protected virtual void OnCardMoved(IndexCardControl card, IndexCardEventArgs e)
    {
        CardMoved?.Invoke(card, e);
    }

    public CorkboardControl()
    {
        _canvas = new Canvas();
        _canvas.ClipToBounds = true;
        
        // Handle drag and drop
        _canvas.AllowDrop = true;
        _canvas.Drop += OnDrop;
        _canvas.DragOver += OnDragOver;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        var grid = GetTemplateChild("PART_CanvasContainer") as Grid;
        if (grid != null)
        {
            grid.Children.Add(_canvas);
        }
    }

    public double ZoomLevel
    {
        get => (double)GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    public bool GridEnabled
    {
        get => (bool)GetValue(GridEnabledProperty);
        set => SetValue(GridEnabledProperty, value);
    }

    public double GridSize
    {
        get => (double)GetValue(GridSizeProperty);
        set => SetValue(GridSizeProperty, value);
    }

    private static void OnZoomLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var corkboard = (CorkboardControl)d;
        corkboard.UpdateScale();
    }

    private void UpdateScale()
    {
        var scale = new ScaleTransform(ZoomLevel, ZoomLevel);
        _canvas.LayoutTransform = scale;
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            var delta = e.Delta > 0 ? 0.1 : -0.1;
            ZoomLevel = Math.Clamp(ZoomLevel + delta, MinZoom, MaxZoom);
            e.Handled = true;
        }
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.MiddleButton == MouseButtonState.Pressed)
        {
            _lastDragPoint = e.GetPosition(_canvas);
            _canvas.CaptureMouse();
            e.Handled = true;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (e.MiddleButton == MouseButtonState.Pressed && _lastDragPoint.HasValue)
        {
            var currentPoint = e.GetPosition(_canvas);
            var delta = currentPoint - _lastDragPoint.Value;

            foreach (UIElement child in _canvas.Children)
            {
                var left = Canvas.GetLeft(child) + delta.X;
                var top = Canvas.GetTop(child) + delta.Y;

                if (GridEnabled)
                {
                    left = Math.Round(left / GridSize) * GridSize;
                    top = Math.Round(top / GridSize) * GridSize;
                }

                Canvas.SetLeft(child, left);
                Canvas.SetTop(child, top);
            }

            _lastDragPoint = currentPoint;
            e.Handled = true;
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.MiddleButton == MouseButtonState.Released)
        {
            _lastDragPoint = null;
            _canvas.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(Guid)))
        {
            e.Effects = DragDropEffects.Move;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(Guid)))
        {
            var sceneId = (Guid)e.Data.GetData(typeof(Guid));
            var position = e.GetPosition(_canvas);

            // Find the index card with matching scene ID
            foreach (UIElement child in _canvas.Children)
            {
                if (child is IndexCardControl card && card.SceneId == sceneId)
                {
                    var left = position.X - (card.ActualWidth / 2);
                    var top = position.Y - (card.ActualHeight / 2);

                    if (GridEnabled)
                    {
                        left = Math.Round(left / GridSize) * GridSize;
                        top = Math.Round(top / GridSize) * GridSize;
                    }

                    Canvas.SetLeft(card, left);
                    Canvas.SetTop(card, top);
                    OnCardMoved(card, new IndexCardEventArgs(sceneId));
                    break;
                }
            }
        }
    }

    public void AddCard(IndexCardControl card, double x, double y)
    {
        _canvas.Children.Add(card);
        Canvas.SetLeft(card, x);
        Canvas.SetTop(card, y);
    }

    public void RemoveCard(IndexCardControl card)
    {
        _canvas.Children.Remove(card);
    }
}