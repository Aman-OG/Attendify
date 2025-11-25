using System;
using System.Collections.Generic;
using System.Linq;
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
        // Sample data collections
        private List<AttendanceRule> _attendanceRules;
        private List<Shift> _shifts;
        private List<BroadcastMessage> _broadcastMessages;
        private List<EmployeeRequest> _employeeRequests;

        // Track current editing items
        private AttendanceRule _currentEditingRule;
        private Shift _currentEditingShift;
        private BroadcastMessage _currentEditingMessage;
        private EmployeeRequest _currentReviewingRequest;

        // Track if data is initialized
        private bool _isDataInitialized = false;

        public SettingsView()
        {
            InitializeComponent();
            Loaded += SettingsView_Loaded;
        }

        private void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isDataInitialized)
            {
                InitializeData();
                ShowTab("AttendanceRules");
                UpdateTabButtonStyles(BtnAttendanceRules);
                _isDataInitialized = true;
            }
        }

        private void InitializeData()
        {
            // Initialize sample data
            _attendanceRules = new List<AttendanceRule>
            {
                new AttendanceRule { Day = "Monday", StartTime = "09:00 AM", EndTime = "05:00 PM", GracePeriod = "10" },
                new AttendanceRule { Day = "Tuesday", StartTime = "09:00 AM", EndTime = "05:00 PM", GracePeriod = "10" },
                new AttendanceRule { Day = "Wednesday", StartTime = "09:00 AM", EndTime = "05:00 PM", GracePeriod = "10" }
            };

            _shifts = new List<Shift>
            {
                new Shift { Name = "Morning", StartTime = "08:00", EndTime = "16:00", GracePeriod = "5" },
                new Shift { Name = "Evening", StartTime = "16:00", EndTime = "00:00", GracePeriod = "5" }
            };

            _broadcastMessages = new List<BroadcastMessage>
            {
                new BroadcastMessage {
                    Title = "System Maintenance",
                    Body = "System will be down for maintenance",
                    Status = "Active",
                    StatusColor = new SolidColorBrush(Color.FromRgb(0, 128, 0)),
                    CreatedDate = DateTime.Now.ToString("MMM dd, yyyy")
                },
                new BroadcastMessage {
                    Title = "Holiday Notice",
                    Body = "Office closed for public holiday",
                    Status = "Inactive",
                    StatusColor = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
                    CreatedDate = DateTime.Now.AddDays(-2).ToString("MMM dd, yyyy")
                }
            };

            _employeeRequests = new List<EmployeeRequest>
            {
                new EmployeeRequest {
                    EmployeeID = "EMP001",
                    EmployeeName = "John Doe",
                    Type = "Late",
                    Message = "Traffic jam caused delay",
                    Status = "Pending",
                    StatusColor = new SolidColorBrush(Color.FromRgb(255, 165, 0))
                },
                new EmployeeRequest {
                    EmployeeID = "EMP002",
                    EmployeeName = "Jane Smith",
                    Type = "Absence",
                    Message = "Medical appointment",
                    Status = "Pending",
                    StatusColor = new SolidColorBrush(Color.FromRgb(255, 165, 0))
                },
                new EmployeeRequest {
                    EmployeeID = "EMP003",
                    EmployeeName = "Mike Johnson",
                    Type = "Correction",
                    Message = "Forgot to clock in",
                    Status = "Approved",
                    StatusColor = new SolidColorBrush(Color.FromRgb(0, 128, 0))
                }
            };

            // Safely bind data to grids
            SafeSetItemsSource(RulesGrid, _attendanceRules);
            SafeSetItemsSource(ShiftsGrid, _shifts);
            SafeSetItemsSource(MessagesGrid, _broadcastMessages);
            SafeSetItemsSource(RequestsGrid, _employeeRequests);

            // Initialize search placeholder
            if (RequestSearchPlaceholder != null)
            {
                RequestSearchPlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void SafeSetItemsSource(DataGrid dataGrid, System.Collections.IEnumerable itemsSource)
        {
            if (dataGrid != null && itemsSource != null)
            {
                dataGrid.ItemsSource = itemsSource;
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
            // Hide all panels first
            if (AttendanceRulesPanel != null) AttendanceRulesPanel.Visibility = Visibility.Collapsed;
            if (ShiftsPanel != null) ShiftsPanel.Visibility = Visibility.Collapsed;
            if (MessagesPanel != null) MessagesPanel.Visibility = Visibility.Collapsed;
            if (RequestsPanel != null) RequestsPanel.Visibility = Visibility.Collapsed;

            // Hide all forms
            if (RuleFormContainer != null) RuleFormContainer.Visibility = Visibility.Collapsed;
            if (ShiftFormContainer != null) ShiftFormContainer.Visibility = Visibility.Collapsed;
            if (MessageFormContainer != null) MessageFormContainer.Visibility = Visibility.Collapsed;
            if (ReviewPanel != null) ReviewPanel.Visibility = Visibility.Collapsed;

            // Show selected tab
            switch (tabName)
            {
                case "AttendanceRules":
                    if (AttendanceRulesPanel != null) AttendanceRulesPanel.Visibility = Visibility.Visible;
                    break;
                case "Shifts":
                    if (ShiftsPanel != null) ShiftsPanel.Visibility = Visibility.Visible;
                    break;
                case "BroadcastMessages":
                    if (MessagesPanel != null) MessagesPanel.Visibility = Visibility.Visible;
                    break;
                case "EmployeeRequests":
                    if (RequestsPanel != null) RequestsPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void UpdateTabButtonStyles(Button activeButton)
        {
            if (activeButton == null) return;

            // Reset all tab buttons
            var inactiveColor = new SolidColorBrush(Color.FromRgb(204, 204, 204));
            if (BtnAttendanceRules != null) BtnAttendanceRules.Foreground = inactiveColor;
            if (BtnShifts != null) BtnShifts.Foreground = inactiveColor;
            if (BtnBroadcastMessages != null) BtnBroadcastMessages.Foreground = inactiveColor;
            if (BtnEmployeeRequests != null) BtnEmployeeRequests.Foreground = inactiveColor;

            if (BtnAttendanceRules != null) BtnAttendanceRules.BorderThickness = new Thickness(0);
            if (BtnShifts != null) BtnShifts.BorderThickness = new Thickness(0);
            if (BtnBroadcastMessages != null) BtnBroadcastMessages.BorderThickness = new Thickness(0);
            if (BtnEmployeeRequests != null) BtnEmployeeRequests.BorderThickness = new Thickness(0);

            // Style active button
            var activeColor = new SolidColorBrush(Color.FromRgb(0, 166, 251));
            activeButton.Foreground = activeColor;
            activeButton.BorderThickness = new Thickness(0, 0, 0, 2);
        }

        #endregion

        #region Attendance Rules

        private void BtnAddRule_Click(object sender, RoutedEventArgs e)
        {
            _currentEditingRule = null;
            ResetRuleForm();
            if (RuleFormContainer != null) RuleFormContainer.Visibility = Visibility.Visible;
        }

        private void EditRule_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            _currentEditingRule = button?.DataContext as AttendanceRule;

            if (_currentEditingRule != null && RuleFormContainer != null)
            {
                // Populate form with rule data
                if (CmbRuleDay != null) CmbRuleDay.Text = _currentEditingRule.Day;
                if (TxtStartTime != null) TxtStartTime.Text = _currentEditingRule.StartTime;
                if (TxtEndTime != null) TxtEndTime.Text = _currentEditingRule.EndTime;
                if (TxtGracePeriod != null) TxtGracePeriod.Text = _currentEditingRule.GracePeriod;

                RuleFormContainer.Visibility = Visibility.Visible;
            }
        }

        private void DeleteRule_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var rule = button?.DataContext as AttendanceRule;

            if (rule != null)
            {
                var result = MessageBox.Show($"Are you sure you want to delete the rule for {rule.Day}?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _attendanceRules.Remove(rule);
                    RefreshRulesGrid();
                }
            }
        }

        private void BtnSaveRule_Click(object sender, RoutedEventArgs e)
        {
            if (CmbRuleDay == null || TxtStartTime == null || TxtEndTime == null)
                return;

            if (string.IsNullOrWhiteSpace(CmbRuleDay.Text) ||
                string.IsNullOrWhiteSpace(TxtStartTime.Text) ||
                string.IsNullOrWhiteSpace(TxtEndTime.Text))
            {
                MessageBox.Show("Please fill in all required fields.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_currentEditingRule == null)
            {
                // Add new rule
                var newRule = new AttendanceRule
                {
                    Day = CmbRuleDay.Text,
                    StartTime = TxtStartTime.Text,
                    EndTime = TxtEndTime.Text,
                    GracePeriod = TxtGracePeriod?.Text ?? "10"
                };
                _attendanceRules.Add(newRule);
            }
            else
            {
                // Update existing rule
                _currentEditingRule.Day = CmbRuleDay.Text;
                _currentEditingRule.StartTime = TxtStartTime.Text;
                _currentEditingRule.EndTime = TxtEndTime.Text;
                _currentEditingRule.GracePeriod = TxtGracePeriod?.Text ?? "10";
            }

            RefreshRulesGrid();
            if (RuleFormContainer != null) RuleFormContainer.Visibility = Visibility.Collapsed;
            ResetRuleForm();
        }

        private void BtnCancelRule_Click(object sender, RoutedEventArgs e)
        {
            if (RuleFormContainer != null) RuleFormContainer.Visibility = Visibility.Collapsed;
            ResetRuleForm();
        }

        private void ResetRuleForm()
        {
            if (CmbRuleDay != null) CmbRuleDay.SelectedIndex = -1;
            if (TxtStartTime != null) TxtStartTime.Text = "09:00 AM";
            if (TxtEndTime != null) TxtEndTime.Text = "05:00 PM";
            if (TxtGracePeriod != null) TxtGracePeriod.Text = "10";
            _currentEditingRule = null;
        }

        private void RefreshRulesGrid()
        {
            SafeSetItemsSource(RulesGrid, _attendanceRules);
        }

        #endregion

        #region Shifts

        private void BtnAddShift_Click(object sender, RoutedEventArgs e)
        {
            _currentEditingShift = null;
            ResetShiftForm();
            if (ShiftFormContainer != null) ShiftFormContainer.Visibility = Visibility.Visible;
        }

        private void EditShift_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            _currentEditingShift = button?.DataContext as Shift;

            if (_currentEditingShift != null && ShiftFormContainer != null)
            {
                if (TxtShiftName != null) TxtShiftName.Text = _currentEditingShift.Name;
                if (TxtShiftStart != null) TxtShiftStart.Text = _currentEditingShift.StartTime;
                if (TxtShiftEnd != null) TxtShiftEnd.Text = _currentEditingShift.EndTime;
                if (TxtShiftGrace != null) TxtShiftGrace.Text = _currentEditingShift.GracePeriod;

                ShiftFormContainer.Visibility = Visibility.Visible;
            }
        }

        private void DeleteShift_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var shift = button?.DataContext as Shift;

            if (shift != null)
            {
                var result = MessageBox.Show($"Are you sure you want to delete the {shift.Name} shift?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _shifts.Remove(shift);
                    RefreshShiftsGrid();
                }
            }
        }

        private void BtnSaveShift_Click(object sender, RoutedEventArgs e)
        {
            if (TxtShiftName == null || TxtShiftStart == null || TxtShiftEnd == null)
                return;

            if (string.IsNullOrWhiteSpace(TxtShiftName.Text) ||
                string.IsNullOrWhiteSpace(TxtShiftStart.Text) ||
                string.IsNullOrWhiteSpace(TxtShiftEnd.Text))
            {
                MessageBox.Show("Please fill in all required fields.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_currentEditingShift == null)
            {
                // Add new shift
                var newShift = new Shift
                {
                    Name = TxtShiftName.Text,
                    StartTime = TxtShiftStart.Text,
                    EndTime = TxtShiftEnd.Text,
                    GracePeriod = TxtShiftGrace?.Text ?? "5"
                };
                _shifts.Add(newShift);
            }
            else
            {
                // Update existing shift
                _currentEditingShift.Name = TxtShiftName.Text;
                _currentEditingShift.StartTime = TxtShiftStart.Text;
                _currentEditingShift.EndTime = TxtShiftEnd.Text;
                _currentEditingShift.GracePeriod = TxtShiftGrace?.Text ?? "5";
            }

            RefreshShiftsGrid();
            if (ShiftFormContainer != null) ShiftFormContainer.Visibility = Visibility.Collapsed;
            ResetShiftForm();
        }

        private void BtnCancelShift_Click(object sender, RoutedEventArgs e)
        {
            if (ShiftFormContainer != null) ShiftFormContainer.Visibility = Visibility.Collapsed;
            ResetShiftForm();
        }

        private void ResetShiftForm()
        {
            if (TxtShiftName != null) TxtShiftName.Text = "Morning";
            if (TxtShiftStart != null) TxtShiftStart.Text = "08:00";
            if (TxtShiftEnd != null) TxtShiftEnd.Text = "14:00";
            if (TxtShiftGrace != null) TxtShiftGrace.Text = "5";
            _currentEditingShift = null;
        }

        private void RefreshShiftsGrid()
        {
            SafeSetItemsSource(ShiftsGrid, _shifts);
        }

        #endregion

        #region Broadcast Messages

        private void BtnNewMessage_Click(object sender, RoutedEventArgs e)
        {
            _currentEditingMessage = null;
            ResetMessageForm();
            if (MessageFormContainer != null) MessageFormContainer.Visibility = Visibility.Visible;
        }

        private void EditMessage_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            _currentEditingMessage = button?.DataContext as BroadcastMessage;

            if (_currentEditingMessage != null && MessageFormContainer != null)
            {
                if (TxtMessageTitle != null) TxtMessageTitle.Text = _currentEditingMessage.Title;
                if (TxtMessageBody != null) TxtMessageBody.Text = _currentEditingMessage.Body;
                if (ChkIsActive != null) ChkIsActive.IsChecked = _currentEditingMessage.Status == "Active";

                MessageFormContainer.Visibility = Visibility.Visible;
            }
        }

        private void DeleteMessage_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var message = button?.DataContext as BroadcastMessage;

            if (message != null)
            {
                var result = MessageBox.Show($"Are you sure you want to delete the message '{message.Title}'?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _broadcastMessages.Remove(message);
                    RefreshMessagesGrid();
                }
            }
        }

        private void BtnSaveMessage_Click(object sender, RoutedEventArgs e)
        {
            if (TxtMessageTitle == null || TxtMessageBody == null || ChkIsActive == null)
                return;

            if (string.IsNullOrWhiteSpace(TxtMessageTitle.Text) ||
                string.IsNullOrWhiteSpace(TxtMessageBody.Text))
            {
                MessageBox.Show("Please fill in all required fields.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var status = ChkIsActive.IsChecked == true ? "Active" : "Inactive";
            var statusColor = status == "Active" ?
                new SolidColorBrush(Color.FromRgb(0, 128, 0)) :
                new SolidColorBrush(Color.FromRgb(255, 165, 0));

            if (_currentEditingMessage == null)
            {
                // Add new message
                var newMessage = new BroadcastMessage
                {
                    Title = TxtMessageTitle.Text,
                    Body = TxtMessageBody.Text,
                    Status = status,
                    StatusColor = statusColor,
                    CreatedDate = DateTime.Now.ToString("MMM dd, yyyy")
                };
                _broadcastMessages.Add(newMessage);
            }
            else
            {
                // Update existing message
                _currentEditingMessage.Title = TxtMessageTitle.Text;
                _currentEditingMessage.Body = TxtMessageBody.Text;
                _currentEditingMessage.Status = status;
                _currentEditingMessage.StatusColor = statusColor;
            }

            RefreshMessagesGrid();
            if (MessageFormContainer != null) MessageFormContainer.Visibility = Visibility.Collapsed;
            ResetMessageForm();
        }

        private void BtnCancelMessage_Click(object sender, RoutedEventArgs e)
        {
            if (MessageFormContainer != null) MessageFormContainer.Visibility = Visibility.Collapsed;
            ResetMessageForm();
        }

        private void ResetMessageForm()
        {
            if (TxtMessageTitle != null) TxtMessageTitle.Text = "";
            if (TxtMessageBody != null) TxtMessageBody.Text = "";
            if (ChkIsActive != null) ChkIsActive.IsChecked = true;
            _currentEditingMessage = null;
        }

        private void RefreshMessagesGrid()
        {
            SafeSetItemsSource(MessagesGrid, _broadcastMessages);
        }

        #endregion

        #region Employee Requests

        private void TxtRequestSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterRequests();
        }

        private void RequestFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterRequests();
        }

        private void FilterRequests()
        {
            try
            {
                if (_employeeRequests == null || RequestsGrid == null)
                {
                    return;
                }

                var searchText = TxtRequestSearch?.Text?.ToLower() ?? "";
                var statusFilterItem = CmbRequestStatus?.SelectedItem as ComboBoxItem;
                var typeFilterItem = CmbRequestType?.SelectedItem as ComboBoxItem;

                var statusFilter = statusFilterItem?.Content?.ToString() ?? "Pending";
                var typeFilter = typeFilterItem?.Content?.ToString() ?? "All Type";

                var filtered = _employeeRequests.Where(r =>
                    (string.IsNullOrEmpty(searchText) ||
                     (r.EmployeeName?.ToLower().Contains(searchText) == true) ||
                     (r.EmployeeID?.ToLower().Contains(searchText) == true)) &&
                    (statusFilter == "All Status" || r.Status == statusFilter) &&
                    (typeFilter == "All Type" || r.Type == typeFilter)
                ).ToList();

                RequestsGrid.ItemsSource = filtered;

                // Show/hide placeholder
                if (RequestSearchPlaceholder != null)
                {
                    RequestSearchPlaceholder.Visibility = string.IsNullOrEmpty(searchText) ?
                        Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                // Log error and show all items
                System.Diagnostics.Debug.WriteLine($"Filter error: {ex.Message}");
                if (RequestsGrid != null)
                    RequestsGrid.ItemsSource = _employeeRequests;
            }
        }

        private void RequestsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RequestsGrid?.SelectedItem is EmployeeRequest selectedRequest)
            {
                _currentReviewingRequest = selectedRequest;
                ShowReviewPanel(selectedRequest);
            }
        }

        private void ReviewRequest_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var request = button?.DataContext as EmployeeRequest;

            if (request != null && RequestsGrid != null)
            {
                _currentReviewingRequest = request;
                ShowReviewPanel(request);
                RequestsGrid.SelectedItem = request;
            }
        }

        private void ShowReviewPanel(EmployeeRequest request)
        {
            if (request == null || ReviewPanel == null) return;

            if (TxtReviewEmployee != null) TxtReviewEmployee.Text = $"{request.EmployeeName} ({request.EmployeeID})";
            if (TxtReviewType != null) TxtReviewType.Text = request.Type;
            if (TxtReviewMessage != null) TxtReviewMessage.Text = request.Message;
            if (TxtAdminReply != null) TxtAdminReply.Text = "";

            // Reset radio buttons
            if (RadioApprove != null) RadioApprove.IsChecked = false;
            if (RadioReject != null) RadioReject.IsChecked = false;

            ReviewPanel.Visibility = Visibility.Visible;
        }

        private void BtnSubmitDecision_Click(object sender, RoutedEventArgs e)
        {
            if (_currentReviewingRequest == null)
            {
                MessageBox.Show("No request selected for review.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if ((RadioApprove?.IsChecked != true) && (RadioReject?.IsChecked != true))
            {
                MessageBox.Show("Please select a decision (Approve or Reject).", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtAdminReply?.Text))
            {
                MessageBox.Show("Please provide an admin reply.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Update request status
            _currentReviewingRequest.Status = RadioApprove.IsChecked == true ? "Approved" : "Rejected";
            _currentReviewingRequest.StatusColor = RadioApprove.IsChecked == true ?
                new SolidColorBrush(Color.FromRgb(0, 128, 0)) :
                new SolidColorBrush(Color.FromRgb(255, 0, 0));

            // In a real application, you would save this to a database
            // and possibly notify the employee

            MessageBox.Show($"Request has been {_currentReviewingRequest.Status.ToLower()}.", "Decision Submitted",
                MessageBoxButton.OK, MessageBoxImage.Information);

            RefreshRequestsGrid();
            if (ReviewPanel != null) ReviewPanel.Visibility = Visibility.Collapsed;
            _currentReviewingRequest = null;
        }

        private void RefreshRequestsGrid()
        {
            SafeSetItemsSource(RequestsGrid, _employeeRequests);
            FilterRequests(); // Re-apply filters
        }

        #endregion
    }

    #region Data Models

    public class AttendanceRule
    {
        public string Day { get; set; } = "";
        public string StartTime { get; set; } = "";
        public string EndTime { get; set; } = "";
        public string GracePeriod { get; set; } = "";
    }

    public class Shift
    {
        public string Name { get; set; } = "";
        public string StartTime { get; set; } = "";
        public string EndTime { get; set; } = "";
        public string GracePeriod { get; set; } = "";
    }

    public class BroadcastMessage
    {
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
        public string Status { get; set; } = "";
        public Brush StatusColor { get; set; } = Brushes.Transparent;
        public string CreatedDate { get; set; } = "";
    }

    public class EmployeeRequest
    {
        public string EmployeeID { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string Type { get; set; } = "";
        public string Message { get; set; } = "";
        public string Status { get; set; } = "";
        public Brush StatusColor { get; set; } = Brushes.Transparent;
    }

    #endregion
}