using System.Windows;
using System.Windows.Controls;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using System;
using OxyPlot.Annotations;

namespace Attendify.Views.UserControls
{
    public partial class ReportsView : UserControl
    {
        // OxyPlot chart properties for data binding
        public PlotModel AttendanceTrendModel { get; set; }
        public PlotModel LeaveDistributionModel { get; set; }
        public PlotModel DepartmentLeaveModel { get; set; }
        public PlotModel PerformanceGaugeModel { get; set; }

        public ReportsView()
        {
            InitializeComponent();
            InitializeCharts();
            DataContext = this; // Set DataContext to this for binding

            // Set default dates
            StartDatePicker.SelectedDate = DateTime.Today.AddDays(-30);
            EndDatePicker.SelectedDate = DateTime.Today;
        }

        private void InitializeCharts()
        {
            // Initialize all chart models
            AttendanceTrendModel = CreateAttendanceTrendModel();
            LeaveDistributionModel = CreateLeaveDistributionModel();
            DepartmentLeaveModel = CreateDepartmentLeaveModel();
            PerformanceGaugeModel = CreateGaugeModel(85); // 85% attendance rate
        }

        private PlotModel CreateAttendanceTrendModel()
        {
            var model = new PlotModel
            {
                PlotAreaBorderColor = OxyColors.Transparent,
                Background = OxyColors.Transparent,
                TextColor = OxyColors.White
            };

            var categoryAxis = new CategoryAxis
            {
                Position = AxisPosition.Bottom,
                TextColor = OxyColors.White,
                AxislineColor = OxyColors.White,
                TicklineColor = OxyColors.White,
                MajorGridlineColor = OxyColor.FromArgb(40, 255, 255, 255)
            };

            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = 0,
                Maximum = 100,
                TextColor = OxyColors.White,
                AxislineColor = OxyColors.White,
                TicklineColor = OxyColors.White,
                MajorGridlineColor = OxyColor.FromArgb(40, 255, 255, 255)
            };

            model.Axes.Add(categoryAxis);
            model.Axes.Add(valueAxis);

            var lineSeries = new LineSeries
            {
                MarkerType = MarkerType.Circle,
                MarkerSize = 5,
                MarkerFill = OxyColor.Parse("#00A6FB"),
                MarkerStroke = OxyColors.White,
                Color = OxyColor.Parse("#00A6FB"),
                StrokeThickness = 3
            };

            // Add sample data (percentage values)
            lineSeries.Points.Add(new DataPoint(0, 85));
            lineSeries.Points.Add(new DataPoint(1, 88));
            lineSeries.Points.Add(new DataPoint(2, 92));
            lineSeries.Points.Add(new DataPoint(3, 87));
            lineSeries.Points.Add(new DataPoint(4, 94));
            lineSeries.Points.Add(new DataPoint(5, 96));
            lineSeries.Points.Add(new DataPoint(6, 93));

            categoryAxis.Labels.Add("Mon");
            categoryAxis.Labels.Add("Tue");
            categoryAxis.Labels.Add("Wed");
            categoryAxis.Labels.Add("Thu");
            categoryAxis.Labels.Add("Fri");
            categoryAxis.Labels.Add("Sat");
            categoryAxis.Labels.Add("Sun");

            model.Series.Add(lineSeries);
            return model;
        }

        private PlotModel CreateLeaveDistributionModel()
        {
            var model = new PlotModel
            {
                PlotAreaBorderColor = OxyColors.Transparent,
                Background = OxyColors.Transparent,
                TextColor = OxyColors.White
            };

            var pieSeries = new PieSeries
            {
                InsideLabelColor = OxyColors.White,
                InsideLabelPosition = 0.7,
                FontSize = 12,
                StrokeThickness = 2,
                Stroke = OxyColors.White
            };

            pieSeries.Slices.Add(new PieSlice("Approved", 65)
            {
                Fill = OxyColor.Parse("#2FBF4C")
            });
            pieSeries.Slices.Add(new PieSlice("Pending", 18)
            {
                Fill = OxyColor.Parse("#E3C63A")
            });
            pieSeries.Slices.Add(new PieSlice("Rejected", 12)
            {
                Fill = OxyColor.Parse("#D23C3C")
            });

          

            model.Series.Add(pieSeries);
            return model;
        }

