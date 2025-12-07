using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

namespace Attendify.Views.UserControls
{
    public partial class EmployeeReportsView : UserControl, INotifyPropertyChanged
    {
        public PlotModel MonthlyAttendanceModel { get; set; }
        public PlotModel AttendanceDistributionModel { get; set; }
        public PlotModel CheckinTimeModel { get; set; }
        public PlotModel PerformanceGaugeModel { get; set; }
        public ObservableCollection<MonthlyReport> MonthlyReports { get; set; }

        public EmployeeReportsView()
        {
            InitializeComponent();
            InitializeCharts();
            InitializeMonthlyReports();
            DataContext = this;

            StartDatePicker.SelectedDate = DateTime.Today.AddMonths(-6);
            EndDatePicker.SelectedDate = DateTime.Today;
        }

        private void InitializeCharts()
        {
            MonthlyAttendanceModel = CreateMonthlyAttendanceModel();
            AttendanceDistributionModel = CreateAttendanceDistributionModel();
            CheckinTimeModel = CreateCheckinTimeModel();
            PerformanceGaugeModel = CreateGaugeModel(85);
        }

        private void InitializeMonthlyReports()
        {
            MonthlyReports = new ObservableCollection<MonthlyReport>
            {
                new MonthlyReport { Month = "Jan 2025", Present = 18, Late = 4, Absent = 2, LeavesApproved = 1, AttendancePercentage = 85 },
                new MonthlyReport { Month = "Dec 2024", Present = 20, Late = 2, Absent = 1, LeavesApproved = 0, AttendancePercentage = 95 },
                new MonthlyReport { Month = "Nov 2024", Present = 19, Late = 3, Absent = 2, LeavesApproved = 1, AttendancePercentage = 90 },
                new MonthlyReport { Month = "Oct 2024", Present = 17, Late = 5, Absent = 3, LeavesApproved = 2, AttendancePercentage = 80 },
                new MonthlyReport { Month = "Sep 2024", Present = 21, Late = 1, Absent = 0, LeavesApproved = 0, AttendancePercentage = 100 },
                new MonthlyReport { Month = "Aug 2024", Present = 18, Late = 4, Absent = 2, LeavesApproved = 1, AttendancePercentage = 85 }
            };
        }

        private PlotModel CreateMonthlyAttendanceModel()
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
                Maximum = 25,
                TextColor = OxyColors.White,
                AxislineColor = OxyColors.White,
                TicklineColor = OxyColors.White,
                MajorGridlineColor = OxyColor.FromArgb(40, 255, 255, 255)
            };

            model.Axes.Add(categoryAxis);
            model.Axes.Add(valueAxis);

            // Use LineSeries with thick lines to simulate bars
            var presentSeries = new LineSeries
            {
                Title = "Present",
                Color = OxyColor.Parse("#2FBF4C"),
                StrokeThickness = 20,
                MarkerType = MarkerType.None
            };

            var lateSeries = new LineSeries
            {
                Title = "Late",
                Color = OxyColor.Parse("#E3C63A"),
                StrokeThickness = 20,
                MarkerType = MarkerType.None
            };

            var absentSeries = new LineSeries
            {
                Title = "Absent",
                Color = OxyColor.Parse("#D23C3C"),
                StrokeThickness = 20,
                MarkerType = MarkerType.None
            };

            // Add data points
            double[] presentData = { 18, 20, 19, 17, 21, 18 };
            double[] lateData = { 4, 2, 3, 5, 1, 4 };
            double[] absentData = { 2, 1, 2, 3, 0, 2 };

            for (int i = 0; i < 6; i++)
            {
                presentSeries.Points.Add(new DataPoint(i, presentData[i]));
                lateSeries.Points.Add(new DataPoint(i, lateData[i]));
                absentSeries.Points.Add(new DataPoint(i, absentData[i]));
            }

            categoryAxis.Labels.Add("Jan");
            categoryAxis.Labels.Add("Dec");
            categoryAxis.Labels.Add("Nov");
            categoryAxis.Labels.Add("Oct");
            categoryAxis.Labels.Add("Sep");
            categoryAxis.Labels.Add("Aug");

            model.Series.Add(presentSeries);
            model.Series.Add(lateSeries);
            model.Series.Add(absentSeries);

            return model;
        }

        private PlotModel CreateAttendanceDistributionModel()
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

            pieSeries.Slices.Add(new PieSlice("Present", 85) { Fill = OxyColor.Parse("#2FBF4C") });
            pieSeries.Slices.Add(new PieSlice("Late", 10) { Fill = OxyColor.Parse("#E3C63A") });
            pieSeries.Slices.Add(new PieSlice("Absent", 5) { Fill = OxyColor.Parse("#D23C3C") });

            model.Series.Add(pieSeries);
            return model;
        }

        private PlotModel CreateCheckinTimeModel()
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
                Minimum = 8,
                Maximum = 10.5,
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

            lineSeries.Points.Add(new DataPoint(0, 9.1));
            lineSeries.Points.Add(new DataPoint(1, 8.9));
            lineSeries.Points.Add(new DataPoint(2, 9.3));
            lineSeries.Points.Add(new DataPoint(3, 8.8));
            lineSeries.Points.Add(new DataPoint(4, 9.2));
            lineSeries.Points.Add(new DataPoint(5, 9.0));
            lineSeries.Points.Add(new DataPoint(6, 8.7));

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

            gaugeSeries.Slices.Add(new PieSlice("Present", value) { Fill = OxyColor.Parse("#00A6FB") });
            gaugeSeries.Slices.Add(new PieSlice("Absent", 100 - value) { Fill = OxyColor.Parse("#333333") });

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

            InitializeCharts();

            MessageBox.Show($"Filters applied for period: {StartDatePicker.SelectedDate.Value:MMM dd} - {EndDatePicker.SelectedDate.Value:MMM dd}",
                          "Filter Applied", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportAttendancePdf_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("My attendance report exported as PDF!", "Export Successful",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportMonthlyExcel_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Monthly summary exported as Excel!", "Export Successful",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportTimeCsv_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Time analysis exported as CSV!", "Export Successful",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MonthlyReport
    {
        public string Month { get; set; }
        public int Present { get; set; }
        public int Late { get; set; }
        public int Absent { get; set; }
        public int LeavesApproved { get; set; }
        public double AttendancePercentage { get; set; }
    }
}