using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using PokerTracker2.Models;

namespace PokerTracker2.Controls
{
    public partial class BuyInLineGraph : UserControl
    {
        public static readonly DependencyProperty BuyInDataProperty =
            DependencyProperty.Register("BuyInData", typeof(ObservableCollection<BuyInPoint>), typeof(BuyInLineGraph),
                new PropertyMetadata(null, OnBuyInDataChanged));

        public static readonly DependencyProperty IsProfitGraphProperty =
            DependencyProperty.Register("IsProfitGraph", typeof(bool), typeof(BuyInLineGraph),
                new PropertyMetadata(false, OnProfitGraphChanged));

        public ObservableCollection<BuyInPoint> BuyInData
        {
            get { return (ObservableCollection<BuyInPoint>)GetValue(BuyInDataProperty); }
            set { SetValue(BuyInDataProperty, value); }
        }

        public bool IsProfitGraph
        {
            get { return (bool)GetValue(IsProfitGraphProperty); }
            set { SetValue(IsProfitGraphProperty, value); }
        }

        public BuyInLineGraph()
        {
            InitializeComponent();
            
            // Subscribe to loaded event
            this.Loaded += (s, e) => Dispatcher.BeginInvoke(new Action(() => UpdateGraph()), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private static void OnBuyInDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (BuyInLineGraph)d;

            // Unsubscribe from old collection if it exists
            if (e.OldValue is ObservableCollection<BuyInPoint> oldCollection)
            {
                oldCollection.CollectionChanged -= control.BuyInData_CollectionChanged;
            }

            // Subscribe to new collection if it exists
            if (e.NewValue is ObservableCollection<BuyInPoint> newCollection)
            {
                newCollection.CollectionChanged += control.BuyInData_CollectionChanged;
            }
            
            control.UpdateGraph();
        }

        private static void OnProfitGraphChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (BuyInLineGraph)d;
            control.UpdateGraph();
        }

        private void BuyInData_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateGraph();
        }

