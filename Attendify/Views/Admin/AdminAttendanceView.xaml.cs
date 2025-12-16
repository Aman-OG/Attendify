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
using System.Windows.Data;

namespace Attendify.Views.UserControls
{
    public partial class AttendanceView : UserControl, INotifyPropertyChanged
    {
        private ObservableCollection<AttendanceRecord> _attendanceRecords;
        private string _currentStatusFilter = "All";
        private HttpClient _httpClient;
        private readonly string _apiBaseUrl = $"{Attendify.Services.HttpClientService.ApiBaseUrl}/attendance";
        private bool _isInitialized = false;
        private bool _isLoading = false;
        private bool _suppressDateFilterEvents = false;
        private DateTime _selectedDate = DateTime.Today;

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
            InitializeHttpClient();
            DataContext = this;
            Loaded += AttendanceView_Loaded;
        }

        private void InitializeHttpClient()
        {
            try
            {
                _httpClient = Attendify.Services.HttpClientService.Instance;
                Console.WriteLine("✅ HttpClient initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error initializing HttpClient: {ex.Message}");
            }
        }

        private async void AttendanceView_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("🏁 AttendanceView_Loaded started");

            Loaded -= AttendanceView_Loaded;

            if (_isInitialized)
            {
                Console.WriteLine("⚠️ Already initialized, skipping...");
                return;
            }

            // Show loading overlay for initial load
            ShowInitialLoadingOverlay();

            // Suppress date filter events during initialization
            _suppressDateFilterEvents = true;

            await Task.Delay(100);

            if (_httpClient == null)
            {
                Console.WriteLine("❌ HttpClient is null");
                HideInitialLoadingOverlay();
                await ShowEmptyTableMessage("Connection not initialized");
                _suppressDateFilterEvents = false;
                return;
            }

            Console.WriteLine("⏳ Loading attendance data...");

            await LoadAttendanceDataAsync();
            _isInitialized = true;
            _suppressDateFilterEvents = false;

