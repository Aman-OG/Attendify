using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Attendify.DATA.Models;
using Newtonsoft.Json;

namespace Attendify.Views.Employee
{
    public partial class EmployeeLeaveRequestsView : UserControl
    {
        private HttpClient _httpClient;
        private string _baseUrl = "https://localhost:7129/api/leaves"; // Adjust port as needed
        private EmployeeModel _currentEmployee;

        public EmployeeLeaveRequestsView()
        {
            InitializeComponent();
            InitializeHttpClient();
            Loaded += OnViewLoaded;
        }

        private void InitializeHttpClient()
        {
            _httpClient = new HttpClient();
            // Add authorization header if needed
            // _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private async void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Load current employee from session/app context
                // _currentEmployee = App.CurrentEmployee;
                await LoadLeaveStats();
                await LoadLeaveHistory();
                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading leave requests: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadLeaveStats()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/stats");
                response.EnsureSuccessStatusCode();

                var stats = await response.Content.ReadFromJsonAsync<LeaveStats>();
                if (stats != null)
                {
                    PendingCount.Text = stats.PendingCount.ToString();
                    ApprovedCount.Text = stats.ApprovedCount.ToString();
                    RejectedCount.Text = stats.RejectedCount.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading stats: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadLeaveHistory()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/my-leaves");
                response.EnsureSuccessStatusCode();

                var leaves = await response.Content.ReadFromJsonAsync<List<LeaveModel>>();
                if (leaves != null)
                {
                    // Format dates for display
                    foreach (var leave in leaves)
                    {
                        leave.FormattedFromDate = leave.FromDate?.ToString("MMM dd, yyyy") ?? "N/A";
                        leave.FormattedToDate = leave.ToDate?.ToString("MMM dd, yyyy") ?? "N/A";
                        leave.FormattedCreatedAt = leave.CreatedAt.ToString("MMM dd, yyyy HH:mm");
                    }

                    LeaveHistoryGrid.ItemsSource = leaves;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading leave history: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetForm()
        {
            DatePickerFrom.SelectedDate = DateTime.Now;
            DatePickerTo.SelectedDate = DateTime.Now;
            TxtReasonTitle.Text = string.Empty;
            TxtDetailedReason.Text = string.Empty;

            // Show new request form
            LeaveRequestForm.Visibility = Visibility.Visible;
            ViewDetailsForm.Visibility = Visibility.Collapsed;
            FormTitle.Text = "Leave Request Form";
        }

        private async void RequestLeave_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }

        private async void SubmitRequest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validation
                if (!DatePickerFrom.SelectedDate.HasValue || !DatePickerTo.SelectedDate.HasValue)
                {
                    MessageBox.Show("Please select both dates", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(TxtReasonTitle.Text))
                {
                    MessageBox.Show("Please enter a reason title", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (DatePickerFrom.SelectedDate.Value > DatePickerTo.SelectedDate.Value)
                {
                    MessageBox.Show("From date cannot be after To date", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (DatePickerFrom.SelectedDate.Value < DateTime.Now.Date)
                {
                    MessageBox.Show("From date cannot be in the past", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var leaveRequest = new LeaveRequestModel
                {
                    FromDate = DatePickerFrom.SelectedDate.Value,
                    ToDate = DatePickerTo.SelectedDate.Value,
                    ReasonTitle = TxtReasonTitle.Text.Trim(),
                    Detail = string.IsNullOrWhiteSpace(TxtDetailedReason.Text) ? null : TxtDetailedReason.Text.Trim()
                };

                // Show loading/processing
                BtnSubmitRequest.IsEnabled = false;
                BtnSubmitRequest.Content = "Submitting...";

                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}", leaveRequest);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Leave request submitted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadLeaveStats();
                    await LoadLeaveHistory();
                    ResetForm();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var error = JsonConvert.DeserializeObject<ErrorResponse>(errorContent);
                    MessageBox.Show(error?.Message ?? "Failed to submit leave request", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error submitting request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnSubmitRequest.IsEnabled = true;
                BtnSubmitRequest.Content = "Submit Request";
            }
        }

        private void CancelNewRequest_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }

        private async void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is LeaveModel leave)
            {
                try
                {
                    // Show loading
                    ViewDetailsForm.Visibility = Visibility.Collapsed;
                    LeaveRequestForm.Visibility = Visibility.Collapsed;
                    FormTitle.Text = "Loading...";

                    // Fetch complete details
                    var response = await _httpClient.GetAsync($"{_baseUrl}/{leave.LeaveID}");
                    if (response.IsSuccessStatusCode)
                    {
                        var detailedLeave = await response.Content.ReadFromJsonAsync<LeaveModel>();
                        if (detailedLeave != null)
                        {
                            PopulateDetailsForm(detailedLeave);
                        }
                    }
                    else
                    {
                        // Fallback to basic details if API fails
                        PopulateDetailsForm(leave);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    // Fallback to basic details
                    PopulateDetailsForm(leave);
                }
            }
        }

        private void PopulateDetailsForm(LeaveModel leave)
        {
            DetailFromDate.Text = leave.FromDate?.ToString("dddd, MMMM dd, yyyy") ?? "N/A";
            DetailToDate.Text = leave.ToDate?.ToString("dddd, MMMM dd, yyyy") ?? "N/A";
            DetailReasonTitle.Text = leave.ReasonTitle ?? "N/A";
            DetailDetailedReason.Text = string.IsNullOrWhiteSpace(leave.Detail) ? "No detailed reason provided" : leave.Detail;
            DetailStatus.Text = leave.Status?.ToUpper() ?? "N/A";
            DetailAdminResponse.Text = string.IsNullOrWhiteSpace(leave.AdminResponse) ? "No response yet" : leave.AdminResponse;
            DetailCreatedAt.Text = leave.CreatedAt.ToString("dddd, MMMM dd, yyyy 'at' hh:mm tt");

            // Show details form
            ViewDetailsForm.Visibility = Visibility.Visible;
            LeaveRequestForm.Visibility = Visibility.Collapsed;
            FormTitle.Text = "Leave Request Details";
        }

        private async void CancelRequest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is LeaveModel leave)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to cancel this leave request?\n\n" +
                    $"From: {leave.FormattedFromDate}\n" +
                    $"To: {leave.FormattedToDate}\n" +
                    $"Reason: {leave.ReasonTitle}",
                    "Confirm Cancellation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var response = await _httpClient.DeleteAsync($"{_baseUrl}/{leave.LeaveID}");

                        if (response.IsSuccessStatusCode)
                        {
                            MessageBox.Show("Leave request cancelled successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            await LoadLeaveStats();
                            await LoadLeaveHistory();
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            var error = JsonConvert.DeserializeObject<ErrorResponse>(errorContent);
                            MessageBox.Show(error?.Message ?? "Failed to cancel leave request", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error cancelling request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void CloseDetails_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }

        private void LeaveHistoryGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optional: Handle row selection if needed
            // var selectedLeave = LeaveHistoryGrid.SelectedItem as LeaveModel;
        }

        // Model classes (these should ideally be in separate files, but as requested)
        public class LeaveModel
        {
            public int LeaveID { get; set; }
            public DateTime? FromDate { get; set; }
            public DateTime? ToDate { get; set; }
            public string? ReasonTitle { get; set; }
            public string? Detail { get; set; }
            public string? Status { get; set; }
            public string? AdminResponse { get; set; }
            public DateTime CreatedAt { get; set; }
            public bool CanCancel { get; set; }

            // Display properties
            [JsonIgnore]
            public string FormattedFromDate { get; set; } = string.Empty;

            [JsonIgnore]
            public string FormattedToDate { get; set; } = string.Empty;

            [JsonIgnore]
            public string FormattedCreatedAt { get; set; } = string.Empty;

            [JsonIgnore]
            public string StatusColor
            {
                get
                {
                    return Status?.ToLower() switch
                    {
                        "pending" => "#FF9800",
                        "approved" => "#38b000",
                        "rejected" => "#FF6B6B",
                        _ => "#888888"
                    };
                }
            }
        }

        public class LeaveRequestModel
        {
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public string ReasonTitle { get; set; } = string.Empty;
            public string? Detail { get; set; }
        }

        public class LeaveStats
        {
            public int PendingCount { get; set; }
            public int ApprovedCount { get; set; }
            public int RejectedCount { get; set; }
        }

        public class ErrorResponse
        {
            public string Message { get; set; } = string.Empty;
        }
    }
}