        public void UpdateGraph()
        {
                         // Add debugging
             System.Diagnostics.Debug.WriteLine($"BuyInLineGraph.UpdateGraph() called. Data count: {BuyInData?.Count ?? 0}");
            
                         if (BuyInData == null || BuyInData.Count == 0)
             {
                 DataPointsCanvas.Children.Clear();
                 LineSegmentsCanvas.Children.Clear();
                 ZeroLine.Visibility = Visibility.Collapsed;
                 System.Diagnostics.Debug.WriteLine("BuyInLineGraph: No data, clearing graph");
                 return;
             }

            var canvasWidth = GraphCanvas.ActualWidth - 10; // Account for margin
            var canvasHeight = GraphCanvas.ActualHeight - 10;

                         // If canvas size is not available, use default values
             if (canvasWidth <= 0 || canvasHeight <= 0)
             {
                 canvasWidth = GraphCanvas.ActualWidth > 0 ? GraphCanvas.ActualWidth - 10 : 300; // Use actual width or default to 300
                 canvasHeight = 90; // Default height (100 - 10 margin)
                 System.Diagnostics.Debug.WriteLine("BuyInLineGraph: Using default canvas size");
             }

            var maxAmount = BuyInData.Max(p => p.Amount);
            var minAmount = BuyInData.Min(p => p.Amount);
            
            // For profit graphs, ensure 0 is included in the range for reference
            if (maxAmount > 0 && minAmount > 0)
                minAmount = 0; // Extend range to include 0 if all values are positive
            else if (maxAmount < 0 && minAmount < 0)
                maxAmount = 0; // Extend range to include 0 if all values are negative
            
            var amountRange = maxAmount - minAmount;

            if (amountRange == 0)
                amountRange = 1; // Prevent division by zero

            // Clear existing data points and line segments
            DataPointsCanvas.Children.Clear();
            LineSegmentsCanvas.Children.Clear();

            // Handle single point case
            if (BuyInData.Count == 1)
            {
                var point = BuyInData[0];
                
                // Calculate Y positions for start ($0) and end (profit value)
                var startY = canvasHeight - ((0 - minAmount) / amountRange) * canvasHeight;
                var endY = canvasHeight - ((point.Amount - minAmount) / amountRange) * canvasHeight;
                var buyInY = canvasHeight - ((point.Amount - minAmount) / amountRange) * canvasHeight;
                
                // Ensure Y positions are within canvas bounds
                startY = Math.Max(0, Math.Min(canvasHeight, startY));
                endY = Math.Max(0, Math.Min(canvasHeight, endY));
                buyInY = Math.Max(0, Math.Min(canvasHeight, buyInY));

                if (IsProfitGraph)
                {
                    // PROFIT GRAPH: Curved line from $0 to profit value with correct colors
                    var pathFigure = new PathFigure
                    {
                        StartPoint = new Point(0, startY),
                        IsClosed = false
                    };

                    // Create a smooth curve using QuadraticBezierSegment with perpendicular offset control point
                    var controlPoint = ComputeControlPoint(0, startY, canvasWidth, endY, 0.15);
                    var quadraticBezier = new QuadraticBezierSegment
                    {
                        Point1 = controlPoint,
                        Point2 = new Point(canvasWidth, endY)
                    };
                    
                    pathFigure.Segments.Add(quadraticBezier);
                    
                    var pathGeometry = new PathGeometry();
                    pathGeometry.Figures.Add(pathFigure);
                    
                    var path = new Path
                    {
                        Data = pathGeometry,
                        Stroke = new SolidColorBrush(point.Amount >= 0 ? Colors.LightGreen : Colors.Red),
                        StrokeThickness = 2,
                        Fill = null
                    };
                    LineSegmentsCanvas.Children.Add(path);
                }
                else
                {
                    // BUY-IN GRAPH: Flat horizontal line at the buy-in amount
                    // Draw horizontal line across the entire width
                    var horizontalLine = new Line
                    {
                        X1 = 0, Y1 = buyInY,
                        X2 = canvasWidth, Y2 = buyInY,
                        Stroke = new SolidColorBrush(Color.FromRgb(79, 195, 247)), // #4FC3F7
                        StrokeThickness = 2
                    };
                    LineSegmentsCanvas.Children.Add(horizontalLine);
                }
                
                // Create data points
                if (IsProfitGraph)
                {
                    // Profit graph: start point at $0 (white), end point colored
                    var startEllipse = new Ellipse
                    {
                        Width = 6, Height = 6,
                        Fill = new SolidColorBrush(Colors.White),
                        Stroke = new SolidColorBrush(Colors.White),
                        StrokeThickness = 1
                    };
                    Canvas.SetLeft(startEllipse, 0 - 3);
                    Canvas.SetTop(startEllipse, startY - 3);
                    DataPointsCanvas.Children.Add(startEllipse);
                    
                    var endEllipse = new Ellipse
                    {
                        Width = 6, Height = 6,
                        Fill = new SolidColorBrush(point.Amount >= 0 ? Colors.LightGreen : Colors.Red),
                        Stroke = new SolidColorBrush(Colors.White),
                        StrokeThickness = 1
                    };
                    Canvas.SetLeft(endEllipse, canvasWidth - 3);
                    Canvas.SetTop(endEllipse, endY - 3);
                    DataPointsCanvas.Children.Add(endEllipse);
                }
                else
                {
                    // Buy-in graph: single point in middle
                    var ellipse = new Ellipse
                    {
                        Width = 6, Height = 6,
                        Fill = new SolidColorBrush(Color.FromRgb(79, 195, 247)),
                        Stroke = new SolidColorBrush(Colors.White),
                        StrokeThickness = 1
                    };
                    Canvas.SetLeft(ellipse, canvasWidth / 2 - 3);
                    Canvas.SetTop(ellipse, buyInY - 3);
                    DataPointsCanvas.Children.Add(ellipse);
                }
                
                System.Diagnostics.Debug.WriteLine($"BuyInLineGraph: Single point: Amount=${point.Amount}, Y={buyInY} (${point.Amount})");
            }
            else
            {
                // Multiple buy-ins: colored line graph with segments
                var points = new List<(double X, double Y, double Amount)>();
                
                for (int i = 0; i < BuyInData.Count; i++)
                {
                    var point = BuyInData[i];
                    var x = (i / (double)(BuyInData.Count - 1)) * canvasWidth;
                    var y = canvasHeight - ((point.Amount - minAmount) / amountRange) * canvasHeight;

                    // Ensure points are within canvas bounds
                    x = Math.Max(0, Math.Min(canvasWidth, x));
                    y = Math.Max(0, Math.Min(canvasHeight, y));

                    points.Add((x, y, point.Amount));
                    
                    // Create and position the ellipse for this data point
                    var ellipse = new Ellipse
                    {
                        Width = 6,
                        Height = 6,
                        Fill = new SolidColorBrush(IsProfitGraph ? 
                            (point.Amount >= 0 ? Colors.LightGreen : Colors.Red) :
                            Color.FromRgb(79, 195, 247)), // Profit graph: green/red, Buy-in graph: blue
                        Stroke = new SolidColorBrush(Colors.White),
                        StrokeThickness = 1
                    };
                    
                    Canvas.SetLeft(ellipse, x - 3); // Center the 6x6 ellipse
                    Canvas.SetTop(ellipse, y - 3);
                    
                    DataPointsCanvas.Children.Add(ellipse);
                    
                    System.Diagnostics.Debug.WriteLine($"BuyInLineGraph: Point {i}: Amount=${point.Amount}, X={x}, Y={y}, Ellipse.Left={x-3}, Ellipse.Top={y-3}");
                }
                
                // Insert zero crossing points to act as spline knots for smooth color transitions
                if (IsProfitGraph)
                {
                    var enriched = new List<(double X, double Y, double Amount)>();
                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        var a = points[i];
                        var b = points[i + 1];
                        enriched.Add(a);
                        if ((a.Amount < 0 && b.Amount > 0) || (a.Amount > 0 && b.Amount < 0))
                        {
                            var zeroY = canvasHeight - ((0 - minAmount) / amountRange) * canvasHeight;
                            var totalDeltaY = b.Y - a.Y;
                            var deltaToZero = zeroY - a.Y;
                            var ratioToZero = totalDeltaY == 0 ? 0.5 : (deltaToZero / totalDeltaY);
                            var zeroX = a.X + (ratioToZero * (b.X - a.X));
                            enriched.Add((zeroX, zeroY, 0));
                        }
                    }
                    enriched.Add(points[^1]);

                    // Draw per-segment Cubic Bezier using Catmull-Rom conversion for continuity
                    for (int i = 0; i < enriched.Count - 1; i++)
                    {
                        var p0 = i == 0 ? enriched[i] : enriched[i - 1];
                        var p1 = enriched[i];
                        var p2 = enriched[i + 1];
                        var p3 = i + 2 < enriched.Count ? enriched[i + 2] : enriched[i + 1];

                        // Catmull-Rom to Bezier (uniform) control points
                        Point cp1 = new Point(
                            p1.X + (p2.X - p0.X) / 6.0,
                            p1.Y + (p2.Y - p0.Y) / 6.0);
                        Point cp2 = new Point(
                            p2.X - (p3.X - p1.X) / 6.0,
                            p2.Y - (p3.Y - p1.Y) / 6.0);

                        var figure = new PathFigure { StartPoint = new Point(p1.X, p1.Y), IsClosed = false };
                        var bezier = new BezierSegment
                        {
                            Point1 = cp1,
                            Point2 = cp2,
                            Point3 = new Point(p2.X, p2.Y),
                            IsStroked = true
                        };
                        figure.Segments.Add(bezier);
                        var geometry = new PathGeometry();
                        geometry.Figures.Add(figure);

                        var midAmount = (p1.Amount + p2.Amount) * 0.5;
                        var color = midAmount >= 0 ? Colors.LightGreen : Colors.Red;
                        var path = new Path
                        {
                            Data = geometry,
                            Stroke = new SolidColorBrush(color),
                            StrokeThickness = 2,
                            Fill = null
                        };
                        LineSegmentsCanvas.Children.Add(path);
                    }
                }
                else
                {
                    // BUY-IN GRAPH: Original single-color straight segments
                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        var currentPoint = points[i];
                        var nextPoint = points[i + 1];
                        var lineSegment = new Line
                        {
                            X1 = currentPoint.X, Y1 = currentPoint.Y,
                            X2 = nextPoint.X, Y2 = nextPoint.Y,
                            Stroke = new SolidColorBrush(Color.FromRgb(79, 195, 247)), // #4FC3F7
                            StrokeThickness = 2
                        };
                        LineSegmentsCanvas.Children.Add(lineSegment);
                    }
                }
            }
             
             // Position the zero line if this is a profit graph and zero is within the range
             if (IsProfitGraph && minAmount <= 0 && maxAmount >= 0)
             {
                 var zeroY = canvasHeight - ((0 - minAmount) / amountRange) * canvasHeight;
                 ZeroLine.X1 = 0;
                 ZeroLine.X2 = canvasWidth;
                 ZeroLine.Y1 = zeroY;
                 ZeroLine.Y2 = zeroY;
                 ZeroLine.Visibility = Visibility.Visible;
             }
             else
             {
                 ZeroLine.Visibility = Visibility.Collapsed;
             }
             
             // Update scale indicators
             UpdateScaleIndicators(maxAmount, minAmount, canvasHeight, canvasWidth);
            
            System.Diagnostics.Debug.WriteLine($"BuyInLineGraph: Updated graph with {DataPointsCanvas.Children.Count} data points, {LineSegmentsCanvas.Children.Count} line segments");
            System.Diagnostics.Debug.WriteLine($"BuyInLineGraph: Canvas size: {canvasWidth}x{canvasHeight}, Max amount: {maxAmount}");
        }

        private static Point ComputeControlPoint(double x1, double y1, double x2, double y2, double curvature)
        {
            // Midpoint between the two points
            double midX = (x1 + x2) * 0.5;
            double midY = (y1 + y2) * 0.5;

            // Direction vector from p1 to p2
            double dx = x2 - x1;
            double dy = y2 - y1;

            double length = Math.Sqrt((dx * dx) + (dy * dy));
            if (length <= 0.0001)
            {
                // Degenerate; return midpoint
                return new Point(midX, midY);
            }

            // Perpendicular unit vector (rotate by 90 degrees)
            double nx = -dy / length;
            double ny = dx / length;

            // Curvature scale relative to segment length
            double offset = length * curvature;

            return new Point(midX + (nx * offset), midY + (ny * offset));
        }

                         private void UpdateScaleIndicators(double maxAmount, double minAmount, double canvasHeight, double canvasWidth)
         {
             // Get the current Y-axis width from the dynamic resource and update the column width
             var yAxisWidth = (double)Application.Current.Resources["ScaledGraphYAxisWidth"];
             YAxisColumn.Width = new GridLength(yAxisWidth);
             
             // Calculate scale positions (0%, 25%, 50%, 75%, 100%)
             var scaleHeight = canvasHeight;
             var scale0 = scaleHeight;
             var scale25 = scaleHeight * 0.75;
             var scale50 = scaleHeight * 0.5;
             var scale75 = scaleHeight * 0.25;
             var scale100 = 0.0;
 
            // Use the Y-axis width for line positioning
            var lineStartX = yAxisWidth - 5;  // Start 5 pixels from the right edge
            var lineEndX = yAxisWidth;        // End at the right edge
            
            // Update scale line positions and coordinates
            ScaleLine0.SetValue(Canvas.TopProperty, scale0);
            ScaleLine0.X1 = lineStartX;
            ScaleLine0.X2 = lineEndX;
            
            ScaleLine25.SetValue(Canvas.TopProperty, scale25);
            ScaleLine25.X1 = lineStartX;
            ScaleLine25.X2 = lineEndX;
            
            ScaleLine50.SetValue(Canvas.TopProperty, scale50);
            ScaleLine50.X1 = lineStartX;
            ScaleLine50.X2 = lineEndX;
            
            ScaleLine75.SetValue(Canvas.TopProperty, scale75);
            ScaleLine75.X1 = lineStartX;
            ScaleLine75.X2 = lineEndX;
            
            ScaleLine100.SetValue(Canvas.TopProperty, scale100);
            ScaleLine100.X1 = lineStartX;
            ScaleLine100.X2 = lineEndX;
 
             // Update grid line positions and lengths
             GridLine0.SetValue(Canvas.TopProperty, scale0);
             GridLine0.X2 = canvasWidth; // Set the width to span the full graph
             GridLine25.SetValue(Canvas.TopProperty, scale25);
             GridLine25.X2 = canvasWidth;
             GridLine50.SetValue(Canvas.TopProperty, scale50);
             GridLine50.X2 = canvasWidth;
             GridLine75.SetValue(Canvas.TopProperty, scale75);
             GridLine75.X2 = canvasWidth;
             GridLine100.SetValue(Canvas.TopProperty, scale100);
             GridLine100.X2 = canvasWidth;
 
             // Update scale labels
             ScaleLabel0.SetValue(Canvas.TopProperty, scale0 - 8);
             ScaleLabel25.SetValue(Canvas.TopProperty, scale25 - 8);
             ScaleLabel50.SetValue(Canvas.TopProperty, scale50 - 8);
             ScaleLabel75.SetValue(Canvas.TopProperty, scale75 - 8);
             ScaleLabel100.SetValue(Canvas.TopProperty, scale100 - 8);
 
            // Calculate proper scale values based on min/max amounts
            // Note: In Canvas coordinates, Y=0 is at the top, Y=canvasHeight is at the bottom
            // So scale0 (bottom) should show minAmount, scale100 (top) should show maxAmount
            var amountRange = maxAmount - minAmount;
            var scaleValue0 = minAmount;     // Bottom of graph (scale0) = minimum value
            var scaleValue25 = minAmount + (amountRange * 0.25);
            var scaleValue50 = minAmount + (amountRange * 0.5);
            var scaleValue75 = minAmount + (amountRange * 0.75);
            var scaleValue100 = maxAmount;   // Top of graph (scale100) = maximum value

            // Update scale label values with proper formatting
            ScaleLabel0.Text = $"${scaleValue0:F0}";
            ScaleLabel25.Text = $"${scaleValue25:F0}";
            ScaleLabel50.Text = $"${scaleValue50:F0}";
            ScaleLabel75.Text = $"${scaleValue75:F0}";
            ScaleLabel100.Text = $"${scaleValue100:F0}";
         }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateGraph();
        }
    }

    public class DataPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
    }
}