        private PlotModel CreateDepartmentLeaveModel()
        {
            var model = new PlotModel
            {
                PlotAreaBorderColor = OxyColors.Transparent,
                Background = OxyColors.Transparent,
                TextColor = OxyColors.White
            };

            // For horizontal bar chart, we use CategoryAxis on Y and LinearAxis on X
            var categoryAxis = new CategoryAxis
            {
                Position = AxisPosition.Left, // Changed to Left for horizontal bars
                TextColor = OxyColors.White,
                AxislineColor = OxyColors.White,
                TicklineColor = OxyColors.White,
            };

            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom, // Changed to Bottom for horizontal bars
                Minimum = 0,
                Maximum = 15,
                TextColor = OxyColors.White,
                AxislineColor = OxyColors.White,
                TicklineColor = OxyColors.White,
                MajorGridlineColor = OxyColor.FromArgb(40, 255, 255, 255)
            };

            model.Axes.Add(categoryAxis);
            model.Axes.Add(valueAxis);

            var barSeries = new BarSeries
            {
                FillColor = OxyColor.Parse("#00A6FB"),
                StrokeColor = OxyColors.White,
                StrokeThickness = 1
            };

            barSeries.Items.Add(new BarItem { Value = 12 });
            barSeries.Items.Add(new BarItem { Value = 8 });
            barSeries.Items.Add(new BarItem { Value = 6 });
            barSeries.Items.Add(new BarItem { Value = 10 });
            barSeries.Items.Add(new BarItem { Value = 4 });

            categoryAxis.Labels.Add("HR");
            categoryAxis.Labels.Add("IT");
            categoryAxis.Labels.Add("Finance");
            categoryAxis.Labels.Add("Marketing");
            categoryAxis.Labels.Add("Operations");

            model.Series.Add(barSeries);
            return model;
        }

        private PlotModel CreateGaugeModel(double value)
        {
            var model = new PlotModel
            {
                PlotAreaBorderColor = OxyColors.Transparent,
                Background = OxyColors.Transparent,
                TextColor = OxyColors.White
            };

            var gaugeSeries = new PieSeries
            {
                StrokeThickness = 2,
                Stroke = OxyColors.White,
                InsideLabelColor = OxyColors.White,
                FontSize = 12,
                StartAngle = 270,
                AngleSpan = 360,
                InsideLabelFormat = "{1}%"
            };

            // Add slices with labels
            gaugeSeries.Slices.Add(new PieSlice("Present", value)
            {
                Fill = OxyColor.Parse("#00A6FB")
            });
            gaugeSeries.Slices.Add(new PieSlice("Absent", 100 - value)
            {
                Fill = OxyColor.Parse("#333333")
            });

            model.Series.Add(gaugeSeries);

            return model;
        }
        

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Please select both start and end dates.", "Date Range Required",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (StartDatePicker.SelectedDate > EndDatePicker.SelectedDate)
            {
                MessageBox.Show("Start date cannot be after end date.", "Invalid Date Range",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Refresh charts with new date range
            InitializeCharts();

            MessageBox.Show($"Filters applied for period: {StartDatePicker.SelectedDate.Value:MMM dd} - {EndDatePicker.SelectedDate.Value:MMM dd}", "Filter Applied",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportAttendancePdf_Click(object sender, RoutedEventArgs e)
        {
            var dateRange = $"{StartDatePicker.SelectedDate.Value:yyyyMMdd}-{EndDatePicker.SelectedDate.Value:yyyyMMdd}";
            MessageBox.Show($"Attendance report ({dateRange}) exported as PDF!", "Export Successful",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportLeaveExcel_Click(object sender, RoutedEventArgs e)
        {
            var dateRange = $"{StartDatePicker.SelectedDate.Value:yyyyMMdd}-{EndDatePicker.SelectedDate.Value:yyyyMMdd}";
            MessageBox.Show($"Leave summary ({dateRange}) exported as Excel!", "Export Successful",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportEmployeeCsv_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Employee list exported as CSV!", "Export Successful",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportAnalyticsPdf_Click(object sender, RoutedEventArgs e)
        {
            var dateRange = $"{StartDatePicker.SelectedDate.Value:yyyyMMdd}-{EndDatePicker.SelectedDate.Value:yyyyMMdd}";
            MessageBox.Show($"Analytics report ({dateRange}) exported as PDF!", "Export Successful",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}