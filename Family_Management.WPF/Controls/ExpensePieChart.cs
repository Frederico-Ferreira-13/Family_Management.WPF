using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Family_Management.WPF.ViewModel.Dashboard.Models;

namespace Family_Management.WPF.Controls;

public sealed class ExpensePieChart : FrameworkElement
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable<ExpenseCategoryData>),
            typeof(ExpensePieChart),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender,
                (d, _) => ((ExpensePieChart)d).ObserveCollection()));

    public static readonly DependencyProperty CurrencySymbolProperty =
        DependencyProperty.Register(
            nameof(CurrencySymbol),
            typeof(string),
            typeof(ExpensePieChart),
            new FrameworkPropertyMetadata(
                "€",
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TextBrushProperty =
        DependencyProperty.Register(
            nameof(TextBrush),
            typeof(Brush),
            typeof(ExpensePieChart),
            new FrameworkPropertyMetadata(
                Brushes.Black,
                FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly Brush[] Colors =
    {
        new SolidColorBrush(Color.FromRgb(37, 99, 235)),
        new SolidColorBrush(Color.FromRgb(13, 148, 136)),
        new SolidColorBrush(Color.FromRgb(245, 158, 11))
    };

    private readonly CultureInfo _culture =
        CultureInfo.GetCultureInfo("pt-PT");

    private INotifyCollectionChanged? _observed;

    public ExpensePieChart()
    {
        Loaded += (_, _) => ObserveCollection();
        Unloaded += (_, _) => StopObserving();
    }

    public IEnumerable<ExpenseCategoryData>? ItemsSource
    {
        get => (IEnumerable<ExpenseCategoryData>?)
            GetValue(ItemsSourceProperty);

        set => SetValue(ItemsSourceProperty, value);
    }

    public string CurrencySymbol
    {
        get => (string)GetValue(CurrencySymbolProperty);
        set => SetValue(CurrencySymbolProperty, value);
    }

    public Brush TextBrush
    {
        get => (Brush)GetValue(TextBrushProperty);
        set => SetValue(TextBrushProperty, value);
    }

    private void ObserveCollection()
    {
        StopObserving();

        if (IsLoaded &&
            ItemsSource is INotifyCollectionChanged collection)
        {
            _observed = collection;
            _observed.CollectionChanged += OnCollectionChanged;
        }

        InvalidateVisual();
    }

    private void StopObserving()
    {
        if (_observed is not null)
            _observed.CollectionChanged -= OnCollectionChanged;

        _observed = null;
    }

    private void OnCollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(
            double.IsInfinity(availableSize.Width)
                ? 280
                : Math.Min(280, availableSize.Width),
            300);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        if (ActualWidth < 40 || ActualHeight < 100)
            return;

        var items = (ItemsSource ??
                     Enumerable.Empty<ExpenseCategoryData>())
            .Where(x => x.Amount > 0)
            .OrderByDescending(x => x.Amount)
            .ThenBy(x => x.CategoryName)
            .Take(3)
            .ToList();

        var radius = Math.Min(
            90,
            Math.Min(
                (ActualWidth - 20) / 2,
                (ActualHeight - 110) / 2));

        if (radius <= 0)
            return;

        var center = new Point(
            ActualWidth / 2,
            radius + 8);

        if (items.Count == 0)
        {
            dc.DrawEllipse(
                null,
                new Pen(Brushes.LightGray, 2),
                center,
                radius,
                radius);

            DrawText(
                dc,
                "Sem despesas neste mês",
                0,
                radius * 2 + 30,
                ActualWidth,
                centered: true);

            ToolTip =
                "Não existem despesas positivas para apresentar neste mês.";

            return;
        }

        var total = items.Sum(x => x.Amount);
        var angle = -90d;

        for (var i = 0; i < items.Count; i++)
        {
            var sweep =
                (double)(items[i].Amount / total) * 360d;

            if (items.Count == 1)
            {
                dc.DrawEllipse(
                    Colors[i],
                    null,
                    center,
                    radius,
                    radius);
            }
            else
            {
                var geometry = new StreamGeometry();

                using (var context = geometry.Open())
                {
                    context.BeginFigure(
                        center,
                        isFilled: true,
                        isClosed: true);

                    context.LineTo(
                        PointAt(center, radius, angle),
                        isStroked: true,
                        isSmoothJoin: false);

                    context.ArcTo(
                        PointAt(center, radius, angle + sweep),
                        new Size(radius, radius),
                        rotationAngle: 0,
                        isLargeArc: sweep > 180,
                        sweepDirection: SweepDirection.Clockwise,
                        isStroked: true,
                        isSmoothJoin: false);
                }

                geometry.Freeze();

                dc.DrawGeometry(
                    Colors[i],
                    new Pen(Brushes.White, 1),
                    geometry);
            }

            angle += sweep;

            var y = radius * 2 + 30 + i * 28;

            dc.DrawRoundedRectangle(
                Colors[i],
                null,
                new Rect(0, y + 3, 12, 12),
                3,
                3);

            var percentage = items[i].Amount / total * 100;

            var label =
                $"{items[i].CategoryName}: " +
                $"{items[i].Amount.ToString("N2", _culture)} " +
                $"{CurrencySymbol} " +
                $"({percentage.ToString("N1", _culture)}%)";

            DrawText(
                dc,
                label,
                22,
                y,
                ActualWidth - 22);
        }

        ToolTip = string.Join(
            Environment.NewLine,
            items.Select(x =>
                $"{x.CategoryName}: " +
                $"{x.Amount.ToString("N2", _culture)} " +
                CurrencySymbol));
    }

    private static Point PointAt(
        Point center,
        double radius,
        double angle)
    {
        var radians = angle * Math.PI / 180;

        return new Point(
            center.X + radius * Math.Cos(radians),
            center.Y + radius * Math.Sin(radians));
    }

    private void DrawText(
        DrawingContext dc,
        string value,
        double x,
        double y,
        double width,
        bool centered = false)
    {
        var text = new FormattedText(
            value,
            _culture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            13,
            TextBrush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip)
        {
            MaxTextWidth = Math.Max(1, width),
            MaxLineCount = 1,
            Trimming = TextTrimming.CharacterEllipsis,
            TextAlignment = centered
                ? TextAlignment.Center
                : TextAlignment.Left
        };

        dc.DrawText(text, new Point(x, y));
    }
}