            HideInitialLoadingOverlay();
            Console.WriteLine("✅ AttendanceView_Loaded completed");
        }

        private async Task LoadAttendanceDataAsync()
        {
            if (_isLoading)
            {
                Console.WriteLine("⚠️ LoadAttendanceDataAsync already in progress");
                return;
            }

            _isLoading = true;

            try
            {
                await LoadAttendanceAsync();
                await LoadDepartmentsAsync();
                await LoadStatisticsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading data: {ex.Message}");
                await ShowFallbackData();
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task LoadAttendanceAsync()
        {
            if (_httpClient == null)
            {
                Console.WriteLine("❌ HttpClient is null in LoadAttendanceAsync");
                await ShowEmptyTableSilently();
                return;
            }

            try
            {
                Console.WriteLine($"🔄 LoadAttendanceAsync started at {DateTime.Now:HH:mm:ss.fff}");

                var date = DateFilter?.SelectedDate?.Date ?? DateTime.Today;
                if (date > DateTime.Today)
                {
                    date = DateTime.Today;
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        DateFilter.SelectedDate = DateTime.Today;
                    });
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
                var url = $"{_apiBaseUrl}?date={Uri.EscapeDataString(date.ToString("yyyy-MM-dd"))}";
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

                    if (attendanceData == null || !attendanceData.Any())
                    {
                        // Show message for empty data
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (DateMessageText != null)
                            {
                                DateMessageText.Text = $"No attendance records for {date:dd/MM/yyyy}";
                                DateMessageText.Foreground = Brushes.Orange;
                                DateMessageText.Visibility = Visibility.Visible;
                            }
                        });

                        await ShowEmptyTableSilently();
                        return;
                    }

                    // Convert API DTO to our view model
                    var records = attendanceData.Select(dto => new AttendanceRecord
                    {
                        AttendanceID = dto.AttendanceID,
                        EmployeeID = dto.EmployeeID ?? "N/A",
                        FirstName = dto.FirstName ?? "Unknown",
                        MiddleName = dto.MiddleName ?? "",
                        Department = dto.Department ?? "N/A",
                        Position = dto.Position ?? "N/A",
                        Date = dto.Date ?? date.ToString("yyyy-MM-dd"),
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
                        UpdateStatusCounts();

                        // Hide date message if data loaded
                        if (DateMessageText != null)
                        {
                            DateMessageText.Visibility = Visibility.Collapsed;
                        }
                    });

                    Console.WriteLine($"✅ Loaded {records.Count} attendance records");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ API Error ({response.StatusCode}): {error}");
                    await ShowEmptyTableSilently();
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"❌ HTTP Exception: {httpEx.Message}");
                await ShowEmptyTableSilently();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception in LoadAttendanceAsync: {ex.Message}");
                await ShowEmptyTableSilently();
            }

            Console.WriteLine($"✅ LoadAttendanceAsync completed at {DateTime.Now:HH:mm:ss.fff}");
        }

        // Helper methods for loading overlay
        private void ShowInitialLoadingOverlay()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (InitialLoadingOverlay != null)
                {
                    InitialLoadingOverlay.Visibility = Visibility.Visible;
                }
            });
        }

        private void HideInitialLoadingOverlay()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (InitialLoadingOverlay != null)
                {
                    InitialLoadingOverlay.Visibility = Visibility.Collapsed;
                }
            });
        }

        private async Task ShowEmptyTableSilently()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // Clear the table
                AttendanceRecords = new ObservableCollection<AttendanceRecord>();
                AttendanceGrid.ItemsSource = AttendanceRecords;

                // Update status counts to 0
                PresentCount.Text = "0";
                LateCount.Text = "0";
                OnLeaveCount.Text = "0";
                AbsentCount.Text = "0";
            });
        }

        private async Task ShowFallbackData()
        {
            bool apiAvailable = await CheckIfApiIsRunning();

            if (!apiAvailable)
            {
                // Fallback sample data for development
                var fallbackData = new List<AttendanceRecord>
                {
                    new AttendanceRecord
                    {
                        AttendanceID = 1,
                        EmployeeID = "EMP001",
                        FirstName = "John",
                        MiddleName = "Michael",
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
                        MiddleName = "Marie",
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
                        MiddleName = "James",
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

                Console.WriteLine("⚠️ Showing fallback data (API is not running)");
            }
            else
            {
                // API is running, just show empty table
                await ShowEmptyTableMessage("No data available");
            }
        }

        private async Task<bool> CheckIfApiIsRunning()
        {
            try
            {
                if (_httpClient == null) return false;
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task ShowEmptyTableMessage(string message)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // Clear the table
                AttendanceRecords = new ObservableCollection<AttendanceRecord>();
                AttendanceGrid.ItemsSource = AttendanceRecords;

                // Update status counts to 0
                PresentCount.Text = "0";
                LateCount.Text = "0";
                OnLeaveCount.Text = "0";
                AbsentCount.Text = "0";

                // Optionally show a message box
                if (!string.IsNullOrEmpty(message) && !message.Contains("Loading"))
                {
                    MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            });
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
            Console.WriteLine($"📊 LoadDepartmentsAsync called at {DateTime.Now:HH:mm:ss.fff}");

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (DepartmentFilter == null) return;

                    // Use departments from already loaded attendance data
                    var departments = new List<string>();

                    if (AttendanceRecords != null && AttendanceRecords.Any())
                    {
                        departments = AttendanceRecords
                            .Select(r => r.Department)
                            .Where(d => !string.IsNullOrEmpty(d) && d != "N/A")
                            .Distinct()
                            .OrderBy(d => d)
                            .ToList();
                    }

                    if (!departments.Any())
                    {
                        departments.AddRange(new[] { "IT", "HR", "Finance", "Sales", "Marketing" });
                    }

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
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error in LoadDepartmentsAsync: {ex.Message}");
                }
            });

            Console.WriteLine($"📊 LoadDepartmentsAsync completed at {DateTime.Now:HH:mm:ss.fff}");
        }

        private async Task LoadStatisticsAsync()
        {
            Console.WriteLine($"📊 LoadStatisticsAsync called at {DateTime.Now:HH:mm:ss.fff}");

            try
            {
                if (_httpClient == null)
                {
                    Console.WriteLine("⚠️ HttpClient is null in LoadStatisticsAsync");
                    UpdateStatusCounts();
                    return;
                }

                string date = DateTime.Today.ToString("yyyy-MM-dd");
                if (DateFilter != null && DateFilter.SelectedDate.HasValue)
                {
                    date = DateFilter.SelectedDate.Value.ToString("yyyy-MM-dd");
                }

                Console.WriteLine($"📊 Fetching stats for date: {date}");
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

            Console.WriteLine($"📊 LoadStatisticsAsync completed at {DateTime.Now:HH:mm:ss.fff}");
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

        // Helper method for getting selected filter
        private string GetSelectedFilter(ComboBox comboBox, string defaultValue)
        {
            if (comboBox?.SelectedItem == null) return defaultValue;

            if (comboBox.SelectedItem is ComboBoxItem comboBoxItem)
                return comboBoxItem.Content?.ToString() ?? defaultValue;

            return comboBox.SelectedItem.ToString() ?? defaultValue;
        }

        // Client-side filtering method
        private void ApplyAttendanceFilters()
        {
            if (AttendanceRecords == null || AttendanceRecords.Count == 0)
                return;

            // Get the default view of the collection
            var view = CollectionViewSource.GetDefaultView(AttendanceRecords);
            if (view == null)
                return;

            string search = SearchBox?.Text?.Trim().ToLower() ?? "";
            string status = GetSelectedFilter(StatusFilter, "All");
            string department = GetSelectedFilter(DepartmentFilter, "All Departments");
            var date = DateFilter?.SelectedDate;

            view.Filter = recordObj =>
            {
                if (recordObj is not AttendanceRecord record)
                    return false;

                // Date filter
                if (date.HasValue)
                {
                    if (!DateTime.TryParse(record.Date, out var recordDate))
                        return false;

                    if (recordDate.Date != date.Value.Date)
                        return false;
                }

                // Status filter
                if (status != "All" && record.Status != status)
                    return false;

                // Department filter
                if (department != "All Departments" && record.Department != department)
                    return false;

                // Search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    return (record.EmployeeID?.ToLower().Contains(search) ?? false) ||
                           (record.FirstName?.ToLower().Contains(search) ?? false) ||
                           (record.MiddleName?.ToLower().Contains(search) ?? false) ||
                           (record.Department?.ToLower().Contains(search) ?? false) ||
                           (record.Position?.ToLower().Contains(search) ?? false) ||
                           (record.Status?.ToLower().Contains(search) ?? false) ||
                           (record.CheckInTime?.ToLower().Contains(search) ?? false);
                }

                return true;
            };

            view.Refresh();
            UpdateStatusCountsFromFilteredView();
        }

        private void UpdateStatusCountsFromFilteredView()
        {
            var view = CollectionViewSource.GetDefaultView(AttendanceRecords);
            if (view == null) return;

            // Cast the filtered view back to a list
            var filteredList = new List<AttendanceRecord>();
            foreach (var item in view)
            {
                if (item is AttendanceRecord record)
                    filteredList.Add(record);
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                PresentCount.Text = filteredList.Count(r => r.Status == "Present").ToString();
                LateCount.Text = filteredList.Count(r => r.Status == "Late").ToString();
                OnLeaveCount.Text = filteredList.Count(r => r.Status == "On Leave").ToString();
                AbsentCount.Text = filteredList.Count(r => r.Status == "Absent").ToString();
            });
        }

        // Event handlers with client-side filtering
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized) return;

            // Debounce - wait 300ms after last keystroke
            var searchText = SearchBox?.Text;
            await Task.Delay(300);

            // Check if text hasn't changed during the delay
            if (SearchBox?.Text != searchText) return;

            // Apply client-side filters
            ApplyAttendanceFilters();
        }

        private async void DateFilter_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            // Don't process events during initialization
            if (_suppressDateFilterEvents)
            {
                Console.WriteLine("⚠️ Suppressed DateFilter_SelectedDateChanged during initialization");
                return;
            }

            if (!_isInitialized || _isLoading) return;

            if (DateFilter != null && DateFilter.SelectedDate.HasValue)
            {
                _selectedDate = DateFilter.SelectedDate.Value;

                Console.WriteLine($"📅 Date filter changed to: {_selectedDate}");

                // Show loading overlay for date changes
                ShowInitialLoadingOverlay();

                // Show loading state
                if (DateMessageText != null)
                {
                    DateMessageText.Text = "Loading attendance data...";
                    DateMessageText.Foreground = Brushes.Orange;
                    DateMessageText.Visibility = Visibility.Visible;
                }

                await LoadAttendanceAsync();
                await LoadStatisticsAsync();

                HideInitialLoadingOverlay();
            }
        }

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            ApplyAttendanceFilters();
        }

        private void DepartmentFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            ApplyAttendanceFilters();
        }

        private void StatusCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isInitialized) return;

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

                ApplyAttendanceFilters();
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

            // Show loading overlay
            ShowInitialLoadingOverlay();

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
                    if (DateMessageText != null) DateMessageText.Visibility = Visibility.Collapsed;
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
                await Task.Delay(600);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    button.Content = "⟳";
                    button.IsEnabled = true;
                });

                HideInitialLoadingOverlay();
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
        public string MiddleName { get; set; } = "";
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
        public string MiddleName { get; set; } = "";
        public string FullName => $"{FirstName} {MiddleName}".Trim();
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