using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Attendify.Views.UserControls
{
    public partial class EmployeesView : UserControl, INotifyPropertyChanged
    {
        private ObservableCollection<Employee> _employees;
        private Employee _selectedEmployee;
        private bool _isAddingNew = false;

        public ObservableCollection<Employee> Employees
        {
            get => _employees;
            set
            {
                _employees = value;
                OnPropertyChanged();
            }
        }

        public Employee SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                _selectedEmployee = value;
                OnPropertyChanged();
            }
        }

        public EmployeesView()
        {
            InitializeComponent();
            DataContext = this;
            LoadSampleData();
            ShowEmptyForm();
        }

        private void LoadSampleData()
        {
            Employees = new ObservableCollection<Employee>
            {
                new Employee { EmployeeID = "EMP10001", FirstName = "Aman", MiddleName = "", LastName = "Baye",
                             Department = "HR", Position = "Manager", Email = "aman@company.com",
                             Phone = "+1234567890", Role = "Admin" },
                new Employee { EmployeeID = "EMP10002", FirstName = "Markos", MiddleName = "K", LastName = "Neby",
                             Department = "Software", Position = "Developer", Email = "markos@company.com",
                             Phone = "+1234567891", Role = "User" },
                new Employee { EmployeeID = "EMP10003", FirstName = "Teddy", MiddleName = "J", LastName = "Smith",
                             Department = "Electrical", Position = "Engineer", Email = "teddy@company.com",
                             Phone = "+1234567892", Role = "Manager" }
            };

            EmployeesGrid.ItemsSource = Employees;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchPlaceholder != null)
                SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            ApplyFilters();
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (Employees == null) return;

            var collectionView = System.Windows.Data.CollectionViewSource.GetDefaultView(Employees);

            collectionView.Filter = item =>
            {
                var employee = item as Employee;
                if (employee == null) return false;

                try
                {
                    // Search filter
                    var searchText = SearchBox?.Text?.ToLower() ?? "";
                    if (!string.IsNullOrEmpty(searchText))
                    {
                        var matchesSearch = (employee.FirstName?.ToLower().Contains(searchText) == true) ||
                                          (employee.LastName?.ToLower().Contains(searchText) == true) ||
                                          (employee.Email?.ToLower().Contains(searchText) == true) ||
                                          (employee.EmployeeID?.ToLower().Contains(searchText) == true) ||
                                          (employee.Department?.ToLower().Contains(searchText) == true);
                        if (!matchesSearch) return false;
                    }

                    // Department filter
                    var departmentFilterItem = DepartmentFilter?.SelectedItem as ComboBoxItem;
                    var departmentFilter = departmentFilterItem?.Content?.ToString();

                    if (!string.IsNullOrEmpty(departmentFilter) &&
                        departmentFilter != "All Departments" &&
                        departmentFilter != employee.Department)
                        return false;

                    // Role filter
                    var roleFilterItem = RoleFilter?.SelectedItem as ComboBoxItem;
                    var roleFilter = roleFilterItem?.Content?.ToString();

                    if (!string.IsNullOrEmpty(roleFilter) &&
                        roleFilter != "All Roles" &&
                        roleFilter != employee.Role)
                        return false;

                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Filter error: {ex.Message}");
                    return true;
                }
            };
        }

        private void EmployeesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedEmployee = EmployeesGrid.SelectedItem as Employee;

            if (SelectedEmployee != null)
            {
                ShowEmployeeForm(SelectedEmployee, false); // Edit mode
            }
        }

        private void BtnAddEmployee_Click(object sender, RoutedEventArgs e)
        {
            ShowEmployeeForm(null, true); // Add mode
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedEmployee != null && ValidateForm())
            {
                // Update existing employee
                SelectedEmployee.FirstName = TxtFirstName.Text;
                SelectedEmployee.MiddleName = TxtMiddleName.Text;
                SelectedEmployee.LastName = TxtLastName.Text;
                SelectedEmployee.Department = TxtDepartment.Text;
                SelectedEmployee.Position = TxtPosition.Text;
                SelectedEmployee.Email = TxtEmail.Text;
                SelectedEmployee.Phone = TxtPhone.Text;
                SelectedEmployee.Role = (CmbRole.SelectedItem as ComboBoxItem)?.Content.ToString();

                EmployeesGrid.Items.Refresh();
                MessageBox.Show("Employee updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                ApplyFilters();
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedEmployee != null)
            {
                var result = MessageBox.Show($"Are you sure you want to delete {SelectedEmployee.FirstName} {SelectedEmployee.LastName}?",
                                           "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    Employees.Remove(SelectedEmployee);
                    ShowEmptyForm();
                    ApplyFilters();
                    MessageBox.Show("Employee deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateForm())
            {
                // Add new employee
                var newEmployee = new Employee
                {
                    EmployeeID = TxtEmployeeID.Text,
                    FirstName = TxtFirstName.Text,
                    MiddleName = TxtMiddleName.Text,
                    LastName = TxtLastName.Text,
                    Department = TxtDepartment.Text,
                    Position = TxtPosition.Text,
                    Email = TxtEmail.Text,
                    Phone = TxtPhone.Text,
                    Role = (CmbRole.SelectedItem as ComboBoxItem)?.Content.ToString()
                };

                Employees.Add(newEmployee);
                MessageBox.Show("Employee added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                ApplyFilters();
                ShowEmptyForm();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ShowEmptyForm();
        }

        private void ShowEmployeeForm(Employee employee, bool isAddMode)
        {
            _isAddingNew = isAddMode;

            if (EmptyFormText != null) EmptyFormText.Visibility = Visibility.Collapsed;
            if (EmployeeForm != null) EmployeeForm.Visibility = Visibility.Visible;

            if (isAddMode)
            {
                // Add mode
                ClearForm();
                TxtEmployeeID.Text = GenerateNewEmployeeID();
                EditModeButtons.Visibility = Visibility.Collapsed;
                AddModeButtons.Visibility = Visibility.Visible;
            }
            else
            {
                // Edit mode
                if (employee != null)
                {
                    TxtEmployeeID.Text = employee.EmployeeID ?? "";
                    TxtFirstName.Text = employee.FirstName ?? "";
                    TxtMiddleName.Text = employee.MiddleName ?? "";
                    TxtLastName.Text = employee.LastName ?? "";
                    TxtDepartment.Text = employee.Department ?? "";
                    TxtPosition.Text = employee.Position ?? "";
                    TxtEmail.Text = employee.Email ?? "";
                    TxtPhone.Text = employee.Phone ?? "";

                    // Select role in combobox
                    if (!string.IsNullOrEmpty(employee.Role) && CmbRole != null)
                    {
                        foreach (ComboBoxItem item in CmbRole.Items)
                        {
                            if (item?.Content?.ToString() == employee.Role)
                            {
                                CmbRole.SelectedItem = item;
                                break;
                            }
                        }
                    }
                }
                EditModeButtons.Visibility = Visibility.Visible;
                AddModeButtons.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowEmptyForm()
        {
            if (EmployeeForm != null) EmployeeForm.Visibility = Visibility.Collapsed;
            if (EmptyFormText != null) EmptyFormText.Visibility = Visibility.Visible;
            ClearForm();
            _isAddingNew = false;
        }

        private void ClearForm()
        {
            if (TxtEmployeeID != null) TxtEmployeeID.Text = "";
            if (TxtFirstName != null) TxtFirstName.Text = "";
            if (TxtMiddleName != null) TxtMiddleName.Text = "";
            if (TxtLastName != null) TxtLastName.Text = "";
            if (TxtDepartment != null) TxtDepartment.Text = "";
            if (TxtPosition != null) TxtPosition.Text = "";
            if (TxtEmail != null) TxtEmail.Text = "";
            if (TxtPhone != null) TxtPhone.Text = "";
            if (CmbRole != null) CmbRole.SelectedIndex = -1;
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(TxtFirstName?.Text))
            {
                MessageBox.Show("First Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtFirstName?.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtLastName?.Text))
            {
                MessageBox.Show("Last Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtLastName?.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtDepartment?.Text))
            {
                MessageBox.Show("Department is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtDepartment?.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtPosition?.Text))
            {
                MessageBox.Show("Position is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtPosition?.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtEmail?.Text))
            {
                MessageBox.Show("Email is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtEmail?.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtPhone?.Text))
            {
                MessageBox.Show("Phone is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtPhone?.Focus();
                return false;
            }

            if (CmbRole?.SelectedItem == null)
            {
                MessageBox.Show("Role is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                CmbRole?.Focus();
                return false;
            }

            return true;
        }

        private string GenerateNewEmployeeID()
        {
            if (Employees == null || Employees.Count == 0)
                return "EMP10001";

            try
            {
                var maxId = Employees.Max(e =>
                {
                    if (e?.EmployeeID != null && e.EmployeeID.StartsWith("EMP"))
                    {
                        var idPart = e.EmployeeID.Replace("EMP", "");
                        if (int.TryParse(idPart, out int id))
                            return id;
                    }
                    return 10000;
                });
                return $"EMP{maxId + 1:00000}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating employee ID: {ex.Message}");
                return "EMP10001";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Employee
    {
        public string EmployeeID { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string MiddleName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Department { get; set; } = "";
        public string Position { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public string Phone { get; set; } = "";
    }
}