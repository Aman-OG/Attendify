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
    public partial class EmployeeContactAdminView : UserControl
    {
        private HttpClient _httpClient;
        // private const string ApiBaseUrl = "https://localhost:7129/api";
        private string _currentEmpCode;
        private DispatcherTimer _refreshTimer;

        // DTO classes
        public class ContactResponseDto
        {
            public int RequestId { get; set; }
            public string Date { get; set; } = null!;
            public string Type { get; set; } = null!;
            public string Message { get; set; } = null!;
            public string Status { get; set; } = null!;
            public string? AdminReply { get; set; }
            public string CreatedAt { get; set; } = null!;
            public string StatusColor { get; set; } = "#FF9800";
            public string TypeIcon { get; set; } = "📝";
        }

        public class ApiResponseDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = null!;
            public object? Data { get; set; }
        }

        public class ContactStatsDto
        {
            public int Total { get; set; }
            public int Pending { get; set; }
            public int Resolved { get; set; }
        }

        public class RequestTypeDto
        {
            public string Value { get; set; } = null!;
            public string Label { get; set; } = null!;
            public string Icon { get; set; } = null!;
        }

        public class ContactRequestDto
        {
            public string EmpCode { get; set; } = null!;
            public string Date { get; set; } = null!;
            public string Type { get; set; } = null!;
            public string Message { get; set; } = null!;
        }

        // Constructor with empCode parameter
        public EmployeeContactAdminView(string empCode)
        {
            InitializeComponent();
            _currentEmpCode = empCode;
            Loaded += EmployeeContactAdminView_Loaded;
        }

        private void EmployeeContactAdminView_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeHttpClient();
            LoadContactData();

            // Set up auto-refresh timer (every 30 seconds)
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _refreshTimer.Tick += (s, e) => LoadContactData();
            _refreshTimer.Start();
        }

        private void InitializeHttpClient()
        {
            if (_httpClient == null)
            {
                _httpClient = Attendify.Services.HttpClientService.Instance;
            }
        }

        private async void LoadContactData()
        {
            try
            {
                await LoadRequestTypes();
                await LoadContactRequests();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading contact data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadRequestTypes()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{Attendify.Services.HttpClientService.ApiBaseUrl}/employeecontact/types");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseDto>(json, options);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        var typesJson = apiResponse.Data.ToString();
                        var types = JsonSerializer.Deserialize<List<RequestTypeDto>>(typesJson, options);

                        Dispatcher.Invoke(() =>
                        {
                            CmbRequestType.ItemsSource = types;
                            if (types?.Count > 0)
                            {
                                CmbRequestType.SelectedIndex = 0;
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading request types: {ex.Message}");
                Dispatcher.Invoke(() => LoadDefaultTypes());
            }
        }

        private void LoadDefaultTypes()
        {
            var defaultTypes = new List<RequestTypeDto>
    {
        new RequestTypeDto { Value = "Late", Label = "Late Arrival", Icon = "⏰" },
        new RequestTypeDto { Value = "Absence", Label = "Absence Report", Icon = "🚫" },
        new RequestTypeDto { Value = "Correction", Label = "Attendance Correction", Icon = "✏️" },
        new RequestTypeDto { Value = "Other", Label = "Other Inquiry", Icon = "📝" }
    };

            CmbRequestType.ItemsSource = defaultTypes;
            if (defaultTypes.Count > 0)
            {
                CmbRequestType.SelectedIndex = 0;
            }
        }


        private async Task LoadContactRequests()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{Attendify.Services.HttpClientService.ApiBaseUrl}/employeecontact/requests/{_currentEmpCode}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseDto>(json, options);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        var requestsJson = apiResponse.Data.ToString();
                        var requests = JsonSerializer.Deserialize<List<ContactResponseDto>>(requestsJson, options);

                        Dispatcher.Invoke(() =>
                        {
                            UpdateRequestHistoryGrid(requests ?? new List<ContactResponseDto>());
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading contact requests: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateRequestHistoryGrid(List<ContactResponseDto> requests)
        {
            var gridItems = new List<ContactGridItem>();
            foreach (var request in requests)
            {
                gridItems.Add(new ContactGridItem
                {
                    RequestId = request.RequestId,
                    Date = request.Date,
                    Type = request.Type,
                    TypeIcon = request.TypeIcon,
                    TypeLabel = GetTypeLabel(request.Type), // Add this for display
                    Message = request.Message.Length > 50 ? request.Message.Substring(0, 50) + "..." : request.Message,
                    Status = request.Status,
                    AdminReply = request.AdminReply,
                    CreatedAt = request.CreatedAt,
                    StatusColor = GetBrushFromColor(request.StatusColor)
                });
            }

            RequestHistoryGrid.ItemsSource = gridItems;
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

        private void NewRequest_Click(object sender, RoutedEventArgs e)
        {
            ShowNewRequestForm();
        }

        private void ShowNewRequestForm()
        {
            FormTitle.Text = "New Request Form";
            NewRequestForm.Visibility = Visibility.Visible;
            ViewDetailsForm.Visibility = Visibility.Collapsed;

            // Clear form fields
            DatePickerRequestDate.SelectedDate = DateTime.Today;
            if (CmbRequestType.Items.Count > 0)
                CmbRequestType.SelectedIndex = 0;
            TxtMessage.Text = "";

            // Show new request buttons
            NewRequestButtons.Visibility = Visibility.Visible;
        }

        private void ShowDetailsForm(ContactResponseDto request)
        {
            FormTitle.Text = "Request Details";
            NewRequestForm.Visibility = Visibility.Collapsed;
            ViewDetailsForm.Visibility = Visibility.Visible;

            // Populate details
            DetailDate.Text = request.Date;
            DetailTypeIcon.Text = request.TypeIcon;

            // Convert the stored value (like "Late") to a user-friendly label
            DetailType.Text = GetTypeLabel(request.Type);
            DetailMessage.Text = request.Message;
            DetailStatus.Text = request.Status;
            DetailAdminResponse.Text = request.AdminReply ?? "No response yet";
            DetailCreatedAt.Text = request.CreatedAt;

            // Set status badge color
            DetailStatusBorder.Background = GetBrushFromColor(request.StatusColor);
        }


        private string GetTypeLabel(string typeValue)
        {
            return typeValue.ToLower() switch
            {
                "late" => "Late Arrival",
                "absence" => "Absence Report",
                "correction" => "Attendance Correction",
                "other" => "Other Inquiry",
                _ => typeValue
            };
        }

        private async void SubmitRequest_Click(object sender, RoutedEventArgs e)
        {
            // Validate form
            if (!ValidateRequestForm())
                return;

            try
            {
                // Disable button to prevent double-click
                BtnSubmitRequest.IsEnabled = false;
                BtnSubmitRequest.Content = "Submitting...";

                var selectedType = (RequestTypeDto)CmbRequestType.SelectedItem;

                // Debug: Check what value is being sent
                Console.WriteLine($"Selected Type Value: {selectedType?.Value}");
                Console.WriteLine($"Selected Type Label: {selectedType?.Label}");

                var contactRequest = new ContactRequestDto
                {
                    EmpCode = _currentEmpCode,
                    Date = DatePickerRequestDate.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd"),
                    Type = selectedType?.Value ?? "Other", // Use the Value property, not Label
                    Message = TxtMessage.Text.Trim()
                };

                Console.WriteLine($"Sending request with Type: {contactRequest.Type}");

                var json = JsonSerializer.Serialize(contactRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{Attendify.Services.HttpClientService.ApiBaseUrl}/employeecontact/request", content);

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
                        ShowNewRequestForm();
                        LoadContactData();
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
                    MessageBox.Show($"Failed to submit request: {errorContent}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
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

        private bool ValidateRequestForm()
        {
            if (DatePickerRequestDate.SelectedDate == null)
            {
                MessageBox.Show("Please select a date", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                DatePickerRequestDate.Focus();
                return false;
            }

            if (CmbRequestType.SelectedItem == null)
            {
                MessageBox.Show("Please select a request type", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CmbRequestType.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtMessage.Text))
            {
                MessageBox.Show("Please enter a message", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtMessage.Focus();
                return false;
            }

            if (TxtMessage.Text.Length < 10)
            {
                MessageBox.Show("Please provide more details in your message (minimum 10 characters)", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtMessage.Focus();
                return false;
            }

            return true;
        }

        private void CancelNewRequest_Click(object sender, RoutedEventArgs e)
        {
            ShowNewRequestForm(); // Reset to empty form
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ContactGridItem item)
            {
                LoadRequestDetails(item.RequestId);
            }
        }

        private async void LoadRequestDetails(int requestId)
        {
            try
            {
                // Since we don't have a details endpoint, we'll use the existing data
                var requests = RequestHistoryGrid.ItemsSource as IEnumerable<ContactGridItem>;
                var request = requests?.FirstOrDefault(r => r.RequestId == requestId);

                if (request != null)
                {
                    var contactResponse = new ContactResponseDto
                    {
                        RequestId = request.RequestId,
                        Date = request.Date,
                        Type = request.Type,
                        Message = request.Message,
                        Status = request.Status,
                        AdminReply = request.AdminReply,
                        CreatedAt = request.CreatedAt,
                        StatusColor = GetColorFromBrush(request.StatusColor),
                        TypeIcon = request.TypeIcon
                    };

                    Dispatcher.Invoke(() => ShowDetailsForm(contactResponse));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading request details: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetColorFromBrush(Brush brush)
        {
            if (brush is SolidColorBrush solidBrush)
            {
                return solidBrush.Color.ToString();
            }
            return "#FF9800"; // Default orange
        }

        private void CloseDetails_Click(object sender, RoutedEventArgs e)
        {
            ShowNewRequestForm(); // Go back to request form
        }

        private void RequestHistoryGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optional: handle row selection if needed
        }
    }

    // Helper class for DataGrid items
    public class ContactGridItem
    {
        public int RequestId { get; set; }
        public string Date { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string TypeLabel { get; set; } = null!;   // Display label (e.g., "Late Arrival")
        public string TypeIcon { get; set; } = "📝";
        public string Message { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? AdminReply { get; set; }
        public string CreatedAt { get; set; } = null!;
        public Brush StatusColor { get; set; } = Brushes.Gray;
    }
}