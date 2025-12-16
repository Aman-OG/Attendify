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
        public class EmployeeDto
        {
            public int EmployeeID { get; set; }
            public string EmpCode { get; set; } = "";
            public string FirstName { get; set; } = "";
            public string? MiddleName { get; set; }
            public string LastName { get; set; } = "";
            public string? Department { get; set; }
            public string? Position { get; set; }
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string? Role { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class ApiResponse
        {
            public EmployeeDto[] Data { get; set; } = Array.Empty<EmployeeDto>();
            public int Total { get; set; }
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int TotalPages { get; set; }
        }

        public class CreateEmployeeResponse
        {
            public int EmployeeID { get; set; }
            public string EmpCode { get; set; } = "";
            public string FirstName { get; set; } = "";
            public string? MiddleName { get; set; }
            public string LastName { get; set; } = "";
            public string? Department { get; set; }
            public string? Position { get; set; }
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string? Role { get; set; }
            public bool IsActive { get; set; }
            public string GeneratedPassword { get; set; } = "";
            public string Message { get; set; } = "";
        }

        public class ResetPasswordResponse
        {
            public string Message { get; set; } = "";
            public string NewPassword { get; set; } = "";
        }

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
                    var result = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions
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
                    MessageBox.Show($"Error loading employees: {response.StatusCode}", "Error");
                }
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Cannot connect to API. Make sure the API is running at \n\nError: {ex.Message}", "Connection Error");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error");
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
                    MessageBox.Show("Employee updated successfully!", "Success");
                    await LoadEmployeesFromApiAsync(true); // Force refresh
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Update failed: {error}", "Error");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update failed: {ex.Message}", "Error");
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
                        MessageBox.Show($"Employee added successfully!\n\nGenerated Password: {result.GeneratedPassword}\n\nPlease inform the employee.",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                        await LoadEmployeesFromApiAsync(true); // Force refresh
                        ShowEmptyForm();
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Add failed: {error}", "Error");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Add failed: {ex.Message}", "Error");
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

            var result = MessageBox.Show($"Deactivate {SelectedEmployee.FirstName} {SelectedEmployee.LastName}?",
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

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
                    MessageBox.Show("Employee deactivated successfully!", "Success");

                    // Update cache
                    _cachedEmployees = _cachedEmployees
                        .Where(e => e.EmpCode != SelectedEmployee.EmpCode)
                        .ToArray();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Delete failed: {error}", "Error");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Delete failed: {ex.Message}", "Error");
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

            var result = MessageBox.Show($"Reset password for {SelectedEmployee.FirstName} {SelectedEmployee.LastName}?",
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

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
                        MessageBox.Show($"Password reset successfully!\n\nNew Password: {resultJson.NewPassword}\n\nPlease inform the employee.",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Reset failed: {error}", "Error");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Reset failed: {ex.Message}", "Error");
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
                MessageBox.Show("First Name is required", "Validation Error");
                TxtFirstName.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(TxtLastName.Text))
            {
                MessageBox.Show("Last Name is required", "Validation Error");
                TxtLastName.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(TxtDepartment.Text))
            {
                MessageBox.Show("Department is required", "Validation Error");
                TxtDepartment.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(TxtPosition.Text))
            {
                MessageBox.Show("Position is required", "Validation Error");
                TxtPosition.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(TxtEmail.Text))
            {
                MessageBox.Show("Email is required", "Validation Error");
                TxtEmail.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(TxtPhone.Text))
            {
                MessageBox.Show("Phone is required", "Validation Error");
                TxtPhone.Focus();
                return false;
            }
            if (CmbRole.SelectedItem == null)
            {
                MessageBox.Show("Role is required", "Validation Error");
                CmbRole.Focus();
                return false;
            }
            if (!isEditMode && string.IsNullOrWhiteSpace(TxtEmployeeID.Text))
            {
                MessageBox.Show("Employee ID is required", "Validation Error");
                TxtEmployeeID.Focus();
                return false;
            }
            return true;
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