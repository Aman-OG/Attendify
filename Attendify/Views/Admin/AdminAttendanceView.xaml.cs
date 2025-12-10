using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Attendify.Views.UserControls
{
    public partial class AttendanceView : UserControl, INotifyPropertyChanged
    {
        private ObservableCollection<AttendanceRecord> _attendanceRecords;
        private string _currentStatusFilter = "All";
        private HttpClient _httpClient;
        private string _apiBaseUrl = "https://localhost:7129/api/attendance";
        private DateTime _selectedDate = DateTime.Today;
        private bool _isInitialized = false;

        public ObservableCollection<AttendanceRecord> AttendanceRecords
        {
            get => _attendanceRecords;
            set
            {
                _attendanceRecords = value;
                OnPropertyChanged();
                UpdateStatusCounts();
            }
        }

        public AttendanceView()
        {
            InitializeComponent();

            // Initialize HttpClient synchronously in constructor
            InitializeHttpClient();

            DataContext = this;

            // Don't load data here - wait for controls to initialize
            Loaded += AttendanceView_Loaded;

            // Initialize date to today
            if (DateFilter != null)
            {
                DateFilter.SelectedDate = DateTime.Today;
            }
        }

        private void InitializeHttpClient()
        {
            try
            {
                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.Timeout = TimeSpan.FromSeconds(30);

                Console.WriteLine("✅ HttpClient initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error initializing HttpClient: {ex.Message}");
                // Don't show message box here
            }
        }

        private async void AttendanceView_Loaded(object sender, RoutedEventArgs e)
        {
            // Unsubscribe to prevent multiple loads
            Loaded -= AttendanceView_Loaded;

            if (_isInitialized) return;

            // Wait a moment for controls to initialize
            await Task.Delay(100);

            // Check if HttpClient was initialized
            if (_httpClient == null)
            {
                Console.WriteLine("❌ HttpClient is null");
                await ShowFallbackData();
                return;
            }

            await LoadAttendanceDataAsync();
            _isInitialized = true;
        }

        private async Task LoadAttendanceDataAsync()
        {
            try
            {
                // Load attendance data
                await LoadAttendanceAsync();

                // Load departments for filter
                await LoadDepartmentsAsync();

                // Load statistics
                await LoadStatisticsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading data: {ex.Message}");
                await ShowFallbackData();
            }
        }

        private async Task LoadAttendanceAsync()
        {
            try
            {
                if (_httpClient == null)
                {
                    Console.WriteLine("❌ HttpClient is null in LoadAttendanceAsync");
                    await ShowFallbackData();
                    return;
                }

                // Get filter values with null checks
                string date = DateTime.Today.ToString("yyyy-MM-dd");
                if (DateFilter != null && DateFilter.SelectedDate.HasValue)
                {
                    date = DateFilter.SelectedDate.Value.ToString("yyyy-MM-dd");
                }

                string status = "All";
                if (StatusFilter != null && StatusFilter.SelectedItem != null)
                {
                    if (StatusFilter.SelectedItem is ComboBoxItem statusItem)
                    {
                        status = statusItem.Content?.ToString() ?? "All";
                    }
                    else if (StatusFilter.SelectedItem is string statusStr)
                    {
                        status = statusStr;
                    }
                }

                string department = "All Departments";
                if (DepartmentFilter != null && DepartmentFilter.SelectedItem != null)
                {
                    if (DepartmentFilter.SelectedItem is ComboBoxItem deptItem)
                    {
                        department = deptItem.Content?.ToString() ?? "All Departments";
                    }
                    else if (DepartmentFilter.SelectedItem is string deptStr)
                    {
                        department = deptStr;
                    }
                }

                var search = SearchBox?.Text ?? "";

                // Build URL
                var url = $"{_apiBaseUrl}?date={Uri.EscapeDataString(date)}";
                if (!string.IsNullOrEmpty(status) && status != "All")
                {
                    url += $"&status={Uri.EscapeDataString(status)}";
                }
                if (!string.IsNullOrEmpty(department) && department != "All Departments")
                {
                    url += $"&department={Uri.EscapeDataString(department)}";
                }
                if (!string.IsNullOrEmpty(search))
                {
                    url += $"&search={Uri.EscapeDataString(search)}";
                }

                Console.WriteLine($"🌐 Calling API: {url}");

                var response = await _httpClient.GetAsync(url);
                Console.WriteLine($"📡 Response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var attendanceData = await response.Content.ReadFromJsonAsync<List<AttendanceApiDto>>();

                    if (attendanceData == null)
                    {
                        attendanceData = new List<AttendanceApiDto>();
                    }

                    // Convert API DTO to our view model
                    var records = attendanceData.Select(dto => new AttendanceRecord
                    {
                        AttendanceID = dto.AttendanceID,
                        EmployeeID = dto.EmployeeID ?? "N/A",
                        FirstName = dto.FirstName ?? "Unknown",
                        LastName = dto.LastName ?? "Unknown",
                        Department = dto.Department ?? "N/A",
                        Position = dto.Position ?? "N/A",
                        Date = dto.Date ?? DateTime.Today.ToString("yyyy-MM-dd"),
                        Status = dto.Status ?? "Absent",
                        StatusColor = dto.StatusColor ?? "#DC3545",
                        StatusBrush = ConvertColorStringToBrush(dto.StatusColor),
                        CheckInTime = dto.CheckInTime ?? "N/A"
                    }).ToList();

                    // Update on UI thread
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        AttendanceRecords = new ObservableCollection<AttendanceRecord>(records);
                        AttendanceGrid.ItemsSource = AttendanceRecords;
                    });

                    Console.WriteLine($"✅ Loaded {records.Count} attendance records");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ API Error ({response.StatusCode}): {error}");

                    // Show fallback data instead of error message
                    await ShowFallbackData();
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"❌ HTTP Exception: {httpEx.Message}");
                await ShowFallbackData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception in LoadAttendanceAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await ShowFallbackData();
            }
        }

        private async Task ShowFallbackData()
        {
            // Fallback sample data for development
            var fallbackData = new List<AttendanceRecord>
            {
                new AttendanceRecord
                {
                    AttendanceID = 1,
                    EmployeeID = "EMP001",
                    FirstName = "John",
                    LastName = "Doe",
                    Department = "IT",
                    Position = "Developer",
                    Date = DateTime.Today.ToString("yyyy-MM-dd"),
                    Status = "Present",
                    StatusColor = "#28A745",
                    StatusBrush = new SolidColorBrush(Colors.Green),
                    CheckInTime = "09:00"
                },
                new AttendanceRecord
                {
                    AttendanceID = 2,
                    EmployeeID = "EMP002",
                    FirstName = "Jane",
                    LastName = "Smith",
                    Department = "HR",
                    Position = "Manager",
                    Date = DateTime.Today.ToString("yyyy-MM-dd"),
                    Status = "Late",
                    StatusColor = "#FFC107",
                    StatusBrush = new SolidColorBrush(Colors.Orange),
                    CheckInTime = "09:30"
                },
                new AttendanceRecord
                {
                    AttendanceID = 3,
                    EmployeeID = "EMP003",
                    FirstName = "Bob",
                    LastName = "Johnson",
                    Department = "Finance",
                    Position = "Analyst",
                    Date = DateTime.Today.ToString("yyyy-MM-dd"),
                    Status = "On Leave",
                    StatusColor = "#007BFF",
                    StatusBrush = new SolidColorBrush(Colors.Blue),
                    CheckInTime = "N/A"
                }
            };

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                AttendanceRecords = new ObservableCollection<AttendanceRecord>(fallbackData);
                AttendanceGrid.ItemsSource = AttendanceRecords;
                UpdateStatusCounts();
            });

            Console.WriteLine("⚠️ Showing fallback data (API might not be running)");
        }

        private Brush ConvertColorStringToBrush(string colorString)
        {
            if (string.IsNullOrEmpty(colorString))
                return new SolidColorBrush(Colors.Gray);

            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorString));
            }
            catch
            {
                return new SolidColorBrush(Colors.Gray);
            }
        }

        private async Task LoadDepartmentsAsync()
        {
            try
            {
                if (_httpClient == null || DepartmentFilter == null)
                {
                    Console.WriteLine("⚠️ HttpClient or DepartmentFilter is null in LoadDepartmentsAsync");
                    SetDefaultDepartments();
                    return;
                }

                // Simple departments for fallback
                var defaultDepartments = new List<string> { "IT", "HR", "Finance", "Sales", "Marketing" };

                try
                {
                    var response = await _httpClient.GetAsync($"{_apiBaseUrl}");
                    if (response.IsSuccessStatusCode)
                    {
                        var attendanceData = await response.Content.ReadFromJsonAsync<List<AttendanceApiDto>>();
                        if (attendanceData != null)
                        {
                            var departments = attendanceData
                                .Select(a => a.Department)
                                .Where(d => !string.IsNullOrEmpty(d) && d != "N/A")
                                .Distinct()
                                .OrderBy(d => d)
                                .ToList();

                            if (departments.Any())
                            {
                                UpdateDepartmentFilter(departments);
                                return;
                            }
                        }
                    }
                }
                catch
                {
                    // If API call fails, use default departments
                }

                // Use default departments
                SetDefaultDepartments();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in LoadDepartmentsAsync: {ex.Message}");
                // Set default departments on error
                SetDefaultDepartments();
            }
        }

        private void SetDefaultDepartments()
        {
            var defaultDepartments = new List<string> { "IT", "HR", "Finance", "Sales", "Marketing" };
            UpdateDepartmentFilter(defaultDepartments);
        }

        private void UpdateDepartmentFilter(List<string> departments)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (DepartmentFilter != null)
                {
                    DepartmentFilter.Items.Clear();
                    DepartmentFilter.Items.Add(new ComboBoxItem { Content = "All Departments" });

                    foreach (var dept in departments)
                    {
                        if (!string.IsNullOrEmpty(dept))
                        {
                            DepartmentFilter.Items.Add(new ComboBoxItem { Content = dept });
                        }
                    }

                    DepartmentFilter.SelectedIndex = 0;
                    Console.WriteLine($"✅ Loaded {departments.Count} departments");
                }
            });
        }

        private async Task LoadStatisticsAsync()
        {
            try
            {
                if (_httpClient == null)
                {
                    Console.WriteLine("⚠️ HttpClient is null in LoadStatisticsAsync");
                    UpdateStatusCounts(); // Update from local data
                    return;
                }

                string date = DateTime.Today.ToString("yyyy-MM-dd");
                if (DateFilter != null && DateFilter.SelectedDate.HasValue)
                {
                    date = DateFilter.SelectedDate.Value.ToString("yyyy-MM-dd");
                }

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/stats?date={Uri.EscapeDataString(date)}");

                if (response.IsSuccessStatusCode)
                {
                    var stats = await response.Content.ReadFromJsonAsync<AttendanceStats>();
                    if (stats != null)
                    {
                        UpdateStatusCountsFromStats(stats);
                        Console.WriteLine($"📊 Stats: P={stats.Present}, L={stats.Late}, OL={stats.OnLeave}, A={stats.Absent}");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error in LoadStatisticsAsync: {ex.Message}");
            }

            // Fallback to local counts
            UpdateStatusCounts();
        }

        private void UpdateStatusCounts()
        {
            if (AttendanceRecords == null) return;

            var presentCount = AttendanceRecords.Count(r => r.Status == "Present");
            var lateCount = AttendanceRecords.Count(r => r.Status == "Late");
            var onLeaveCount = AttendanceRecords.Count(r => r.Status == "On Leave");
            var absentCount = AttendanceRecords.Count(r => r.Status == "Absent");

            Application.Current.Dispatcher.Invoke(() =>
            {
                PresentCount.Text = presentCount.ToString();
                LateCount.Text = lateCount.ToString();
                OnLeaveCount.Text = onLeaveCount.ToString();
                AbsentCount.Text = absentCount.ToString();
            });
        }

        private void UpdateStatusCountsFromStats(AttendanceStats stats)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                PresentCount.Text = stats.Present.ToString();
                LateCount.Text = stats.Late.ToString();
                OnLeaveCount.Text = stats.OnLeave.ToString();
                AbsentCount.Text = stats.Absent.ToString();
            });
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Update placeholder visibility with null check
            if (SearchPlaceholder != null)
            {
                SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox?.Text) ?
                    Visibility.Visible : Visibility.Collapsed;
            }

            // Debounce search - wait 300ms after last keystroke
            await Task.Delay(300);

            // Check if user is still typing
            if (SearchBox?.IsKeyboardFocused == true)
            {
                await LoadAttendanceAsync();
            }
        }

        private async void DateFilter_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DateFilter != null && DateFilter.SelectedDate.HasValue)
            {
                _selectedDate = DateFilter.SelectedDate.Value;
                await LoadAttendanceAsync();
                await LoadStatisticsAsync();
            }
        }

        private async void DepartmentFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DepartmentFilter != null && DepartmentFilter.SelectedItem != null)
            {
                await Task.Delay(100); // Small delay for UI responsiveness
                await LoadAttendanceAsync();
            }
        }

        private async void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StatusFilter != null && StatusFilter.SelectedItem != null)
            {
                await Task.Delay(100); // Small delay for UI responsiveness
                await LoadAttendanceAsync();
            }
        }

        private async void StatusCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Reset all cards
            ResetStatusCards();

            var card = sender as Border;
            if (card != null)
            {
                // Highlight selected card
                var color = card.Name switch
                {
                    "PresentCard" => "#6028A745",
                    "LateCard" => "#60FFC107",
                    "OnLeaveCard" => "#60007BFF",
                    "AbsentCard" => "#60DC3545",
                    _ => "#6000A6FB"
                };

                try
                {
                    card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
                }
                catch
                {
                    card.Background = new SolidColorBrush(Color.FromArgb(0x60, 0x00, 0xA6, 0xFB));
                }

                // Set filter based on card
                if (card == PresentCard)
                    _currentStatusFilter = "Present";
                else if (card == LateCard)
                    _currentStatusFilter = "Late";
                else if (card == OnLeaveCard)
                    _currentStatusFilter = "On Leave";
                else if (card == AbsentCard)
                    _currentStatusFilter = "Absent";
                else
                    _currentStatusFilter = "All";

                // Update status filter combobox if it exists
                if (StatusFilter != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var item in StatusFilter.Items)
                        {
                            if (item is ComboBoxItem comboItem && comboItem.Content?.ToString() == _currentStatusFilter)
                            {
                                StatusFilter.SelectedItem = item;
                                break;
                            }
                        }
                    });
                }
            }
        }

        private void ResetStatusCards()
        {
            try
            {
                PresentCard.Background = new SolidColorBrush(Color.FromArgb(0x20, 0x28, 0xA7, 0x45));
                LateCard.Background = new SolidColorBrush(Color.FromArgb(0x20, 0xFF, 0xC1, 0x07));
                OnLeaveCard.Background = new SolidColorBrush(Color.FromArgb(0x20, 0x00, 0x7B, 0xFF));
                AbsentCard.Background = new SolidColorBrush(Color.FromArgb(0x20, 0xDC, 0x35, 0x45));
                _currentStatusFilter = "All";
            }
            catch
            {
                // Ignore errors in resetting cards
            }
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            // Show loading state
            button.Content = "⏳";
            button.IsEnabled = false;

            try
            {
                // Clear filters
                ResetStatusCards();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (StatusFilter != null) StatusFilter.SelectedIndex = 0;
                    if (DepartmentFilter != null) DepartmentFilter.SelectedIndex = 0;
                    if (SearchBox != null) SearchBox.Text = "";
                    if (DateFilter != null) DateFilter.SelectedDate = DateTime.Today;
                });

                // Refresh data
                await LoadAttendanceDataAsync();

                // Show refresh animation
                var animation = new System.Windows.Media.Animation.DoubleAnimation(360, 0,
                    new System.Windows.Duration(TimeSpan.FromSeconds(0.5)));
                var rotateTransform = new RotateTransform();
                button.RenderTransform = rotateTransform;
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, animation);
            }
            finally
            {
                // Restore button state
                await Task.Delay(600); // Wait for animation to complete
                Application.Current.Dispatcher.Invoke(() =>
                {
                    button.Content = "⟳";
                    button.IsEnabled = true;
                });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // API DTO (for JSON deserialization)
    public class AttendanceApiDto
    {
        public int AttendanceID { get; set; }
        public string EmployeeID { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Department { get; set; } = "";
        public string Position { get; set; } = "";
        public string Date { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusColor { get; set; } = ""; // Hex color string from API
        public string CheckInTime { get; set; } = "";
    }

    // View Model (for WPF binding)
    public class AttendanceRecord
    {
        public int AttendanceID { get; set; }
        public string EmployeeID { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string FullName => $"{FirstName} {LastName}";
        public string Department { get; set; } = "";
        public string Position { get; set; } = "";
        public string Date { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusColor { get; set; } = ""; // Hex color string from API
        public Brush StatusBrush { get; set; } = Brushes.Gray; // Brush property for WPF
        public string CheckInTime { get; set; } = "";
    }

    public class AttendanceStats
    {
        public string Date { get; set; } = "";
        public int TotalEmployees { get; set; }
        public int Present { get; set; }
        public int Late { get; set; }
        public int OnLeave { get; set; }
        public int HalfDay { get; set; }
        public int Absent { get; set; }
    }
}