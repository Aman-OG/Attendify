using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Attendify.Views.Employee
{
    public partial class EmployeeLeaveRequestsView : UserControl
    {
        private HttpClient _httpClient;
        // private const string ApiBaseUrl = "https://localhost:7129/api";
        private string _currentEmpCode;
        private DispatcherTimer _refreshTimer;

        // DTO classes matching API
        public class LeaveResponseDto
        {
            public int LeaveId { get; set; }
            public string FromDate { get; set; } = null!;
            public string ToDate { get; set; } = null!;
            public string ReasonTitle { get; set; } = null!;
            public string? DetailedReason { get; set; }
            public string Status { get; set; } = null!;
            public string? AdminResponse { get; set; }
            public string CreatedAt { get; set; } = null!;
            public bool CanCancel { get; set; }
            public string StatusColor { get; set; } = "#FF9800";
        }

        public class ApiResponseDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = null!;
            public object? Data { get; set; }
        }

        public class LeaveStatsDto
        {
            public int Pending { get; set; }
            public int Approved { get; set; }
            public int Rejected { get; set; }
        }

        public class LeaveRequestDto
        {
            public string EmpCode { get; set; } = null!;
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public string ReasonTitle { get; set; } = null!;
            public string? DetailedReason { get; set; }
        }

        public class CancelLeaveDto
        {
            public int LeaveId { get; set; }
        }

        // Constructor with empCode parameter
        public EmployeeLeaveRequestsView(string empCode)
        {
            InitializeComponent();
            _currentEmpCode = empCode;
            Loaded += EmployeeLeaveRequestsView_Loaded;
        }

        private void EmployeeLeaveRequestsView_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeHttpClient();
            LoadLeaveData();

            // Set up auto-refresh timer (every 30 seconds)
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _refreshTimer.Tick += (s, e) => LoadLeaveData();
            _refreshTimer.Start();
        }

        private void InitializeHttpClient()
        {
            if (_httpClient == null)
            {
                _httpClient = Attendify.Services.HttpClientService.Instance;
            }
        }

        private async void LoadLeaveData()
        {
            try
            {
                await LoadLeaveStats();
                await LoadLeaveRequests();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading leave data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadLeaveStats()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{Attendify.Services.HttpClientService.ApiBaseUrl}/employeeleave/stats/{_currentEmpCode}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseDto>(json, options);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        var statsJson = apiResponse.Data.ToString();
                        var stats = JsonSerializer.Deserialize<LeaveStatsDto>(statsJson, options);

                        Dispatcher.Invoke(() =>
                        {
                            PendingCount.Text = stats?.Pending.ToString() ?? "0";
                            ApprovedCount.Text = stats?.Approved.ToString() ?? "0";
                            RejectedCount.Text = stats?.Rejected.ToString() ?? "0";
                        });
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error loading stats. Status: {response.StatusCode}, Content: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading stats: {ex.Message}");
            }
        }

        private async Task LoadLeaveRequests()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{Attendify.Services.HttpClientService.ApiBaseUrl}/employeeleave/requests/{_currentEmpCode}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseDto>(json, options);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        var leavesJson = apiResponse.Data.ToString();
                        var leaves = JsonSerializer.Deserialize<List<LeaveResponseDto>>(leavesJson, options);

                        Dispatcher.Invoke(() =>
                        {
                            UpdateLeaveHistoryGrid(leaves ?? new List<LeaveResponseDto>());
                        });
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error loading requests. Status: {response.StatusCode}, Content: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading leave requests: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateLeaveHistoryGrid(List<LeaveResponseDto> leaves)
        {
            var gridItems = new List<LeaveGridItem>();
            foreach (var leave in leaves)
            {
                gridItems.Add(new LeaveGridItem
                {
                    LeaveId = leave.LeaveId,
                    FromDate = leave.FromDate,
                    ToDate = leave.ToDate,
                    ReasonTitle = leave.ReasonTitle,
                    Status = leave.Status,
                    CanCancel = leave.CanCancel,
                    StatusColor = GetBrushFromColor(leave.StatusColor)
                });
            }

            LeaveHistoryGrid.ItemsSource = gridItems;
        }

        private Brush GetBrushFromColor(string colorHex)
        {
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
            }
            catch
            {
                return Brushes.Gray;
            }
        }

        private void RequestLeave_Click(object sender, RoutedEventArgs e)
        {
            ShowLeaveRequestForm();
        }

        private void ShowLeaveRequestForm()
        {
            FormTitle.Text = "Leave Request Form";
            LeaveRequestForm.Visibility = Visibility.Visible;
            ViewDetailsForm.Visibility = Visibility.Collapsed;

            // Clear form fields
            DatePickerFrom.SelectedDate = DateTime.Today;
            DatePickerTo.SelectedDate = DateTime.Today;
            TxtReasonTitle.Text = "";
            TxtDetailedReason.Text = "";

            // Show new request buttons, hide edit buttons
            NewRequestButtons.Visibility = Visibility.Visible;
        }

        private void ShowLeaveDetailsForm(LeaveResponseDto leave)
        {
            FormTitle.Text = "Leave Request Details";
            LeaveRequestForm.Visibility = Visibility.Collapsed;
            ViewDetailsForm.Visibility = Visibility.Visible;

            // Populate details
            DetailFromDate.Text = leave.FromDate;
            DetailToDate.Text = leave.ToDate;
            DetailReasonTitle.Text = leave.ReasonTitle;
            DetailDetailedReason.Text = leave.DetailedReason ?? "No detailed reason provided";
            DetailStatus.Text = leave.Status;
            DetailAdminResponse.Text = leave.AdminResponse ?? "No response yet";
            DetailCreatedAt.Text = leave.CreatedAt;
        }

        private async void SubmitRequest_Click(object sender, RoutedEventArgs e)
        {
            // Validate form
            if (!ValidateLeaveRequestForm())
                return;

            try
            {
                // Disable button to prevent double-click
                BtnSubmitRequest.IsEnabled = false;
                BtnSubmitRequest.Content = "Submitting...";

                var leaveRequest = new LeaveRequestDto
                {
                    EmpCode = _currentEmpCode,
                    FromDate = DatePickerFrom.SelectedDate.Value,
                    ToDate = DatePickerTo.SelectedDate.Value,
                    ReasonTitle = TxtReasonTitle.Text.Trim(),
                    DetailedReason = TxtDetailedReason.Text.Trim()
                };

                Console.WriteLine($"Submitting leave request for {_currentEmpCode} from {leaveRequest.FromDate} to {leaveRequest.ToDate}");

                var json = JsonSerializer.Serialize(leaveRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{Attendify.Services.HttpClientService.ApiBaseUrl}/employeeleave/request", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseDto>(responseJson, options);

                    if (apiResponse?.Success == true)
                    {
                        MessageBox.Show(apiResponse.Message, "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Clear form and reload data
                        ShowLeaveRequestForm();
                        LoadLeaveData();
                    }
                    else
                    {
                        MessageBox.Show(apiResponse?.Message ?? "Submission failed", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Error: Status={response.StatusCode}, Content={errorContent}");

                    try
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto>(errorContent, options);
                        MessageBox.Show(apiResponse?.Message ?? $"Failed to submit leave request. Status: {response.StatusCode}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch
                    {
                        MessageBox.Show($"Failed to submit leave request. Status: {response.StatusCode}\n{errorContent}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Submission Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Re-enable button
                BtnSubmitRequest.IsEnabled = true;
                BtnSubmitRequest.Content = "Submit Request";
            }
        }

        private bool ValidateLeaveRequestForm()
        {
            if (DatePickerFrom.SelectedDate == null)
            {
                MessageBox.Show("Please select a from date", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                DatePickerFrom.Focus();
                return false;
            }

            if (DatePickerTo.SelectedDate == null)
            {
                MessageBox.Show("Please select a to date", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                DatePickerTo.Focus();
                return false;
            }

            if (DatePickerFrom.SelectedDate > DatePickerTo.SelectedDate)
            {
                MessageBox.Show("From date cannot be after To date", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                DatePickerFrom.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtReasonTitle.Text))
            {
                MessageBox.Show("Please enter a reason title", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtReasonTitle.Focus();
                return false;
            }

            return true;
        }

        private void CancelNewRequest_Click(object sender, RoutedEventArgs e)
        {
            ShowLeaveRequestForm(); // Reset to empty form
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is LeaveGridItem item)
            {
                LoadLeaveDetails(item.LeaveId);
            }
        }

        private async void LoadLeaveDetails(int leaveId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{Attendify.Services.HttpClientService.ApiBaseUrl}/employeeleave/details/{leaveId}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseDto>(json, options);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        var leaveJson = apiResponse.Data.ToString();
                        var leave = JsonSerializer.Deserialize<LeaveResponseDto>(leaveJson, options);

                        if (leave != null)
                        {
                            Dispatcher.Invoke(() => ShowLeaveDetailsForm(leave));
                        }
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Error loading details: {errorContent}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading leave details: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CancelRequest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is LeaveGridItem item)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to cancel this leave request?",
                    "Confirm Cancellation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await CancelLeaveRequest(item.LeaveId);
                }
            }
        }

        private async Task CancelLeaveRequest(int leaveId)
        {
            try
            {
                var cancelRequest = new CancelLeaveDto { LeaveId = leaveId };
                var json = JsonSerializer.Serialize(cancelRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{Attendify.Services.HttpClientService.ApiBaseUrl}/employeeleave/cancel", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseDto>(responseJson, options);

                    if (apiResponse?.Success == true)
                    {
                        MessageBox.Show(apiResponse.Message, "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadLeaveData();
                    }
                    else
                    {
                        MessageBox.Show(apiResponse?.Message ?? "Cancellation failed", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Cancellation failed: {errorContent}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Cancellation Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseDetails_Click(object sender, RoutedEventArgs e)
        {
            ShowLeaveRequestForm(); // Go back to request form
        }

        private void LeaveHistoryGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optional: handle row selection if needed
        }
    }

    // Helper class for DataGrid items
    public class LeaveGridItem
    {
        public int LeaveId { get; set; }
        public string FromDate { get; set; } = null!;
        public string ToDate { get; set; } = null!;
        public string ReasonTitle { get; set; } = null!;
        public string Status { get; set; } = null!;
        public bool CanCancel { get; set; }
        public Brush StatusColor { get; set; } = Brushes.Gray;
    }
}