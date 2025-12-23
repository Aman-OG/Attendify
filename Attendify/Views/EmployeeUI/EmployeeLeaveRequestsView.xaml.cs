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
using Attendify.Views;

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
            LoadLeaveData(true);

            // Set up auto-refresh timer (every 30 seconds)
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _refreshTimer.Tick += (s, e) => LoadLeaveData(false);
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
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                await LoadLeaveStats();
                await LoadLeaveRequests();
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Error loading leave data: {ex.Message}", "Error");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        // Note: LoadLeaveStats and LoadLeaveRequests are helpers called by LoadLeaveData. 
        // Since LoadLeaveData manages the overlay, we don't necessarily need to add it inside these if they are always called from there.
        // HOWEVER, the timer calls LoadLeaveData, so maybe we SHOULD NOT show the overlay on every timer tick?
        // Actually, the timer tick calls LoadLeaveData. This would be annoying every 30 seconds.
        // Let's modify behavior: LoadLeaveData takes a parameter 'showSpinner' default true.
        // But to keep it simple and consistent with "only show specific spinners during API calls", we probably want the spinner when the user FIRST loads the view, but not necessarily on background regresh.
        // The current implementation calls LoadLeaveData() from Loaded event and Timer.
        // Let's change the Loaded event to call with true, and timer with false.

        // Wait, I can't easily change the signature and all calls without reading everything carefully.
        // Let's just look at how LoadLeaveData is defined.
        
        // Revised plan for LoadLeaveData:
        // Overload LoadLeaveData(bool showSpinner = true)
        
        private async void LoadLeaveData(bool showSpinner = true)
        {
            if (showSpinner) LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                await LoadLeaveStats();
                await LoadLeaveRequests();
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Error loading leave data: {ex.Message}", "Error");
            }
            finally
            {
                if (showSpinner) LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        // We also need to update the call sites.
        // line 76: LoadLeaveData(); -> LoadLeaveData(true);
        // line 80: _refreshTimer.Tick += (s, e) => LoadLeaveData(); -> ... LoadLeaveData(false);
        // line 286: LoadLeaveData(); -> LoadLeaveData(true);
        // line 438: LoadLeaveData(); -> LoadLeaveData(true);

        private async Task LoadLeaveStats()
        {
             // No internal spinner management here, relying on caller
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
             // No internal spinner management here, relying on caller
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
                GlassMessageBox.Show($"Error loading leave requests: {ex.Message}", "Error");
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

        private bool ValidateLeaveRequestForm()
        {
            if (DatePickerFrom.SelectedDate == null)
            {
                GlassMessageBox.Show("Please select a from date", "Validation Error");
                DatePickerFrom.Focus();
                return false;
            }

            if (DatePickerTo.SelectedDate == null)
            {
                GlassMessageBox.Show("Please select a to date", "Validation Error");
                DatePickerTo.Focus();
                return false;
            }

            if (DatePickerFrom.SelectedDate > DatePickerTo.SelectedDate)
            {
                GlassMessageBox.Show("From date cannot be after To date", "Validation Error");
                DatePickerFrom.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtReasonTitle.Text))
            {
                GlassMessageBox.Show("Please enter a reason title", "Validation Error");
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

        private async void CancelRequest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is LeaveGridItem item)
            {
                var result = GlassMessageBox.Show(
                    "Are you sure you want to cancel this leave request?",
                    "Confirm Cancellation",
                    true);

                if (result == GlassMessageBox.MessageBoxResult.OK)
                {
                    await CancelLeaveRequest(item.LeaveId);
                }
            }
        }

        private async void LoadLeaveDetails(int leaveId)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
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
                    GlassMessageBox.Show($"Error loading details: {errorContent}", "Error");
                }
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Error loading leave details: {ex.Message}", "Error");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async Task CancelLeaveRequest(int leaveId)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
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
                        GlassMessageBox.Show(apiResponse.Message, "Success");
                        LoadLeaveData(true);
                    }
                    else
                    {
                        GlassMessageBox.Show(apiResponse?.Message ?? "Cancellation failed", "Error");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    GlassMessageBox.Show($"Cancellation failed: {errorContent}", "Error");
                }
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Error: {ex.Message}", "Cancellation Error");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
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

        private async void SubmitRequest_Click(object sender, RoutedEventArgs e)
        {
            // Validate form
            if (!ValidateLeaveRequestForm())
                return;

            LoadingOverlay.Visibility = Visibility.Visible;
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
                        GlassMessageBox.Show(apiResponse.Message, "Success");

                        // Clear form and reload data
                        ShowLeaveRequestForm();
                        LoadLeaveData(true);
                    }
                    else
                    {
                        GlassMessageBox.Show(apiResponse?.Message ?? "Submission failed", "Error");
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
                        GlassMessageBox.Show(apiResponse?.Message ?? $"Failed to submit leave request. Status: {response.StatusCode}", "Error");
                    }
                    catch
                    {
                        GlassMessageBox.Show($"Failed to submit leave request. Status: {response.StatusCode}\n{errorContent}", "Error");
                    }
                }
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Error: {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Submission Error");
            }
            finally
            {
                // Re-enable button
                BtnSubmitRequest.IsEnabled = true;
                BtnSubmitRequest.Content = "Submit Request";
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
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