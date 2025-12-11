using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.IO;
using System.Windows.Controls;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace Attendify.ViewModels
{
    public class AdminReportViewModel : INotifyPropertyChanged
    {
        private readonly HttpClient _httpClient;
        private bool _isLoading;
        private string _loadingMessage = "Loading reports...";

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public string LoadingMessage
        {
            get => _loadingMessage;
            set
            {
                _loadingMessage = value;
                OnPropertyChanged(nameof(LoadingMessage));
            }
        }

        // KPI Properties
        private int _totalEmployees;
        public int TotalEmployees
        {
            get => _totalEmployees;
            set { _totalEmployees = value; OnPropertyChanged(); }
        }

        private double _todayAttendancePercentage;
        public double TodayAttendancePercentage
        {
            get => _todayAttendancePercentage;
            set { _todayAttendancePercentage = value; OnPropertyChanged(); }
        }

        private int _pendingLeaves;
        public int PendingLeaves
        {
            get => _pendingLeaves;
            set { _pendingLeaves = value; OnPropertyChanged(); }
        }

        private double _attendanceRate;
        public double AttendanceRate
        {
            get => _attendanceRate;
            set { _attendanceRate = value; OnPropertyChanged(); }
        }

        // Charts
        private PlotModel _attendanceTrendModel;
        public PlotModel AttendanceTrendModel
        {
            get => _attendanceTrendModel;
            set { _attendanceTrendModel = value; OnPropertyChanged(); }
        }

        private PlotModel _leaveDistributionModel;
        public PlotModel LeaveDistributionModel
        {
            get => _leaveDistributionModel;
            set { _leaveDistributionModel = value; OnPropertyChanged(); }
        }

        private PlotModel _departmentLeaveModel;
        public PlotModel DepartmentLeaveModel
        {
            get => _departmentLeaveModel;
            set { _departmentLeaveModel = value; OnPropertyChanged(); }
        }

        private PlotModel _performanceGaugeModel;
        public PlotModel PerformanceGaugeModel
        {
            get => _performanceGaugeModel;
            set { _performanceGaugeModel = value; OnPropertyChanged(); }
        }

        // Quick Summary
        private string _period;
        public string Period
        {
            get => _period;
            set { _period = value; OnPropertyChanged(); }
        }

        private double _averageDailyAttendance;
        public double AverageDailyAttendance
        {
            get => _averageDailyAttendance;
            set { _averageDailyAttendance = value; OnPropertyChanged(); }
        }

        private int _totalLeaveRequests;
        public int TotalLeaveRequests
        {
            get => _totalLeaveRequests;
            set { _totalLeaveRequests = value; OnPropertyChanged(); }
        }

        private string _mostActiveDepartment;
        public string MostActiveDepartment
        {
            get => _mostActiveDepartment;
            set { _mostActiveDepartment = value; OnPropertyChanged(); }
        }

        private string _bestAttendanceDepartment;
        public string BestAttendanceDepartment
        {
            get => _bestAttendanceDepartment;
            set { _bestAttendanceDepartment = value; OnPropertyChanged(); }
        }

        // Date Range
        private DateTime _startDate = DateTime.Today.AddDays(-30);
        public DateTime StartDate
        {
            get => _startDate;
            set { _startDate = value; OnPropertyChanged(); }
        }

        private DateTime _endDate = DateTime.Today;
        public DateTime EndDate
        {
            get => _endDate;
            set { _endDate = value; OnPropertyChanged(); }
        }

        public AdminReportViewModel()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:7129/api/")
            };
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            InitializeEmptyCharts();
        }

        public async Task LoadReportsAsync()
        {
            IsLoading = true;
            LoadingMessage = "Loading reports data...";

            try
            {
                var url = $"reports?startDate={StartDate:yyyy-MM-dd}&endDate={EndDate:yyyy-MM-dd}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var report = await response.Content.ReadFromJsonAsync<ReportResponse>();
                    if (report != null)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            UpdateViewModel(report);
                        });
                    }
                }
                else
                {
                    MessageBox.Show($"Failed to load reports: {response.StatusCode}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading reports: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task ExportAttendancePdfAsync()
        {
            await ExportFileAsync($"reports/export/attendance/pdf?startDate={StartDate:yyyy-MM-dd}&endDate={EndDate:yyyy-MM-dd}",
                $"Attendance_Report_{StartDate:yyyyMMdd}_{EndDate:yyyyMMdd}.pdf", "PDF");
        }

        public async Task ExportLeaveExcelAsync()
        {
            await ExportFileAsync($"reports/export/leave/excel?startDate={StartDate:yyyy-MM-dd}&endDate={EndDate:yyyy-MM-dd}",
                $"Leave_Summary_{StartDate:yyyyMMdd}_{EndDate:yyyyMMdd}.xlsx", "Excel");
        }

        public async Task ExportEmployeeCsvAsync()
        {
            await ExportFileAsync("reports/export/employees/csv",
                $"Employee_List_{DateTime.Today:yyyyMMdd}.csv", "CSV");
        }

        public async Task ExportAnalyticsPdfAsync()
        {
            await ExportFileAsync($"reports/export/analytics/pdf?startDate={StartDate:yyyy-MM-dd}&endDate={EndDate:yyyy-MM-dd}",
                $"Analytics_Report_{StartDate:yyyyMMdd}_{EndDate:yyyyMMdd}.pdf", "PDF");
        }

        private async Task ExportFileAsync(string apiUrl, string fileName, string fileType)
        {
            try
            {
                IsLoading = true;
                LoadingMessage = $"Generating {fileType}...";

                var response = await _httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        SaveFile(fileBytes, fileName, fileType);
                    });
                }
                else
                {
                    MessageBox.Show($"Failed to export {fileType}: {response.StatusCode}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting {fileType}: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void SaveFile(byte[] fileBytes, string fileName, string fileType)
        {
            var saveDialog = new SaveFileDialog
            {
                FileName = fileName,
                Filter = GetFileFilter(fileType)
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllBytes(saveDialog.FileName, fileBytes);
                    MessageBox.Show($"{fileType} file saved successfully: {saveDialog.FileName}", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string GetFileFilter(string fileType)
        {
            return fileType switch
            {
                "PDF" => "PDF Files (*.pdf)|*.pdf",
                "Excel" => "Excel Files (*.xlsx)|*.xlsx",
                "CSV" => "CSV Files (*.csv)|*.csv",
                _ => "All Files (*.*)|*.*"
            };
        }

        private void UpdateViewModel(ReportResponse report)
        {
            // Update KPI stats
            TotalEmployees = report.KpiStats.TotalEmployees;
            TodayAttendancePercentage = report.KpiStats.TodayAttendancePercentage;
            PendingLeaves = report.KpiStats.PendingLeaves;
            AttendanceRate = report.KpiStats.AttendanceRate;

            // Update Quick Summary
            Period = report.QuickSummary.Period;
            AverageDailyAttendance = report.QuickSummary.AverageDailyAttendance;
            TotalLeaveRequests = report.QuickSummary.TotalLeaveRequests;
            MostActiveDepartment = report.QuickSummary.MostActiveDepartment;
            BestAttendanceDepartment = report.QuickSummary.BestAttendanceDepartment;

            // Update charts with real data
            AttendanceTrendModel = CreateAttendanceTrendModel(report.AttendanceTrend);
            LeaveDistributionModel = CreateLeaveDistributionModel(report.LeaveDistribution);
            DepartmentLeaveModel = CreateDepartmentLeaveModel(report.DepartmentLeave);
            PerformanceGaugeModel = CreateGaugeModel(report.PerformanceGauge.AttendancePercentage);
        }

        // Chart creation methods
        private PlotModel CreateAttendanceTrendModel(AttendanceTrend trend)
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
                Title = "Attendance %",
                TitleColor = OxyColors.White,
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

            for (int i = 0; i < trend.Labels.Count && i < trend.Values.Count; i++)
            {
                lineSeries.Points.Add(new DataPoint(i, trend.Values[i]));
                categoryAxis.Labels.Add(trend.Labels[i]);
            }

            model.Series.Add(lineSeries);
            return model;
        }

        private PlotModel CreateLeaveDistributionModel(LeaveDistribution distribution)
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

            pieSeries.Slices.Add(new PieSlice("Approved", distribution.Approved)
            {
                Fill = OxyColor.Parse("#2FBF4C")
            });
            pieSeries.Slices.Add(new PieSlice("Pending", distribution.Pending)
            {
                Fill = OxyColor.Parse("#E3C63A")
            });
            pieSeries.Slices.Add(new PieSlice("Rejected", distribution.Rejected)
            {
                Fill = OxyColor.Parse("#D23C3C")
            });
            pieSeries.Slices.Add(new PieSlice("Cancelled", distribution.Cancelled)
            {
                Fill = OxyColor.Parse("#A95315")
            });

            model.Series.Add(pieSeries);
            return model;
        }

        private PlotModel CreateDepartmentLeaveModel(DepartmentLeave departmentLeave)
        {
            var model = new PlotModel
            {
                PlotAreaBorderColor = OxyColors.Transparent,
                Background = OxyColors.Transparent,
                TextColor = OxyColors.White
            };

            var categoryAxis = new CategoryAxis
            {
                Position = AxisPosition.Left,
                TextColor = OxyColors.White,
                AxislineColor = OxyColors.White,
                TicklineColor = OxyColors.White,
            };

            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = 0,
                MaximumPadding = 0.1,
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

            for (int i = 0; i < departmentLeave.Departments.Count && i < departmentLeave.LeaveCounts.Count; i++)
            {
                barSeries.Items.Add(new BarItem { Value = departmentLeave.LeaveCounts[i] });
                categoryAxis.Labels.Add(departmentLeave.Departments[i]);
            }

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

        private void InitializeEmptyCharts()
        {
            AttendanceTrendModel = new PlotModel
            {
                PlotAreaBorderColor = OxyColors.Transparent,
                Background = OxyColors.Transparent,
                TextColor = OxyColors.White
            };

            LeaveDistributionModel = new PlotModel
            {
                PlotAreaBorderColor = OxyColors.Transparent,
                Background = OxyColors.Transparent,
                TextColor = OxyColors.White
            };

            DepartmentLeaveModel = new PlotModel
            {
                PlotAreaBorderColor = OxyColors.Transparent,
                Background = OxyColors.Transparent,
                TextColor = OxyColors.White
            };

            PerformanceGaugeModel = new PlotModel
            {
                PlotAreaBorderColor = OxyColors.Transparent,
                Background = OxyColors.Transparent,
                TextColor = OxyColors.White
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // DTO classes for deserialization
    public class ReportResponse
    {
        public DateRange DateRange { get; set; } = new();
        public KpiStats KpiStats { get; set; } = new();
        public AttendanceTrend AttendanceTrend { get; set; } = new();
        public LeaveDistribution LeaveDistribution { get; set; } = new();
        public DepartmentLeave DepartmentLeave { get; set; } = new();
        public PerformanceGauge PerformanceGauge { get; set; } = new();
        public QuickSummary QuickSummary { get; set; } = new();
    }

    public class DateRange
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class KpiStats
    {
        public int TotalEmployees { get; set; }
        public double TodayAttendancePercentage { get; set; }
        public int PendingLeaves { get; set; }
        public double AttendanceRate { get; set; }
    }

    public class AttendanceTrend
    {
        public List<string> Labels { get; set; } = new();
        public List<double> Values { get; set; } = new();
    }

    public class LeaveDistribution
    {
        public int Approved { get; set; }
        public int Pending { get; set; }
        public int Rejected { get; set; }
        public int Cancelled { get; set; }
    }

    public class DepartmentLeave
    {
        public List<string> Departments { get; set; } = new();
        public List<int> LeaveCounts { get; set; } = new();
    }

    public class PerformanceGauge
    {
        public double AttendancePercentage { get; set; }
        public double OnTimePercentage { get; set; }
        public double LeavePercentage { get; set; }
    }

    public class QuickSummary
    {
        public string Period { get; set; } = string.Empty;
        public double AverageDailyAttendance { get; set; }
        public int TotalLeaveRequests { get; set; }
        public string MostActiveDepartment { get; set; } = string.Empty;
        public string BestAttendanceDepartment { get; set; } = string.Empty;
    }
}