using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Input;
using System.ComponentModel;
using System.Globalization;

namespace Attendify.Views.UserControls
{
    public partial class SettingsView : UserControl
    {
        private HttpClient _httpClient;
        private string _apiBaseUrl = $"{Attendify.Services.HttpClientService.ApiBaseUrl}/settings";

        // Data collections
        private List<AttendanceRuleDto> _attendanceRules = new List<AttendanceRuleDto>();
        private List<ShiftDto> _shifts = new List<ShiftDto>();
        private List<BroadcastMessageDto> _broadcastMessages = new List<BroadcastMessageDto>();
        private List<EmployeeRequestDto> _employeeRequests = new List<EmployeeRequestDto>();

        // Track current editing items
        private AttendanceRuleDto _currentEditingRule;
        private ShiftDto _currentEditingShift;
        private BroadcastMessageDto _currentEditingMessage;
        private EmployeeRequestDto _currentReviewingRequest;

        // Track if data is initialized
        private bool _isDataInitialized = false;

        #region DTO Classes

        public class AttendanceRuleDto
        {
            public int AttendanceRuleId { get; set; }
            public string Day { get; set; } = "";
            public string StartTime { get; set; } = "";
            public string EndTime { get; set; } = "";
            public string GracePeriod { get; set; } = "";
        }

        public class ShiftDto
        {
            public int ShiftId { get; set; }
            public string Name { get; set; } = "";
            public string StartTime { get; set; } = "";
            public string EndTime { get; set; } = "";
            public string GracePeriod { get; set; } = "";
        }

        public class BroadcastMessageDto
        {
            public int BroadcastMessageId { get; set; }
            public string Title { get; set; } = "";
            public string Body { get; set; } = "";
            public string Status { get; set; } = "";
            public string StatusColor { get; set; } = "";
            public string CreatedDate { get; set; } = "";
        }

        public class EmployeeRequestDto
        {
            public int EmployeeRequestId { get; set; }
            public string EmployeeID { get; set; } = "";
            public string EmployeeName { get; set; } = "";
            public string Type { get; set; } = "";
            public string Message { get; set; } = "";
            public string Status { get; set; } = "";
            public string StatusColor { get; set; } = "";
            public string CreatedDate { get; set; } = "";
        }

        #endregion

        public SettingsView()
        {
            InitializeComponent();
            InitializeHttpClient();
            Loaded += SettingsView_Loaded;
        }

        private void InitializeHttpClient()
        {
            _httpClient = Attendify.Services.HttpClientService.Instance;
        }

