using System.Windows;
using System.Windows.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;

namespace Attendify.Views.UserControls
{
    public partial class ReportsView : UserControl
    {
        // Chart properties for data binding
        public ISeries[] AttendanceTrendSeries { get; set; }
        public ISeries[] LeaveDistributionSeries { get; set; }
        public ISeries[] DepartmentLeaveSeries { get; set; }
        public Axis[] XAxes { get; set; }
        public Axis[] YAxes { get; set; }
        public Axis[] DepartmentXAxes { get; set; }
        public Axis[] DepartmentYAxes { get; set; }

        public ReportsView()
        {
            InitializeComponent();
            InitializeCharts();
            DataContext = this; // Set DataContext to this for binding
        }

        private void InitializeCharts()
        {
            // Attendance Trend Line Chart
            AttendanceTrendSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = new double[] { 85, 88, 92, 87, 94, 96, 93, 95, 98, 97, 96, 94 },
                    Name = "Attendance %",
                    Stroke = new SolidColorPaint(SKColor.Parse("#00A6FB")) { StrokeThickness = 3 }, // FIXED: SKColor.Parse
                    Fill = new LinearGradientPaint(
                        new[] { SKColor.Parse("#00A6FB").WithAlpha(100), SKColor.Parse("#00A6FB").WithAlpha(20) }, // FIXED: SKColor.Parse
                        new SKPoint(0.5f, 0),
                        new SKPoint(0.5f, 1)),
                    GeometryStroke = new SolidColorPaint(SKColor.Parse("#00A6FB")), // FIXED: SKColor.Parse
                    GeometrySize = 8
                }
            };

            XAxes = new[]
            {
                new Axis
                {
                    Labels = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" },
                    LabelsPaint = new SolidColorPaint(SKColors.White), // SKColors.White is fine (predefined color)
                    TextSize = 12
                }
            };

            YAxes = new[]
            {
                new Axis
                {
                    Labeler = value => value.ToString("N0") + "%",
                    LabelsPaint = new SolidColorPaint(SKColors.White), // SKColors.White is fine
                    TextSize = 12
                }
            };

            // Leave Distribution Pie Chart
            LeaveDistributionSeries = new ISeries[]
            {
                new PieSeries<int> { Values = new[] { 65 }, Name = "Approved", Fill = new SolidColorPaint(SKColor.Parse("#2FBF4C")) }, // FIXED
                new PieSeries<int> { Values = new[] { 18 }, Name = "Pending", Fill = new SolidColorPaint(SKColor.Parse("#E3C63A")) }, // FIXED
                new PieSeries<int> { Values = new[] { 12 }, Name = "Rejected", Fill = new SolidColorPaint(SKColor.Parse("#D23C3C")) }, // FIXED
                new PieSeries<int> { Values = new[] { 5 }, Name = "Cancelled", Fill = new SolidColorPaint(SKColor.Parse("#A95315")) } // FIXED
            };

            // Department Leave Bar Chart
            DepartmentLeaveSeries = new ISeries[]
            {
                new ColumnSeries<int>
                {
                    Values = new[] { 12, 8, 6, 10, 4 },
                    Name = "Leaves",
                    Fill = new LinearGradientPaint(
                        new[] { SKColor.Parse("#00A6FB"), SKColor.Parse("#0088CC") }, // FIXED
                        new SKPoint(0.5f, 0),
                        new SKPoint(0.5f, 1))
                }
            };

            DepartmentXAxes = new[]
            {
                new Axis
                {
                    Labels = new[] { "HR", "IT", "Finance", "Marketing", "Operations" },
                    LabelsPaint = new SolidColorPaint(SKColors.White), // SKColors.White is fine
                    TextSize = 12
                }
            };

            DepartmentYAxes = new[]
            {
                new Axis
                {
                    Labeler = value => value.ToString("N0"),
                    LabelsPaint = new SolidColorPaint(SKColors.White), // SKColors.White is fine
                    TextSize = 12
                }
            };
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            // Refresh data based on selected date range
            MessageBox.Show("Filters applied successfully!", "Filter",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportAttendancePdf_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Attendance report exported as PDF!", "Export",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportLeaveExcel_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Leave summary exported as Excel!", "Export",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportEmployeeCsv_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Employee list exported as CSV!", "Export",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportAnalyticsPdf_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Analytics report exported as PDF!", "Export",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}