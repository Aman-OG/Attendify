using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using System.ComponentModel;
using System.Linq;

namespace Attendify.ViewModels
{
    public class ReportsViewModel : INotifyPropertyChanged
    {
        public PlotModel AttendanceTrendModel { get; private set; }
        public PlotModel LeaveDistributionModel { get; private set; }
        public PlotModel DepartmentLeaveModel { get; private set; }
        public PlotModel PerformanceGaugeModel { get; private set; }

        public ReportsViewModel()
        {
            InitializeModels();
        }

        private void InitializeModels()
        {
            AttendanceTrendModel = CreateAttendanceTrendModel();
            LeaveDistributionModel = CreateLeaveDistributionModel();
            DepartmentLeaveModel = CreateDepartmentLeaveModel();
            PerformanceGaugeModel = CreateGaugeModel(85);
        }

        private PlotModel CreateAttendanceTrendModel()
        {
            var model = new PlotModel
            {
                Title = "Monthly Attendance Trend",
                TitleColor = OxyColors.White,
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
                MajorGridlineColor = OxyColor.FromArgb(40, 255, 255, 255),
                MinorGridlineColor = OxyColor.FromArgb(20, 255, 255, 255)
            };

            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = 0,
                Maximum = 150,
                TextColor = OxyColors.White,
                AxislineColor = OxyColors.White,
                TicklineColor = OxyColors.White,
                MajorGridlineColor = OxyColor.FromArgb(40, 255, 255, 255),
                MinorGridlineColor = OxyColor.FromArgb(20, 255, 255, 255)
            };

            model.Axes.Add(categoryAxis);
            model.Axes.Add(valueAxis);

            var lineSeries = new LineSeries
            {
                Title = "Attendance",
                MarkerType = MarkerType.Circle,
                MarkerSize = 5,
                MarkerFill = OxyColor.Parse("#00A6FB"),
                MarkerStroke = OxyColors.White,
                Color = OxyColor.Parse("#00A6FB"),
                StrokeThickness = 3
            };

            // Add sample data
            lineSeries.Points.Add(new DataPoint(0, 120));
            lineSeries.Points.Add(new DataPoint(1, 125));
            lineSeries.Points.Add(new DataPoint(2, 118));
            lineSeries.Points.Add(new DataPoint(3, 130));
            lineSeries.Points.Add(new DataPoint(4, 122));
            lineSeries.Points.Add(new DataPoint(5, 128));
            lineSeries.Points.Add(new DataPoint(6, 135));

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
                Title = "Leave Distribution",
                TitleColor = OxyColors.White,
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

            pieSeries.Slices.Add(new PieSlice("Approved", 25)
            {
                Fill = OxyColor.Parse("#2FBF4C")
            });
            pieSeries.Slices.Add(new PieSlice("Pending", 18)
            {
                Fill = OxyColor.Parse("#E3C63A")
            });
            pieSeries.Slices.Add(new PieSlice("Rejected", 8)
            {
                Fill = OxyColor.Parse("#FF6B6B")
            });
            pieSeries.Slices.Add(new PieSlice("Cancelled", 4)
            {
                Fill = OxyColor.Parse("#A95315")
            });

            model.Series.Add(pieSeries);
            return model;
        }

        private PlotModel CreateDepartmentLeaveModel()
        {
            var model = new PlotModel
            {
                Title = "Department Leave Comparison",
                TitleColor = OxyColors.White,
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
                StrokeThickness = 1,
                LabelColor = OxyColors.White,
                LabelPlacement = LabelPlacement.Inside
            };

            barSeries.Items.Add(new BarItem { Value = 10 });
            barSeries.Items.Add(new BarItem { Value = 4 });
            barSeries.Items.Add(new BarItem { Value = 6 });
            barSeries.Items.Add(new BarItem { Value = 12 });

            categoryAxis.Labels.Add("HR");
            categoryAxis.Labels.Add("IT");
            categoryAxis.Labels.Add("Finance");
            categoryAxis.Labels.Add("Marketing");

            model.Series.Add(barSeries);
            return model;
        }

        private PlotModel CreateGaugeModel(double value)
        {
            var model = new PlotModel
            {
                Title = $"{value}% Attendance Rate",
                TitleColor = OxyColors.White,
                PlotAreaBorderColor = OxyColors.Transparent,
                Background = OxyColors.Transparent,
                TextColor = OxyColors.White
            };

            var gaugeSeries = new PieSeries
            {
                StrokeThickness = 2,
                Stroke = OxyColors.White,
                InsideLabelColor = OxyColors.White,
                FontSize = 16,
                StartAngle = 270,
                AngleSpan = 360
            };

            gaugeSeries.Slices.Add(new PieSlice("", value)
            {
                Fill = OxyColor.Parse("#00A6FB")
            });
            gaugeSeries.Slices.Add(new PieSlice("", 100 - value)
            {
                Fill = OxyColor.Parse("#333333")
            });

            model.Series.Add(gaugeSeries);
            return model;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}