using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Text.RegularExpressions;
using Attendify.Models;
using Attendify.Views;

namespace Attendify.Views.UserControls
{
    public partial class EmployeesView : UserControl, INotifyPropertyChanged
    {
        private ObservableCollection<EmployeeDto> _employees = new();
        private EmployeeDto _selectedEmployee;
        private bool _isAddingNew = false;
        private readonly HttpClient _httpClient;
        // private readonly string _apiBaseUrl = "https://localhost:7129/";

        // Loading states
        private bool _isLoadingEmployees = false;
        private bool _isUpdating = false;
        private bool _isAdding = false;
        private bool _isDeleting = false;
        private bool _isResettingPassword = false;

        // Cache for performance
        private EmployeeDto[] _cachedEmployees = Array.Empty<EmployeeDto>();
        private DateTime _lastLoadTime = DateTime.MinValue;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

        // DTO Classes - MUST BE PUBLIC
        // DTO classes moved to Attendify.Models namespace

        // Properties
        public ObservableCollection<EmployeeDto> Employees
        {
            get => _employees;
            set { _employees = value; OnPropertyChanged(); }
        }

        public EmployeeDto SelectedEmployee
        {
            get => _selectedEmployee;
            set { _selectedEmployee = value; OnPropertyChanged(); }
        }

        // Loading properties
        public bool IsLoadingEmployees
        {
            get => _isLoadingEmployees;
            set { _isLoadingEmployees = value; OnPropertyChanged(); }
        }

        public bool IsUpdating
        {
            get => _isUpdating;
            set { _isUpdating = value; OnPropertyChanged(); }
        }

        public bool IsAdding
        {
            get => _isAdding;
            set { _isAdding = value; OnPropertyChanged(); }
        }

        public bool IsDeleting
        {
            get => _isDeleting;
            set { _isDeleting = value; OnPropertyChanged(); }
        }

        public bool IsResettingPassword
        {
            get => _isResettingPassword;
            set { _isResettingPassword = value; OnPropertyChanged(); }
        }

        public bool IsBusy => IsUpdating || IsAdding || IsDeleting || IsResettingPassword;

        public EmployeesView()
        {
            InitializeComponent();
            DataContext = this;

            // Initialize HttpClient
            _httpClient = Attendify.Services.HttpClientService.Instance;

            Loaded += async (s, e) => await LoadEmployeesFromApiAsync(false);
            ShowEmptyForm();
        }

        private async Task LoadEmployeesFromApiAsync(bool forceRefresh = false)
        {
            // Use cache if available and not expired
            if (!forceRefresh &&
                _cachedEmployees.Length > 0 &&
                (DateTime.Now - _lastLoadTime) < _cacheDuration)
            {
                Employees = new ObservableCollection<EmployeeDto>(_cachedEmployees);
                return;
            }

            if (IsLoadingEmployees) return;

            IsLoadingEmployees = true;
            EmployeesLoading.Visibility = Visibility.Visible;

            try
            {
                var response = await _httpClient.GetAsync("admin/employees?pageSize=100");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<PaginatedResponse<EmployeeDto>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (result != null && result.Data != null)
                    {
                        // Update cache
                        _cachedEmployees = result.Data;
                        _lastLoadTime = DateTime.Now;

                        Employees = new ObservableCollection<EmployeeDto>(_cachedEmployees);
                        EmployeesGrid.ItemsSource = Employees;
                    }
                }
                else
                {
                    GlassMessageBox.Show($"Error loading employees: {response.StatusCode}", "Error", false, GlassMessageBox.MessageType.Error);
                }
            }
            catch (HttpRequestException ex)
            {
                GlassMessageBox.Show($"Cannot connect to API. Make sure the API is running.\n\nError: {ex.Message}", "Connection Error", false, GlassMessageBox.MessageType.Error);
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Error: {ex.Message}", "Error", false, GlassMessageBox.MessageType.Error);
            }
            finally
            {
                IsLoadingEmployees = false;
                EmployeesLoading.Visibility = Visibility.Collapsed;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (Employees == null || Employees.Count == 0)
                return;

            var view = CollectionViewSource.GetDefaultView(Employees);
            if (view == null)
                return;

            string search = SearchBox?.Text?.Trim().ToLower() ?? "";

            if (string.IsNullOrWhiteSpace(search))
            {
                view.Filter = null; // Show all
            }
            else
            {
                view.Filter = empObj =>
                {
                    if (empObj is not EmployeeDto emp)
                        return false;

                    return (emp.EmpCode?.ToLower().Contains(search) ?? false) ||
                           (emp.FirstName?.ToLower().Contains(search) ?? false) ||
                           (emp.LastName?.ToLower().Contains(search) ?? false) ||
                           (emp.Email?.ToLower().Contains(search) ?? false) ||
                           (emp.Department?.ToLower().Contains(search) ?? false) ||
                           (emp.Position?.ToLower().Contains(search) ?? false);
                };
            }

            view.Refresh();
        }

