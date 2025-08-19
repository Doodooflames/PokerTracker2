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

        public ObservableCollection<BuyInPoint> BuyInData
        {
            get { return (ObservableCollection<BuyInPoint>)GetValue(BuyInDataProperty); }
            set { SetValue(BuyInDataProperty, value); }
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
                 LineGraph.Points.Clear();
                 DataPointsCanvas.Children.Clear();
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
            var minAmount = 0; // Always start from 0 for buy-ins
            var amountRange = maxAmount - minAmount;

            if (amountRange == 0)
                amountRange = 1; // Prevent division by zero

            var pointCollection = new PointCollection();

            // Clear existing data points
            DataPointsCanvas.Children.Clear();

            // Handle single buy-in case: create a horizontal line across the middle
            if (BuyInData.Count == 1)
            {
                var point = BuyInData[0];
                // Position the line in the middle of the graph vertically, regardless of buy-in amount
                var y = canvasHeight / 2; // Center of the graph

                // Create a horizontal line from left to right at the middle of the graph
                pointCollection.Add(new Point(0, y)); // Start at left edge
                pointCollection.Add(new Point(canvasWidth, y)); // End at right edge
                
                // Create a single ellipse in the middle
                var ellipse = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = new SolidColorBrush(Color.FromRgb(79, 195, 247)), // #4FC3F7
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 1
                };
                
                var centerX = canvasWidth / 2;
                Canvas.SetLeft(ellipse, centerX - 3); // Center the 6x6 ellipse
                Canvas.SetTop(ellipse, y - 3);
                
                DataPointsCanvas.Children.Add(ellipse);
                
                System.Diagnostics.Debug.WriteLine($"BuyInLineGraph: Single point: Amount=${point.Amount}, Y={y} (middle), CenterX={centerX}");
            }
            else
            {
                // Multiple buy-ins: normal line graph
                for (int i = 0; i < BuyInData.Count; i++)
                {
                    var point = BuyInData[i];
                    var x = (i / (double)(BuyInData.Count - 1)) * canvasWidth;
                    var y = canvasHeight - ((point.Amount - minAmount) / amountRange) * canvasHeight;

                    // Ensure points are within canvas bounds
                    x = Math.Max(0, Math.Min(canvasWidth, x));
                    y = Math.Max(0, Math.Min(canvasHeight, y));

                    pointCollection.Add(new Point(x, y));
                    
                    // Create and position the ellipse for this data point
                    var ellipse = new Ellipse
                    {
                        Width = 6,
                        Height = 6,
                        Fill = new SolidColorBrush(Color.FromRgb(79, 195, 247)), // #4FC3F7
                        Stroke = new SolidColorBrush(Colors.White),
                        StrokeThickness = 1
                    };
                    
                    Canvas.SetLeft(ellipse, x - 3); // Center the 6x6 ellipse
                    Canvas.SetTop(ellipse, y - 3);
                    
                    DataPointsCanvas.Children.Add(ellipse);
                    
                    System.Diagnostics.Debug.WriteLine($"BuyInLineGraph: Point {i}: Amount=${point.Amount}, X={x}, Y={y}, Ellipse.Left={x-3}, Ellipse.Top={y-3}");
                }
            }

                         LineGraph.Points = pointCollection;
             
             // Update scale indicators
             UpdateScaleIndicators(maxAmount, canvasHeight, canvasWidth);
            
            System.Diagnostics.Debug.WriteLine($"BuyInLineGraph: Updated graph with {pointCollection.Count} points, {DataPointsCanvas.Children.Count} data points");
            System.Diagnostics.Debug.WriteLine($"BuyInLineGraph: Canvas size: {canvasWidth}x{canvasHeight}, Max amount: {maxAmount}");
        }

                 private void UpdateScaleIndicators(double maxAmount, double canvasHeight, double canvasWidth)
         {
             // Calculate scale positions (0%, 25%, 50%, 75%, 100%)
             var scaleHeight = canvasHeight;
             var scale0 = scaleHeight;
             var scale25 = scaleHeight * 0.75;
             var scale50 = scaleHeight * 0.5;
             var scale75 = scaleHeight * 0.25;
             var scale100 = 0.0;
 
             // Update scale line positions
             ScaleLine0.SetValue(Canvas.TopProperty, scale0);
             ScaleLine25.SetValue(Canvas.TopProperty, scale25);
             ScaleLine50.SetValue(Canvas.TopProperty, scale50);
             ScaleLine75.SetValue(Canvas.TopProperty, scale75);
             ScaleLine100.SetValue(Canvas.TopProperty, scale100);
 
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
 
             // Update scale label values
             ScaleLabel0.Text = "$0";
             ScaleLabel25.Text = $"${maxAmount * 0.25:F0}";
             ScaleLabel50.Text = $"${maxAmount * 0.5:F0}";
             ScaleLabel75.Text = $"${maxAmount * 0.75:F0}";
             ScaleLabel100.Text = $"${maxAmount:F0}";
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
