using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Attendify.Views.UserControls
{
    public partial class EmployeeReportsView : UserControl, INotifyPropertyChanged
    {
        private HttpClient _httpClient;
        private const string ApiBaseUrl = "https://localhost:7129/api";
        private string _currentEmpCode;
        private DispatcherTimer _refreshTimer;
        private bool _isLoading;

        // DTO classes matching API
        public class EmployeeReportStatsDto
        {
            public double AttendanceRate { get; set; }
            public int DaysPresent { get; set; }
            public int LateArrivals { get; set; }
            public int DaysAbsent { get; set; }
            public int LeavesUsed { get; set; }
        }

        public class MonthlyReportDto
        {
            public string Month { get; set; } = null!;
            public int Present { get; set; }
            public int Late { get; set; }
            public int Absent { get; set; }
            public int LeavesApproved { get; set; }
            public double AttendancePercentage { get; set; }
        }

        public class ApiResponseDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = null!;
            public object? Data { get; set; }
        }

        // Properties for data binding
        public List<MonthlyReportDto> MonthlyReports { get; set; } = new List<MonthlyReportDto>();

        // Properties for KPI cards with PropertyChanged
        private string _attendanceRate = "85%";
        public string AttendanceRate
        {
            get => _attendanceRate;
            set
            {
                if (_attendanceRate != value)
                {
                    _attendanceRate = value;
                    OnPropertyChanged(nameof(AttendanceRate));
                }
            }
        }

        private string _daysPresent = "18";
        public string DaysPresent
        {
            get => _daysPresent;
            set
            {
                if (_daysPresent != value)
                {
                    _daysPresent = value;
                    OnPropertyChanged(nameof(DaysPresent));
                }
            }
        }

        private string _lateArrivals = "6";
        public string LateArrivals
        {
            get => _lateArrivals;
            set
            {
                if (_lateArrivals != value)
                {
                    _lateArrivals = value;
                    OnPropertyChanged(nameof(LateArrivals));
                }
            }
        }

        private string _daysAbsent = "2";
        public string DaysAbsent
        {
            get => _daysAbsent;
            set
            {
                if (_daysAbsent != value)
                {
                    _daysAbsent = value;
                    OnPropertyChanged(nameof(DaysAbsent));
                }
            }
        }

        private string _leavesUsed = "3";
        public string LeavesUsed
        {
            get => _leavesUsed;
            set
            {
                if (_leavesUsed != value)
                {
                    _leavesUsed = value;
                    OnPropertyChanged(nameof(LeavesUsed));
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                    UpdateLoadingVisibility();
                }
            }
        }

        // Constructor with empCode parameter
        public EmployeeReportsView(string empCode)
        {
            _currentEmpCode = empCode;
            Console.WriteLine($"EmployeeReportsView created with EmpCode: {empCode}");

            InitializeComponent();
            DataContext = this;

            // Start loading data
            InitializeHttpClient();
            LoadReportsData();

            // Set up auto-refresh timer
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
            _refreshTimer.Tick += (s, e) => LoadReportsData();
            _refreshTimer.Start();
        }

        // Default constructor (for design time)
        public EmployeeReportsView()
        {
            InitializeComponent();
            DataContext = this;
            Console.WriteLine("EmployeeReportsView created without EmpCode (design mode)");
        }

        private void InitializeHttpClient()
        {
            if (_httpClient == null)
            {
                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                _httpClient.Timeout = TimeSpan.FromSeconds(30);
                Console.WriteLine("HttpClient initialized");
            }
        }

        private async void LoadReportsData()
        {
            try
            {
                IsLoading = true;
                Console.WriteLine($"Starting to load reports data for: {_currentEmpCode}");

                if (string.IsNullOrEmpty(_currentEmpCode))
                {
                    Console.WriteLine("Warning: Employee code is empty, showing sample data");
                    ShowDefaultKPICards();
                    ShowSampleMonthlyReport();
                    IsLoading = false;
                    return;
                }

                // Load both data in parallel
                await Task.WhenAll(
                    LoadEmployeeStats(),
                    LoadMonthlyReport()
                );

                Console.WriteLine("Reports data loaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading reports: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                ShowDefaultKPICards();
                ShowSampleMonthlyReport();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadEmployeeStats()
        {
            try
            {
                Console.WriteLine($"Calling API: {ApiBaseUrl}/employeereports/stats/{_currentEmpCode}");

                var response = await _httpClient.GetAsync($"{ApiBaseUrl}/employeereports/stats/{_currentEmpCode}");
                Console.WriteLine($"API Response Status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Response JSON: {json}");

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseDto>(json, options);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        var statsJson = apiResponse.Data.ToString();
                        var stats = JsonSerializer.Deserialize<EmployeeReportStatsDto>(statsJson, options);

                        Console.WriteLine($"Stats received: Rate={stats?.AttendanceRate}, Present={stats?.DaysPresent}");

                        Dispatcher.Invoke(() =>
                        {
                            UpdateKPICards(stats);
                        });
                    }
                    else
                    {
                        Console.WriteLine($"API Response not successful: {apiResponse?.Message}");
                        Dispatcher.Invoke(() => ShowDefaultKPICards());
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Error: {response.StatusCode}, Content: {errorContent}");
                    Dispatcher.Invoke(() => ShowDefaultKPICards());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in LoadEmployeeStats: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Dispatcher.Invoke(() => ShowDefaultKPICards());
            }
        }

        private async Task LoadMonthlyReport()
        {
            try
            {
                Console.WriteLine($"Calling API: {ApiBaseUrl}/employeereports/monthly-report/{_currentEmpCode}");

                var response = await _httpClient.GetAsync($"{ApiBaseUrl}/employeereports/monthly-report/{_currentEmpCode}");
                Console.WriteLine($"Monthly Report API Response Status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseDto>(json, options);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        var reportsJson = apiResponse.Data.ToString();
                        var reports = JsonSerializer.Deserialize<List<MonthlyReportDto>>(reportsJson, options);

                        Console.WriteLine($"Monthly reports received: {reports?.Count} months");

                        Dispatcher.Invoke(() =>
                        {
                            UpdateMonthlyReportGrid(reports ?? new List<MonthlyReportDto>());
                        });
                    }
                    else
                    {
                        Console.WriteLine($"Monthly Report API Response not successful");
                        Dispatcher.Invoke(() => ShowSampleMonthlyReport());
                    }
                }
                else
                {
                    Console.WriteLine($"Monthly Report API Error: {response.StatusCode}");
                    Dispatcher.Invoke(() => ShowSampleMonthlyReport());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in LoadMonthlyReport: {ex.Message}");
                Dispatcher.Invoke(() => ShowSampleMonthlyReport());
            }
        }

        private void UpdateKPICards(EmployeeReportStatsDto stats)
        {
            if (stats == null) return;

            Console.WriteLine($"Updating KPI cards with real data");

            // Update properties that are bound to XAML
            AttendanceRate = $"{stats.AttendanceRate:F1}%";
            DaysPresent = stats.DaysPresent.ToString();
            LateArrivals = stats.LateArrivals.ToString();
            DaysAbsent = stats.DaysAbsent.ToString();
            LeavesUsed = stats.LeavesUsed.ToString();

            Console.WriteLine($"KPI Values: {AttendanceRate}, {DaysPresent}, {LateArrivals}, {DaysAbsent}, {LeavesUsed}");
        }

        private void UpdateMonthlyReportGrid(List<MonthlyReportDto> reports)
        {
            MonthlyReports = reports;
            MonthlyReportGrid.ItemsSource = MonthlyReports;

            // Force refresh of the DataGrid
            MonthlyReportGrid.Items.Refresh();
            Console.WriteLine($"Monthly report grid updated with {reports.Count} items");
        }

        private void ShowDefaultKPICards()
        {
            Console.WriteLine("Showing default KPI cards (sample data)");

            // Set sample values
            AttendanceRate = "85%";
            DaysPresent = "18";
            LateArrivals = "6";
            DaysAbsent = "2";
            LeavesUsed = "3";

            Console.WriteLine($"Sample KPI Values: {AttendanceRate}, {DaysPresent}, {LateArrivals}, {DaysAbsent}, {LeavesUsed}");
        }

        private void ShowSampleMonthlyReport()
        {
            Console.WriteLine("Showing sample monthly report");

            var sampleReports = new List<MonthlyReportDto>
            {
                new MonthlyReportDto { Month = "January 2024", Present = 20, Late = 3, Absent = 2, LeavesApproved = 1, AttendancePercentage = 85.7 },
                new MonthlyReportDto { Month = "February 2024", Present = 18, Late = 4, Absent = 1, LeavesApproved = 2, AttendancePercentage = 82.6 },
                new MonthlyReportDto { Month = "March 2024", Present = 22, Late = 2, Absent = 0, LeavesApproved = 1, AttendancePercentage = 95.7 },
                new MonthlyReportDto { Month = "April 2024", Present = 19, Late = 5, Absent = 3, LeavesApproved = 0, AttendancePercentage = 82.6 },
                new MonthlyReportDto { Month = "May 2024", Present = 21, Late = 3, Absent = 1, LeavesApproved = 2, AttendancePercentage = 91.3 },
                new MonthlyReportDto { Month = "June 2024", Present = 20, Late = 4, Absent = 2, LeavesApproved = 1, AttendancePercentage = 87.0 }
            };

            UpdateMonthlyReportGrid(sampleReports);
        }

        private void UpdateLoadingVisibility()
        {
            Dispatcher.Invoke(() =>
            {
                LoadingOverlay.Visibility = IsLoading ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        // Implement INotifyPropertyChanged for data binding
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}