        private async void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isDataInitialized)
            {
                ShowTab("AttendanceRules");
                UpdateTabButtonStyles(BtnAttendanceRules);
                await LoadAttendanceRulesAsync();
                _isDataInitialized = true;
            }
        }

        #region Tab Navigation

        private void TabButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var tabName = button.Name.Replace("Btn", "");
                ShowTab(tabName);
                UpdateTabButtonStyles(button);
            }
        }

        private void ShowTab(string tabName)
        {
            // Hide all panels
            if (AttendanceRulesPanel != null) AttendanceRulesPanel.Visibility = Visibility.Collapsed;
            if (ShiftsPanel != null) ShiftsPanel.Visibility = Visibility.Collapsed;
            if (MessagesPanel != null) MessagesPanel.Visibility = Visibility.Collapsed;
            if (RequestsPanel != null) RequestsPanel.Visibility = Visibility.Collapsed;

            // Hide all forms
            if (RuleFormContainer != null) RuleFormContainer.Visibility = Visibility.Collapsed;
            if (ShiftFormContainer != null) ShiftFormContainer.Visibility = Visibility.Collapsed;
            if (MessageFormContainer != null) MessageFormContainer.Visibility = Visibility.Collapsed;
            if (ReviewPanel != null) ReviewPanel.Visibility = Visibility.Collapsed;

            // Show selected tab and load data
            switch (tabName)
            {
                case "AttendanceRules":
                    AttendanceRulesPanel.Visibility = Visibility.Visible;
                    if (_attendanceRules.Count == 0)
                        _ = LoadAttendanceRulesAsync();
                    break;
                case "Shifts":
                    ShiftsPanel.Visibility = Visibility.Visible;
                    if (_shifts.Count == 0)
                        _ = LoadShiftsAsync();
                    break;
                case "BroadcastMessages":
                    MessagesPanel.Visibility = Visibility.Visible;
                    if (_broadcastMessages.Count == 0)
                        _ = LoadBroadcastMessagesAsync();
                    break;
                case "EmployeeRequests":
                    RequestsPanel.Visibility = Visibility.Visible;
                    if (_employeeRequests.Count == 0)
                        _ = LoadEmployeeRequestsAsync();
                    break;
            }
        }

        private void UpdateTabButtonStyles(Button activeButton)
        {
            if (activeButton == null) return;

            // Reset all tab buttons
            var inactiveColor = new SolidColorBrush(Color.FromRgb(204, 204, 204));
            BtnAttendanceRules.Foreground = inactiveColor;
            BtnShifts.Foreground = inactiveColor;
            BtnBroadcastMessages.Foreground = inactiveColor;
            BtnEmployeeRequests.Foreground = inactiveColor;

            BtnAttendanceRules.BorderThickness = new Thickness(0);
            BtnShifts.BorderThickness = new Thickness(0);
            BtnBroadcastMessages.BorderThickness = new Thickness(0);
            BtnEmployeeRequests.BorderThickness = new Thickness(0);

            // Style active button
            var activeColor = new SolidColorBrush(Color.FromRgb(0, 166, 251));
            activeButton.Foreground = activeColor;
            activeButton.BorderThickness = new Thickness(0, 0, 0, 2);
        }

        #endregion

        #region Attendance Rules API Methods

        private async Task LoadAttendanceRulesAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/attendance-rules");
                if (response.IsSuccessStatusCode)
                {
                    var rules = await response.Content.ReadFromJsonAsync<List<AttendanceRuleDto>>();
                    _attendanceRules = rules ?? new List<AttendanceRuleDto>();

                    Dispatcher.Invoke(() =>
                    {
                        RulesGrid.ItemsSource = _attendanceRules;
                    });
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    GlassMessageBox.Show($"Error loading attendance rules: {error}", "Error", false, GlassMessageBox.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Error: {ex.Message}", "Error", false, GlassMessageBox.MessageType.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async Task AddAttendanceRuleAsync(AttendanceRuleDto rule)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/attendance-rules", rule);
                if (response.IsSuccessStatusCode)
                {
                    var createdRule = await response.Content.ReadFromJsonAsync<AttendanceRuleDto>();
                    if (createdRule != null)
                    {
                        _attendanceRules.Add(createdRule);
                        Dispatcher.Invoke(() =>
                        {
                            RulesGrid.ItemsSource = null;
                            RulesGrid.ItemsSource = _attendanceRules;
                        });
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    GlassMessageBox.Show($"Error: {error}", "Error", false, GlassMessageBox.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Error: {ex.Message}", "Error", false, GlassMessageBox.MessageType.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async Task UpdateAttendanceRuleAsync(AttendanceRuleDto rule)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_apiBaseUrl}/attendance-rules/{rule.AttendanceRuleId}", rule);
                if (response.IsSuccessStatusCode)
                {
                    var index = _attendanceRules.FindIndex(r => r.AttendanceRuleId == rule.AttendanceRuleId);
                    if (index != -1)
                    {
                        _attendanceRules[index] = rule;
                        Dispatcher.Invoke(() =>
                        {
                            RulesGrid.ItemsSource = null;
                            RulesGrid.ItemsSource = _attendanceRules;
                        });
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    GlassMessageBox.Show($"Error: {error}", "Error", false, GlassMessageBox.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Error: {ex.Message}", "Error", false, GlassMessageBox.MessageType.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async Task DeleteAttendanceRuleAsync(int id)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/attendance-rules/{id}");
                if (response.IsSuccessStatusCode)
                {
                    _attendanceRules.RemoveAll(r => r.AttendanceRuleId == id);
                    Dispatcher.Invoke(() =>
                    {
                        RulesGrid.ItemsSource = null;
                        RulesGrid.ItemsSource = _attendanceRules;
                    });
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    GlassMessageBox.Show($"Error: {error}", "Error", false, GlassMessageBox.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Error: {ex.Message}", "Error", false, GlassMessageBox.MessageType.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Attendance Rules Event Handlers

        private void BtnAddRule_Click(object sender, RoutedEventArgs e)
        {
            _currentEditingRule = null;
            ResetRuleForm();
            RuleFormContainer.Visibility = Visibility.Visible;
        }

        private void EditRule_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            _currentEditingRule = button?.DataContext as AttendanceRuleDto;

            if (_currentEditingRule != null)
            {
                if (CmbRuleDay != null) CmbRuleDay.Text = _currentEditingRule.Day;
                if (TxtStartTime != null) SetTimeFields(_currentEditingRule.StartTime, TxtStartTime, TxtStartAmPm);
                if (TxtEndTime != null) SetTimeFields(_currentEditingRule.EndTime, TxtEndTime, TxtEndAmPm);
                if (TxtGracePeriod != null) TxtGracePeriod.Text = _currentEditingRule.GracePeriod;

                RuleFormContainer.Visibility = Visibility.Visible;
            }
        }

        private async void DeleteRule_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var rule = button?.DataContext as AttendanceRuleDto;

            if (rule != null)
            {
                var result = GlassMessageBox.Show($"Delete rule for {rule.Day}?", "Confirm", true);

                if (result == GlassMessageBox.MessageBoxResult.OK)
                {
                    await DeleteAttendanceRuleAsync(rule.AttendanceRuleId);
                }
            }
        }

        private async void BtnSaveRule_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CmbRuleDay?.Text) ||
                string.IsNullOrWhiteSpace(TxtStartTime?.Text) ||
                string.IsNullOrWhiteSpace(TxtStartAmPm?.Text) ||
                string.IsNullOrWhiteSpace(TxtEndTime?.Text) ||
                string.IsNullOrWhiteSpace(TxtEndAmPm?.Text))
            {
                GlassMessageBox.Show("Please fill in all required fields.", "Error", false, GlassMessageBox.MessageType.Error);
                return;
            }

            string startTime, endTime;
            try
            {
                startTime = Get24HourTime(TxtStartTime.Text, TxtStartAmPm.Text);
                endTime = Get24HourTime(TxtEndTime.Text, TxtEndAmPm.Text);
            }
            catch (ArgumentException ex) {
                  GlassMessageBox.Show(ex.Message, "Invalid Time", false, GlassMessageBox.MessageType.Error);
                 return;
            }

            var rule = new AttendanceRuleDto
            {
                Day = CmbRuleDay.Text,
                StartTime = startTime,
                EndTime = endTime,
                GracePeriod = TxtGracePeriod?.Text ?? "10"
            };

            if (_currentEditingRule == null)
            {
                await AddAttendanceRuleAsync(rule);
            }
            else
            {
                rule.AttendanceRuleId = _currentEditingRule.AttendanceRuleId;
                await UpdateAttendanceRuleAsync(rule);
            }

            RuleFormContainer.Visibility = Visibility.Collapsed;
            ResetRuleForm();
        }

        private void BtnCancelRule_Click(object sender, RoutedEventArgs e)
        {
            RuleFormContainer.Visibility = Visibility.Collapsed;
            ResetRuleForm();
        }

        private void ResetRuleForm()
        {
            if (CmbRuleDay != null) CmbRuleDay.SelectedIndex = -1;
            if (TxtStartTime != null) TxtStartTime.Text = "09:00";
            if (TxtStartAmPm != null) TxtStartAmPm.Text = "AM";
            if (TxtEndTime != null) TxtEndTime.Text = "05:00";
            if (TxtEndAmPm != null) TxtEndAmPm.Text = "PM";
            if (TxtGracePeriod != null) TxtGracePeriod.Text = "10";
            _currentEditingRule = null;
        }

        #endregion

        #region Shifts API Methods

        private async Task LoadShiftsAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/shifts");
                if (response.IsSuccessStatusCode)
                {
                    var shifts = await response.Content.ReadFromJsonAsync<List<ShiftDto>>();
                    _shifts = shifts ?? new List<ShiftDto>();

                    Dispatcher.Invoke(() =>
                    {
                        ShiftsGrid.ItemsSource = _shifts;
                    });
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    GlassMessageBox.Show($"Error loading shifts: {error}", "Error", false, GlassMessageBox.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Error loading shifts: {ex.Message}", "Error", false, GlassMessageBox.MessageType.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async Task AddShiftAsync(ShiftDto shift)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/shifts", shift);
                if (response.IsSuccessStatusCode)
                {
                    var createdShift = await response.Content.ReadFromJsonAsync<ShiftDto>();
                    if (createdShift != null)
                    {
                        _shifts.Add(createdShift);
                        Dispatcher.Invoke(() =>
                        {
                            ShiftsGrid.ItemsSource = null;
                            ShiftsGrid.ItemsSource = _shifts;
                        });
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    GlassMessageBox.Show($"Error: {error}", "Error", false, GlassMessageBox.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Error: {ex.Message}", "Error", false, GlassMessageBox.MessageType.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async Task UpdateShiftAsync(ShiftDto shift)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_apiBaseUrl}/shifts/{shift.ShiftId}", shift);
                if (response.IsSuccessStatusCode)
                {
                    var index = _shifts.FindIndex(s => s.ShiftId == shift.ShiftId);
                    if (index != -1)
                    {
                        _shifts[index] = shift;
                        Dispatcher.Invoke(() =>
                        {
                            ShiftsGrid.ItemsSource = null;
                            ShiftsGrid.ItemsSource = _shifts;
                        });
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    GlassMessageBox.Show($"Error: {error}", "Error", false, GlassMessageBox.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Error: {ex.Message}", "Error", false, GlassMessageBox.MessageType.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async Task DeleteShiftAsync(int id)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/shifts/{id}");
                if (response.IsSuccessStatusCode)
                {
                    _shifts.RemoveAll(s => s.ShiftId == id);
                    Dispatcher.Invoke(() =>
                    {
                        ShiftsGrid.ItemsSource = null;
                        ShiftsGrid.ItemsSource = _shifts;
                    });
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    GlassMessageBox.Show($"Error: {error}", "Error", false, GlassMessageBox.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Error: {ex.Message}", "Error", false, GlassMessageBox.MessageType.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Shifts Event Handlers

        private void BtnAddShift_Click(object sender, RoutedEventArgs e)
        {
            _currentEditingShift = null;
            ResetShiftForm();
            ShiftFormContainer.Visibility = Visibility.Visible;
        }

        private void EditShift_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            _currentEditingShift = button?.DataContext as ShiftDto;

            if (_currentEditingShift != null)
            {
                if (TxtShiftName != null) TxtShiftName.Text = _currentEditingShift.Name;
                if (TxtShiftStart != null) SetTimeFields(_currentEditingShift.StartTime, TxtShiftStart, TxtShiftStartAmPm);
                if (TxtShiftEnd != null) SetTimeFields(_currentEditingShift.EndTime, TxtShiftEnd, TxtShiftEndAmPm);
                if (TxtShiftGrace != null) TxtShiftGrace.Text = _currentEditingShift.GracePeriod;

                ShiftFormContainer.Visibility = Visibility.Visible;
            }
        }

        private async void DeleteShift_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var shift = button?.DataContext as ShiftDto;

            if (shift != null)
            {
                var result = GlassMessageBox.Show($"Delete shift '{shift.Name}'?", "Confirm", true);

                if (result == GlassMessageBox.MessageBoxResult.OK)
                {
                    await DeleteShiftAsync(shift.ShiftId);
                }
            }
        }

        private async void BtnSaveShift_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtShiftName?.Text) ||
                string.IsNullOrWhiteSpace(TxtShiftStart?.Text) ||
                string.IsNullOrWhiteSpace(TxtShiftStartAmPm?.Text) ||
                string.IsNullOrWhiteSpace(TxtShiftEnd?.Text) ||
                string.IsNullOrWhiteSpace(TxtShiftEndAmPm?.Text))
            {
                GlassMessageBox.Show("Please fill in all required fields.", "Error");
                return;
            }

            string startTime, endTime;
            try
            {
                startTime = Get24HourTime(TxtShiftStart.Text, TxtShiftStartAmPm.Text);
                endTime = Get24HourTime(TxtShiftEnd.Text, TxtShiftEndAmPm.Text);
            }
            catch (ArgumentException ex) {
                 GlassMessageBox.Show(ex.Message, "Invalid Time");
                 return;
            }

            var shift = new ShiftDto
            {
                Name = TxtShiftName.Text,
                StartTime = startTime,
                EndTime = endTime,
                GracePeriod = TxtShiftGrace?.Text ?? "5"
            };

            if (_currentEditingShift == null)
            {
                await AddShiftAsync(shift);
            }
            else
            {
                shift.ShiftId = _currentEditingShift.ShiftId;
                await UpdateShiftAsync(shift);
            }

            ShiftFormContainer.Visibility = Visibility.Collapsed;
            ResetShiftForm();
        }

        private void BtnCancelShift_Click(object sender, RoutedEventArgs e)
        {
            ShiftFormContainer.Visibility = Visibility.Collapsed;
            ResetShiftForm();
        }

        private void ResetShiftForm()
        {
            if (TxtShiftName != null) TxtShiftName.Text = "";
            if (TxtShiftStart != null) TxtShiftStart.Text = "08:00";
            if (TxtShiftStartAmPm != null) TxtShiftStartAmPm.Text = "AM";
            if (TxtShiftEnd != null) TxtShiftEnd.Text = "02:00";
            if (TxtShiftEndAmPm != null) TxtShiftEndAmPm.Text = "PM";
            if (TxtShiftGrace != null) TxtShiftGrace.Text = "5";
            _currentEditingShift = null;
        }

        #endregion

        #region Helper Methods

        private string Get24HourTime(string time12, string amPm)
        {
            if (string.IsNullOrWhiteSpace(time12) || string.IsNullOrWhiteSpace(amPm)) return null;

            // Normalize AM/PM
            amPm = amPm.Trim().ToUpper();
            if (amPm != "AM" && amPm != "PM") throw new ArgumentException("AM/PM must be 'AM' or 'PM'");

            // Try different formats to be robust
            string[] formats = { "h:mm tt", "hh:mm tt", "H:mm", "HH:mm" };
            
            // If user enters "09:00" "AM" -> "09:00 AM"
            string combined = $"{time12.Trim()} {amPm}";
            
            if (DateTime.TryParseExact(combined, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
            {
                return dt.ToString("HH:mm"); // Return 24h format for backend
            }
            
            // Fallback for simple parse
             if (DateTime.TryParse(combined, out dt))
            {
                return dt.ToString("HH:mm");
            }
            
            throw new ArgumentException("Invalid time format. Use 'hh:mm'");
        }

        private void SetTimeFields(string time24, TextBox txtTime, TextBox txtAmPm)
        {
            if (string.IsNullOrEmpty(time24))
            {
                txtTime.Text = "";
                txtAmPm.Text = "AM";
                return;
            }

            // Backend typically sends "HH:mm:ss" or "HH:mm"
            if (DateTime.TryParse(time24, out DateTime dt))
            {
                txtTime.Text = dt.ToString("hh:mm");
                txtAmPm.Text = dt.ToString("tt", CultureInfo.InvariantCulture).ToUpper();
            }
            else
            {
                // Fallback if parsing fails
                txtTime.Text = time24;
                txtAmPm.Text = "AM";
            }
        }

        #endregion
        
        #region Broadcast Messages API Methods

        private async Task LoadBroadcastMessagesAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/broadcast-messages");
                if (response.IsSuccessStatusCode)
                {
                    var messages = await response.Content.ReadFromJsonAsync<List<BroadcastMessageDto>>();
                    _broadcastMessages = messages ?? new List<BroadcastMessageDto>();

                    Dispatcher.Invoke(() =>
                    {
                        MessagesGrid.ItemsSource = _broadcastMessages;
                    });
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    GlassMessageBox.Show($"Error loading messages: {error}", "Error", false, GlassMessageBox.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Error loading messages: {ex.Message}", "Error", false, GlassMessageBox.MessageType.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async Task AddBroadcastMessageAsync(BroadcastMessageDto message)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/broadcast-messages", message);
                if (response.IsSuccessStatusCode)
                {
                    var createdMessage = await response.Content.ReadFromJsonAsync<BroadcastMessageDto>();
                    if (createdMessage != null)
                    {
                        _broadcastMessages.Add(createdMessage);
                        Dispatcher.Invoke(() =>
                        {
                            MessagesGrid.ItemsSource = null;
                            MessagesGrid.ItemsSource = _broadcastMessages;
                        });
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    GlassMessageBox.Show($"Error: {error}", "Error", false, GlassMessageBox.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Error: {ex.Message}", "Error", false, GlassMessageBox.MessageType.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async Task UpdateBroadcastMessageAsync(BroadcastMessageDto message)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_apiBaseUrl}/broadcast-messages/{message.BroadcastMessageId}", message);
                if (response.IsSuccessStatusCode)
                {
                    var index = _broadcastMessages.FindIndex(m => m.BroadcastMessageId == message.BroadcastMessageId);
                    if (index != -1)
                    {
                        _broadcastMessages[index] = message;
                        Dispatcher.Invoke(() =>
                        {
                            MessagesGrid.ItemsSource = null;
                            MessagesGrid.ItemsSource = _broadcastMessages;
                        });
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    GlassMessageBox.Show($"Error: {error}", "Error", false, GlassMessageBox.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Error: {ex.Message}", "Error", false, GlassMessageBox.MessageType.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async Task DeleteBroadcastMessageAsync(int id)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/broadcast-messages/{id}");
                if (response.IsSuccessStatusCode)
                {
                    _broadcastMessages.RemoveAll(m => m.BroadcastMessageId == id);
                    Dispatcher.Invoke(() =>
                    {
                        MessagesGrid.ItemsSource = null;
                        MessagesGrid.ItemsSource = _broadcastMessages;
                    });
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    GlassMessageBox.Show($"Error: {error}", "Error", false, GlassMessageBox.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Error: {ex.Message}", "Error", false, GlassMessageBox.MessageType.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Broadcast Messages Event Handlers

        private void BtnNewMessage_Click(object sender, RoutedEventArgs e)
        {
            _currentEditingMessage = null;
            ResetMessageForm();
            MessageFormContainer.Visibility = Visibility.Visible;
        }

        private void EditMessage_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            _currentEditingMessage = button?.DataContext as BroadcastMessageDto;

            if (_currentEditingMessage != null)
            {
                if (TxtMessageTitle != null) TxtMessageTitle.Text = _currentEditingMessage.Title;
                if (TxtMessageBody != null) TxtMessageBody.Text = _currentEditingMessage.Body;
                if (ChkIsActive != null) ChkIsActive.IsChecked = _currentEditingMessage.Status == "Active";

                MessageFormContainer.Visibility = Visibility.Visible;
            }
        }

        private async void DeleteMessage_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var message = button?.DataContext as BroadcastMessageDto;

            if (message != null)
            {
                var result = GlassMessageBox.Show($"Delete message '{message.Title}'?", "Confirm", true);

                if (result == GlassMessageBox.MessageBoxResult.OK)
                {
                    await DeleteBroadcastMessageAsync(message.BroadcastMessageId);
                }
            }
        }

        private async void BtnSaveMessage_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtMessageTitle?.Text) ||
                string.IsNullOrWhiteSpace(TxtMessageBody?.Text))
            {
                GlassMessageBox.Show("Please fill in title and message body.", "Error");
                return;
            }

            var message = new BroadcastMessageDto
            {
                Title = TxtMessageTitle.Text,
                Body = TxtMessageBody.Text,
                Status = (ChkIsActive?.IsChecked == true) ? "Active" : "Inactive",
                StatusColor = (ChkIsActive?.IsChecked == true) ? "#4CAF50" : "#F44336",
                CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            };

            if (_currentEditingMessage == null)
            {
                await AddBroadcastMessageAsync(message);
            }
            else
            {
                message.BroadcastMessageId = _currentEditingMessage.BroadcastMessageId;
                await UpdateBroadcastMessageAsync(message);
            }

            MessageFormContainer.Visibility = Visibility.Collapsed;
            ResetMessageForm();
        }

        private void BtnCancelMessage_Click(object sender, RoutedEventArgs e)
        {
            MessageFormContainer.Visibility = Visibility.Collapsed;
            ResetMessageForm();
        }

        private void ResetMessageForm()
        {
            if (TxtMessageTitle != null) TxtMessageTitle.Text = "";
            if (TxtMessageBody != null) TxtMessageBody.Text = "";
            if (ChkIsActive != null) ChkIsActive.IsChecked = true;
            _currentEditingMessage = null;
        }

        #endregion

        #region Employee Requests API Methods

        private async Task LoadEmployeeRequestsAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/employee-requests");
                if (response.IsSuccessStatusCode)
                {
                    var requests = await response.Content.ReadFromJsonAsync<List<EmployeeRequestDto>>();
                    _employeeRequests = requests ?? new List<EmployeeRequestDto>();

                    Dispatcher.Invoke(() =>
                    {
                        RequestsGrid.ItemsSource = _employeeRequests;
                    });
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    GlassMessageBox.Show($"Error loading requests: {error}", "Error", false, GlassMessageBox.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Error loading requests: {ex.Message}", "Error", false, GlassMessageBox.MessageType.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Employee Requests Event Handlers

        private void FilterRequests()
        {
            // This will be handled server-side now
            _ = LoadEmployeeRequestsAsync(); // Reload with current filters
        }

        private void TxtRequestSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterRequests();
        }

        private void RequestsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RequestsGrid?.SelectedItem is EmployeeRequestDto selectedRequest)
            {
                _currentReviewingRequest = selectedRequest;
                ShowReviewPanel(selectedRequest);
            }
        }

        private void ReviewRequest_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var request = button?.DataContext as EmployeeRequestDto;

            if (request != null)
            {
                _currentReviewingRequest = request;
                ShowReviewPanel(request);
            }
        }

        private void ShowReviewPanel(EmployeeRequestDto request)
        {
            if (request == null) return;

            TxtReviewEmployee.Text = $"{request.EmployeeName} ({request.EmployeeID})";
            TxtReviewType.Text = request.Type;
            TxtReviewMessage.Text = request.Message;
            TxtAdminReply.Text = "";

            RadioApprove.IsChecked = false;
            RadioReject.IsChecked = false;

            ReviewPanel.Visibility = Visibility.Visible;
        }

        private async void BtnSubmitDecision_Click(object sender, RoutedEventArgs e)
        {
            if (_currentReviewingRequest == null)
            {
                GlassMessageBox.Show("No request selected.", "Error", false, GlassMessageBox.MessageType.Error);
                return;
            }

            if ((RadioApprove?.IsChecked != true) && (RadioReject?.IsChecked != true))
            {
                GlassMessageBox.Show("Select Approve or Reject.", "Error");
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtAdminReply?.Text))
            {
                GlassMessageBox.Show("Enter admin reply.", "Error");
                return;
            }

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var reviewDto = new
                {
                    Decision = RadioApprove.IsChecked == true ? "Approved" : "Rejected",
                    AdminReply = TxtAdminReply.Text,
                    AdminId = 1 // Get from your auth system
                };

                var response = await _httpClient.PutAsJsonAsync(
                    $"{_apiBaseUrl}/employee-requests/{_currentReviewingRequest.EmployeeRequestId}/review",
                    reviewDto);

                if (response.IsSuccessStatusCode)
                {
                    GlassMessageBox.Show("Decision submitted.", "Success", false, GlassMessageBox.MessageType.Success);
                    ReviewPanel.Visibility = Visibility.Collapsed;
                    _currentReviewingRequest = null;
                    await LoadEmployeeRequestsAsync(); // Refresh list
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    GlassMessageBox.Show($"Error: {error}", "Error", false, GlassMessageBox.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Error: {ex.Message}", "Error", false, GlassMessageBox.MessageType.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        #endregion
    }
}