        private void EmployeesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedEmployee = EmployeesGrid.SelectedItem as EmployeeDto;
            if (SelectedEmployee != null)
                ShowEmployeeForm(SelectedEmployee, false);
        }

        private void BtnAddEmployee_Click(object sender, RoutedEventArgs e)
        {
            ShowEmployeeForm(null, true);
        }

        private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedEmployee == null || !ValidateForm(true) || IsUpdating) return;

            IsUpdating = true;
            FormLoadingText.Text = "Updating employee...";
            FormLoading.Visibility = Visibility.Visible;

            try
            {
                var updateRequest = new
                {
                    FirstName = TxtFirstName.Text,
                    MiddleName = TxtMiddleName.Text,
                    LastName = TxtLastName.Text,
                    Department = TxtDepartment.Text,
                    Position = TxtPosition.Text,
                    Email = TxtEmail.Text,
                    Phone = TxtPhone.Text,
                    Role = (CmbRole.SelectedItem as ComboBoxItem)?.Content?.ToString()
                };

                var response = await _httpClient.PutAsJsonAsync(
                    $"admin/employees/{SelectedEmployee.EmpCode}",
                    updateRequest);

                if (response.IsSuccessStatusCode)
                {
                    GlassMessageBox.Show("Employee updated successfully!", "Success", false, GlassMessageBox.MessageType.Success);
                    await LoadEmployeesFromApiAsync(true); // Force refresh
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    GlassMessageBox.Show($"Update failed: {error}", "Error", false, GlassMessageBox.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Update failed: {ex.Message}", "Error", false, GlassMessageBox.MessageType.Error);
            }
            finally
            {
                IsUpdating = false;
                FormLoading.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm(false) || IsAdding) return;

            IsAdding = true;
            FormLoadingText.Text = "Creating employee...";
            FormLoading.Visibility = Visibility.Visible;

            var createRequest = new
            {
                EmpCode = TxtEmployeeID.Text.Trim(),
                FirstName = TxtFirstName.Text,
                MiddleName = TxtMiddleName.Text,
                LastName = TxtLastName.Text,
                Department = TxtDepartment.Text,
                Position = TxtPosition.Text,
                Email = TxtEmail.Text,
                Phone = TxtPhone.Text,
                Role = (CmbRole.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "User"
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("admin/employees", createRequest);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CreateEmployeeResponse>();

                    if (result != null)
                    {
                        GlassMessageBox.Show($"Employee added successfully!\n\nGenerated Password: {result.GeneratedPassword}\n\nPlease inform the employee.",
                            "Success", false, GlassMessageBox.MessageType.Success);

                        await LoadEmployeesFromApiAsync(true); // Force refresh
                        ShowEmptyForm();
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    GlassMessageBox.Show($"Add failed: {error}", "Error", false, GlassMessageBox.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Add failed: {ex.Message}", "Error", false, GlassMessageBox.MessageType.Error);
            }
            finally
            {
                IsAdding = false;
                FormLoading.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedEmployee == null || IsDeleting) return;

            var result = GlassMessageBox.Show($"Deactivate {SelectedEmployee.FirstName} {SelectedEmployee.LastName}?",
                "Confirm", true);
            if (result != GlassMessageBox.MessageBoxResult.OK) return;

            IsDeleting = true;
            FormLoadingText.Text = "Deactivating employee...";
            FormLoading.Visibility = Visibility.Visible;

            try
            {
                var response = await _httpClient.DeleteAsync($"admin/employees/{SelectedEmployee.EmpCode}");

                if (response.IsSuccessStatusCode)
                {
                    // Remove from local collection immediately
                    Employees.Remove(SelectedEmployee);
                    ShowEmptyForm();
                    GlassMessageBox.Show("Employee deactivated successfully!", "Success", false, GlassMessageBox.MessageType.Success);

                    // Update cache
                    _cachedEmployees = _cachedEmployees
                        .Where(e => e.EmpCode != SelectedEmployee.EmpCode)
                        .ToArray();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    GlassMessageBox.Show($"Delete failed: {error}", "Error", false, GlassMessageBox.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Delete failed: {ex.Message}", "Error", false, GlassMessageBox.MessageType.Error);
            }
            finally
            {
                IsDeleting = false;
                FormLoading.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnResetPassword_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedEmployee == null || IsResettingPassword) return;

            var result = GlassMessageBox.Show($"Reset password for {SelectedEmployee.FirstName} {SelectedEmployee.LastName}?",
                "Confirm", true);

            if (result != GlassMessageBox.MessageBoxResult.OK) return;

            IsResettingPassword = true;
            FormLoadingText.Text = "Resetting password...";
            FormLoading.Visibility = Visibility.Visible;

            try
            {
                var response = await _httpClient.PostAsync(
                    $"admin/employees/{SelectedEmployee.EmpCode}/reset-password",
                    null);

                if (response.IsSuccessStatusCode)
                {
                    var resultJson = await response.Content.ReadFromJsonAsync<ResetPasswordResponse>();
                    if (resultJson != null)
                    {
                        GlassMessageBox.Show($"Password reset successfully!\n\nNew Password: {resultJson.NewPassword}\n\nPlease inform the employee.",
                            "Success", false, GlassMessageBox.MessageType.Success);
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    GlassMessageBox.Show($"Reset failed: {error}", "Error", false, GlassMessageBox.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Reset failed: {ex.Message}", "Error", false, GlassMessageBox.MessageType.Error);
            }
            finally
            {
                IsResettingPassword = false;
                FormLoading.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ShowEmptyForm();
        }

        // Refresh button click handler (add this if you add refresh button)
        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadEmployeesFromApiAsync(true);
        }

        private void ShowEmployeeForm(EmployeeDto employee, bool isAddMode)
        {
            _isAddingNew = isAddMode;
            EmptyFormText.Visibility = Visibility.Collapsed;
            EmployeeForm.Visibility = Visibility.Visible;

            if (isAddMode)
            {
                ClearForm();
                TxtEmployeeID.IsReadOnly = false;
                AddModeButtons.Visibility = Visibility.Visible;
                EditModeButtons.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (employee != null)
                {
                    TxtEmployeeID.Text = employee.EmpCode ?? "";
                    TxtEmployeeID.IsReadOnly = true;
                    TxtFirstName.Text = employee.FirstName ?? "";
                    TxtMiddleName.Text = employee.MiddleName ?? "";
                    TxtLastName.Text = employee.LastName ?? "";
                    TxtDepartment.Text = employee.Department ?? "";
                    TxtPosition.Text = employee.Position ?? "";
                    TxtEmail.Text = employee.Email ?? "";
                    TxtPhone.Text = employee.Phone ?? "";

                    foreach (ComboBoxItem item in CmbRole.Items)
                    {
                        if (item.Content?.ToString() == employee.Role)
                        {
                            CmbRole.SelectedItem = item;
                            break;
                        }
                    }
                }
                AddModeButtons.Visibility = Visibility.Collapsed;
                EditModeButtons.Visibility = Visibility.Visible;
            }
        }

        private void ShowEmptyForm()
        {
            EmployeeForm.Visibility = Visibility.Collapsed;
            EmptyFormText.Visibility = Visibility.Visible;
            ClearForm();
            _isAddingNew = false;
        }

        private void ClearForm()
        {
            TxtEmployeeID.Text = "";
            TxtFirstName.Text = "";
            TxtMiddleName.Text = "";
            TxtLastName.Text = "";
            TxtDepartment.Text = "";
            TxtPosition.Text = "";
            TxtEmail.Text = "";
            TxtPhone.Text = "";
            CmbRole.SelectedIndex = -1;
        }

        private bool ValidateForm(bool isEditMode)
        {
            if (string.IsNullOrWhiteSpace(TxtFirstName.Text))
            {
                GlassMessageBox.Show("First Name is required", "Validation Error", false, GlassMessageBox.MessageType.Error);
                TxtFirstName.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(TxtLastName.Text))
            {
                GlassMessageBox.Show("Last Name is required", "Validation Error", false, GlassMessageBox.MessageType.Error);
                TxtLastName.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(TxtDepartment.Text))
            {
                GlassMessageBox.Show("Department is required", "Validation Error", false, GlassMessageBox.MessageType.Error);
                TxtDepartment.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(TxtPosition.Text))
            {
                GlassMessageBox.Show("Position is required", "Validation Error", false, GlassMessageBox.MessageType.Error);
                TxtPosition.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(TxtEmail.Text))
            {
                GlassMessageBox.Show("Email is required", "Validation Error", false, GlassMessageBox.MessageType.Error);
                TxtEmail.Focus();
                return false;
            }
             // Email validation
            if (!IsValidEmail(TxtEmail.Text))
            {
                GlassMessageBox.Show("Please enter a valid email address (e.g., user@example.com)", "Validation Error", false, GlassMessageBox.MessageType.Error);
                TxtEmail.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(TxtPhone.Text))
            {
                GlassMessageBox.Show("Phone is required", "Validation Error", false, GlassMessageBox.MessageType.Error);
                TxtPhone.Focus();
                return false;
            }
            // Phone validation
            if (!IsValidPhone(TxtPhone.Text))
            {
                GlassMessageBox.Show("Phone number must be exactly 10 digits", "Validation Error", false, GlassMessageBox.MessageType.Error);
                TxtPhone.Focus();
                return false;
            }

            if (CmbRole.SelectedItem == null)
            {
                GlassMessageBox.Show("Role is required", "Validation Error", false, GlassMessageBox.MessageType.Error);
                CmbRole.Focus();
                return false;
            }
            if (!isEditMode && string.IsNullOrWhiteSpace(TxtEmployeeID.Text))
            {
                GlassMessageBox.Show("Employee ID is required", "Validation Error", false, GlassMessageBox.MessageType.Error);
                TxtEmployeeID.Focus();
                return false;
            }
            return true;
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                // Simple regex for email validation
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        private bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            // Check if it contains only digits and is 10 digits long
            // Remove any potential formatting (like dashes or spaces) if user types them, 
            // but requirement says "is 10 digits", so usually strict check is better for consistency.
            // Let's assume strict 10 digits for now as per request.
            return phone.Length == 10 && phone.All(char.IsDigit);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            // Notify IsBusy property when any loading state changes
            if (propertyName == nameof(IsUpdating) ||
                propertyName == nameof(IsAdding) ||
                propertyName == nameof(IsDeleting) ||
                propertyName == nameof(IsResettingPassword))
            {
                OnPropertyChanged(nameof(IsBusy));
            }
        }
    }
}