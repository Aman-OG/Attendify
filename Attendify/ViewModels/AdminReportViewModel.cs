using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

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
                // Update command availability
                ExportAttendancePdfCommand.RaiseCanExecuteChanged();
                ExportLeaveExcelCommand.RaiseCanExecuteChanged();
                ExportEmployeeCsvCommand.RaiseCanExecuteChanged();
                ExportAnalyticsPdfCommand.RaiseCanExecuteChanged();
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

        // Commands
        private AsyncRelayCommand _exportAttendancePdfCommand;
        public AsyncRelayCommand ExportAttendancePdfCommand =>
            _exportAttendancePdfCommand ??= new AsyncRelayCommand(ExportAttendancePdfAsync, () => !IsLoading);

        private AsyncRelayCommand _exportLeaveExcelCommand;
        public AsyncRelayCommand ExportLeaveExcelCommand =>
            _exportLeaveExcelCommand ??= new AsyncRelayCommand(ExportLeaveExcelAsync, () => !IsLoading);

        private AsyncRelayCommand _exportEmployeeCsvCommand;
        public AsyncRelayCommand ExportEmployeeCsvCommand =>
            _exportEmployeeCsvCommand ??= new AsyncRelayCommand(ExportEmployeeCsvAsync, () => !IsLoading);

        private AsyncRelayCommand _exportAnalyticsPdfCommand;
        public AsyncRelayCommand ExportAnalyticsPdfCommand =>
            _exportAnalyticsPdfCommand ??= new AsyncRelayCommand(ExportAnalyticsPdfAsync, () => !IsLoading);

        public AdminReportViewModel()
        {
            _httpClient = Attendify.Services.HttpClientService.Instance;

            InitializeEmptyCharts();
        }

        public async Task LoadReportsAsync()
        {
            IsLoading = true;
            LoadingMessage = "Loading reports data...";

            try
            {
                var url = $"reports?startDate={StartDate:yyyy-MM-dd}&endDate={EndDate:yyyy-MM-dd}";
                Debug.WriteLine($"API URL: {url}");

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Response: {json}");

                    var report = await response.Content.ReadFromJsonAsync<ReportResponse>();
                    if (report != null)
                    {
                        Debug.WriteLine($"Leave Distribution: Approved={report.LeaveDistribution.Approved}, " +
                                       $"Pending={report.LeaveDistribution.Pending}, " +
                                       $"Rejected={report.LeaveDistribution.Rejected}, " +
                                       $"Cancelled={report.LeaveDistribution.Cancelled}");

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            UpdateViewModel(report);
                        });
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Error: {response.StatusCode} - {error}");

                    Attendify.Views.GlassMessageBox.Show($"Failed to load reports: {response.StatusCode}\n{error}", "Error", false, Attendify.Views.GlassMessageBox.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception: {ex}");
                Attendify.Views.GlassMessageBox.Show($"Error loading reports: {ex.Message}", "Error", false, Attendify.Views.GlassMessageBox.MessageType.Error);
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

                Debug.WriteLine($"Export URL: {apiUrl}");

                var response = await _httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    Debug.WriteLine($"Received {fileBytes.Length} bytes for {fileType}");

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        SaveFile(fileBytes, fileName, fileType);
                    });
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Export Error: {response.StatusCode} - {error}");

                    Attendify.Views.GlassMessageBox.Show($"Failed to export {fileType}: {response.ReasonPhrase}\n{error}", "Error", false, Attendify.Views.GlassMessageBox.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Export Exception: {ex}");
                Attendify.Views.GlassMessageBox.Show($"Error exporting {fileType}: {ex.Message}", "Error", false, Attendify.Views.GlassMessageBox.MessageType.Error);
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
                    Debug.WriteLine($"File saved: {saveDialog.FileName}");
                    Attendify.Views.GlassMessageBox.Show($"{fileType} file saved successfully!", "Success", false, Attendify.Views.GlassMessageBox.MessageType.Success);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Save Error: {ex}");
                    Attendify.Views.GlassMessageBox.Show($"Error saving file: {ex.Message}", "Error", false, Attendify.Views.GlassMessageBox.MessageType.Error);
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
            try
            {
                // Update KPI stats
                TotalEmployees = report.KpiStats.TotalEmployees;
                TodayAttendancePercentage = report.KpiStats.TodayAttendancePercentage;
                PendingLeaves = report.KpiStats.PendingLeaves;
                AttendanceRate = report.KpiStats.AttendanceRate;

                Debug.WriteLine($"KPI Stats: TotalEmployees={TotalEmployees}, " +
                              $"TodayAttendance={TodayAttendancePercentage}%, " +
                              $"PendingLeaves={PendingLeaves}, " +
                              $"AttendanceRate={AttendanceRate}%");

                // Update Quick Summary
                Period = report.QuickSummary.Period;
                AverageDailyAttendance = report.QuickSummary.AverageDailyAttendance;
                TotalLeaveRequests = report.QuickSummary.TotalLeaveRequests;
                MostActiveDepartment = report.QuickSummary.MostActiveDepartment;
                BestAttendanceDepartment = report.QuickSummary.BestAttendanceDepartment;

                Debug.WriteLine($"Quick Summary: Period={Period}, " +
                              $"AvgAttendance={AverageDailyAttendance}%, " +
                              $"TotalLeaves={TotalLeaveRequests}, " +
                              $"MostActiveDept={MostActiveDepartment}, " +
                              $"BestAttendanceDept={BestAttendanceDepartment}");

                // Debug leave distribution
                Debug.WriteLine($"Leave Distribution: Approved={report.LeaveDistribution.Approved}, " +
                               $"Pending={report.LeaveDistribution.Pending}, " +
                               $"Rejected={report.LeaveDistribution.Rejected}, " +
                               $"Cancelled={report.LeaveDistribution.Cancelled}");

                // Debug department leave
                if (report.DepartmentLeave.Departments != null)
                {
                    Debug.WriteLine($"Department Leave has {report.DepartmentLeave.Departments.Count} departments");
                    for (int i = 0; i < report.DepartmentLeave.Departments.Count; i++)
                    {
                        Debug.WriteLine($"  {report.DepartmentLeave.Departments[i]}: {report.DepartmentLeave.LeaveCounts[i]} leaves");
                    }
                }

                // Debug Performance Gauge data - ADD THIS
                Debug.WriteLine($"Performance Gauge: Attendance={report.PerformanceGauge.AttendancePercentage}%, " +
                              $"OnTime={report.PerformanceGauge.OnTimePercentage}%, " +
                              $"Leave={report.PerformanceGauge.LeavePercentage}%");

                // If PerformanceGauge data is 0, use AttendanceRate instead
                double gaugeValue = report.PerformanceGauge.AttendancePercentage;
                if (gaugeValue <= 0)
                {
                    gaugeValue = AttendanceRate; // Fall back to KPI AttendanceRate
                    Debug.WriteLine($"Using fallback gauge value: {gaugeValue}% (from AttendanceRate)");
                }

                // Ensure we have a visible value (minimum 10% so it shows something)
                if (gaugeValue <= 0)
                {
                    gaugeValue = 50; // Default value for testing
                    Debug.WriteLine($"Using default gauge value: {gaugeValue}% for visibility");
                }

                // Update charts with real data
                AttendanceTrendModel = CreateAttendanceTrendModel(report.AttendanceTrend);
                LeaveDistributionModel = CreateLeaveDistributionModel(report.LeaveDistribution);
                DepartmentLeaveModel = CreateDepartmentLeaveModel(report.DepartmentLeave);
                PerformanceGaugeModel = CreateGaugeModel(gaugeValue);

                // Force UI update
                OnPropertyChanged(nameof(Period));
                OnPropertyChanged(nameof(AverageDailyAttendance));
                OnPropertyChanged(nameof(TotalLeaveRequests));
                OnPropertyChanged(nameof(MostActiveDepartment));
                OnPropertyChanged(nameof(BestAttendanceDepartment));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateViewModel: {ex.Message}");
                Attendify.Views.GlassMessageBox.Show($"Error updating view: {ex.Message}", "Error", false, Attendify.Views.GlassMessageBox.MessageType.Error);
            }
        }
        // Chart creation methods
        private PlotModel CreateAttendanceTrendModel(AttendanceTrend trend)
        {
            var model = new PlotModel
            {
                Title = "Weekly Attendance Trend",
                PlotAreaBorderColor = OxyColors.Transparent,
                Background = OxyColors.Transparent,
                TextColor = OxyColors.White,
                TitleColor = OxyColors.White
            };

            if (trend.Labels == null || trend.Values == null || trend.Labels.Count == 0)
            {
                // Create dummy data for testing
                trend.Labels = new List<string> { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
                trend.Values = new List<double> { 85, 90, 88, 92, 95, 30, 25 };
            }

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
                Title = "Leave Status Distribution",
                PlotAreaBorderColor = OxyColors.Transparent,
                Background = OxyColors.Transparent,
                TextColor = OxyColors.White,
                TitleColor = OxyColors.White
            };

            var pieSeries = new PieSeries
            {
                InsideLabelColor = OxyColors.White,
                InsideLabelPosition = 0.7,
                FontSize = 12,
                StrokeThickness = 2,
                Stroke = OxyColors.White,
                OutsideLabelFormat = "{0}: {1}",
                StartAngle = 0,
                AngleSpan = 360
            };

            // Check if we have any data
            bool hasData = distribution.Approved > 0 || distribution.Pending > 0 ||
                           distribution.Rejected > 0 || distribution.Cancelled > 0;

            if (hasData)
            {
                // Add slices only if they have value
                if (distribution.Approved > 0)
                    pieSeries.Slices.Add(new PieSlice("Approved", distribution.Approved)
                    {
                        Fill = OxyColor.Parse("#2FBF4C")
                    });

                if (distribution.Pending > 0)
                    pieSeries.Slices.Add(new PieSlice("Pending", distribution.Pending)
                    {
                        Fill = OxyColor.Parse("#E3C63A")
                    });

                if (distribution.Rejected > 0)
                    pieSeries.Slices.Add(new PieSlice("Rejected", distribution.Rejected)
                    {
                        Fill = OxyColor.Parse("#D23C3C")
                    });

                if (distribution.Cancelled > 0)
                    pieSeries.Slices.Add(new PieSlice("Cancelled", distribution.Cancelled)
                    {
                        Fill = OxyColor.Parse("#A95315")
                    });
            }
            else
            {
                // Show a single slice indicating no data
                pieSeries.Slices.Add(new PieSlice("No Leave Data", 1)
                {
                    Fill = OxyColors.Gray
                });
                pieSeries.InsideLabelFormat = "{0}";
            }

            model.Series.Add(pieSeries);
            return model;
        }

        private PlotModel CreateDepartmentLeaveModel(DepartmentLeave departmentLeave)
        {
            var model = new PlotModel
            {
                Title = "Department Leave Comparison",
                PlotAreaBorderColor = OxyColors.Transparent,
                Background = OxyColors.Transparent,
                TextColor = OxyColors.White,
                TitleColor = OxyColors.White
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
                LabelPlacement = LabelPlacement.Inside,
                LabelFormatString = "{0}"
            };

            // Check if we have real data
            if (departmentLeave.Departments != null && departmentLeave.LeaveCounts != null &&
                departmentLeave.Departments.Count > 0)
            {
                for (int i = 0; i < departmentLeave.Departments.Count && i < departmentLeave.LeaveCounts.Count; i++)
                {
                    barSeries.Items.Add(new BarItem { Value = departmentLeave.LeaveCounts[i] });
                    categoryAxis.Labels.Add(departmentLeave.Departments[i]);
                }

                // Auto-adjust maximum based on data
                if (departmentLeave.LeaveCounts.Count > 0)
                {
                    valueAxis.Maximum = departmentLeave.LeaveCounts.Max() * 1.2; // Add 20% padding
                }
            }
            else
            {
                // Add some sample data for visualization
                barSeries.Items.Add(new BarItem { Value = 5 });
                barSeries.Items.Add(new BarItem { Value = 3 });
                barSeries.Items.Add(new BarItem { Value = 2 });

                categoryAxis.Labels.Add("IT");
                categoryAxis.Labels.Add("HR");
                categoryAxis.Labels.Add("Finance");

                model.Title = "Department Leave (Sample Data)";
            }

            model.Series.Add(barSeries);
            return model;
        }

        private PlotModel CreateGaugeModel(double value)
        {
            var model = new PlotModel
            {
                Title = "Attendance Rate",
                PlotAreaBorderColor = OxyColors.Transparent,
                Background = OxyColors.Transparent,
                TextColor = OxyColors.White,
                TitleColor = OxyColors.White
            };

            // Ensure value is between 0 and 100
            value = Math.Max(0, Math.Min(100, value));

            var gaugeSeries = new PieSeries
            {
                StrokeThickness = 2,
                Stroke = OxyColors.White,
                InsideLabelColor = OxyColors.White,
                FontSize = 14,
                FontWeight = OxyPlot.FontWeights.Bold,
                StartAngle = 270,
                AngleSpan = 360,
                InsideLabelFormat = $"{value:F1}%",
                InsideLabelPosition = 0.5
            };

            // Main attendance slice
            gaugeSeries.Slices.Add(new PieSlice("", value)
            {
                Fill = OxyColor.Parse("#00A6FB")
            });

            // Remaining slice (darker to show contrast)
            gaugeSeries.Slices.Add(new PieSlice("", 100 - value)
            {
                Fill = OxyColor.FromArgb(60, 100, 100, 100) // Semi-transparent
            });

            model.Series.Add(gaugeSeries);
            return model;
        }

        private void InitializeEmptyCharts()
        {
            AttendanceTrendModel = CreateAttendanceTrendModel(new AttendanceTrend());
            LeaveDistributionModel = CreateLeaveDistributionModel(new LeaveDistribution());
            DepartmentLeaveModel = CreateDepartmentLeaveModel(new DepartmentLeave());
            PerformanceGaugeModel = CreateGaugeModel(0);
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