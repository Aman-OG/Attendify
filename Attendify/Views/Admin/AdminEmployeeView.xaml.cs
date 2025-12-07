
using Attendify.DATA;// ← THIS ONE ONLY for Employee
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Data;


using DataEmployee = Attendify.DATA.Models.Employee;

namespace Attendify.Views.UserControls
{
    public partial class EmployeesView : UserControl, INotifyPropertyChanged
    {
        private ObservableCollection<DataEmployee> _employees = new();
        private DataEmployee _selectedEmployee;
        private bool _isAddingNew = false;
        private App _app;  // For service provider

        public ObservableCollection<DataEmployee> Employees
        {
            get => _employees;
            set { _employees = value; OnPropertyChanged(); }
        }

        public DataEmployee SelectedEmployee
        {
            get => _selectedEmployee;
            set { _selectedEmployee = value; OnPropertyChanged(); }
        }

        public EmployeesView()
        {
            InitializeComponent();
            _app = (App)Application.Current;  // Get service provider
            DataContext = this;
            Loaded += async (s, e) => await LoadEmployeesFromSupabaseAsync();  // Load on init
            ShowEmptyForm();
        }

        private async Task LoadEmployeesFromSupabaseAsync()
        {
            try
            {
                using var scope = _app.ServiceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var employees = await context.Employees
                    .Where(e => e.IsActive)  // Only active ones
                    .OrderBy(e => e.EmpCode)
                    .ToListAsync();

                Employees = new ObservableCollection<DataEmployee>(employees);
                EmployeesGrid.ItemsSource = Employees;

                if (Employees.Count == 0)
                    MessageBox.Show("No employees found. Add your first one!", "Info");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading from Supabase: {ex.Message}\nCheck connection string.", "Error");
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Console.WriteLine($"SearchBox text: {SearchBox.Text}");
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

      
            view.Filter = empObj =>
            {
                if (empObj is not DataEmployee emp)
                    return false;

                // ============================
                // SEARCH FILTER
                // ============================
                if (!string.IsNullOrWhiteSpace(search))
                {
                    bool match =
                        (emp.EmpCode?.ToLower().Contains(search) ?? false) ||
                        (emp.FirstName?.ToLower().Contains(search) ?? false) ||
                        (emp.LastName?.ToLower().Contains(search) ?? false) ||
                        (emp.Email?.ToLower().Contains(search) ?? false) ||
                        (emp.Department?.ToLower().Contains(search) ?? false) ||
                        (emp.Position?.ToLower().Contains(search) ?? false);

                    if (!match)
                        return false;
                }


                return true;
            };

            view.Refresh();
        }


        private void EmployeesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedEmployee = EmployeesGrid.SelectedItem as DataEmployee;
            if (SelectedEmployee != null)
                ShowEmployeeForm(SelectedEmployee, false);
        }

        private void BtnAddEmployee_Click(object sender, RoutedEventArgs e)
        {
            ShowEmployeeForm(null, true);
        }

        private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedEmployee == null || !ValidateForm()) return;

            try
            {
                using var scope = _app.ServiceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                SelectedEmployee.FirstName = TxtFirstName.Text;
                SelectedEmployee.MiddleName = TxtMiddleName.Text;
                SelectedEmployee.LastName = TxtLastName.Text;
                SelectedEmployee.Department = TxtDepartment.Text;
                SelectedEmployee.Position = TxtPosition.Text;
                SelectedEmployee.Email = TxtEmail.Text;
                SelectedEmployee.Phone = TxtPhone.Text;
                SelectedEmployee.Role = (CmbRole.SelectedItem as ComboBoxItem)?.Content?.ToString();


                context.Employees.Update(SelectedEmployee);
                await context.SaveChangesAsync();

                MessageBox.Show("Employee updated in Supabase!", "Success");
                await LoadEmployeesFromSupabaseAsync();  // Refresh
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update failed: {ex.Message}");
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedEmployee == null) return;

            var result = MessageBox.Show($"Delete {SelectedEmployee.FirstName} {SelectedEmployee.LastName}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                using var scope = _app.ServiceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                SelectedEmployee.IsActive = false;  // Soft delete
                context.Employees.Update(SelectedEmployee);
                await context.SaveChangesAsync();

                Employees.Remove(SelectedEmployee);
                ShowEmptyForm();
                MessageBox.Show("Employee deactivated in Supabase!", "Success");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Delete failed: {ex.Message}");
            }
        }

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            var newEmp = new DataEmployee
            {
                EmpCode = TxtEmployeeID.Text.Trim(),
                FirstName = TxtFirstName.Text,
                MiddleName = TxtMiddleName.Text,
                LastName = TxtLastName.Text,
                Department = TxtDepartment.Text,
                Position = TxtPosition.Text,
                Email = TxtEmail.Text,
                Phone = TxtPhone.Text,
                Role = (CmbRole.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            if (string.IsNullOrWhiteSpace(newEmp.EmpCode))
            {
                MessageBox.Show("Employee ID (EmpCode) is required!", "Error");
                return;
            }

            try
            {
                using var scope = _app.ServiceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                if (await context.Employees.AnyAsync(x => x.EmpCode == newEmp.EmpCode))
                {
                    MessageBox.Show("Employee ID already exists!", "Duplicate");
                    return;
                }

                context.Employees.Add(newEmp);
                await context.SaveChangesAsync();

                MessageBox.Show($"Employee {newEmp.EmpCode} added to Supabase!", "Success");
                await LoadEmployeesFromSupabaseAsync();  // Refresh
                ShowEmptyForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Add failed: {ex.Message}");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => ShowEmptyForm();

        private void ShowEmployeeForm(DataEmployee employee, bool isAddMode)
        {
            _isAddingNew = isAddMode;
            EmptyFormText.Visibility = Visibility.Collapsed;
            EmployeeForm.Visibility = Visibility.Visible;

            if (isAddMode)
            {
                ClearForm();
                TxtEmployeeID.IsReadOnly = false;  // Editable for new
                AddModeButtons.Visibility = Visibility.Visible;
                EditModeButtons.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (employee != null)
                {
                    TxtEmployeeID.Text = employee.EmpCode ?? "";
                    TxtEmployeeID.IsReadOnly = true;  // Read-only for edit
                    TxtFirstName.Text = employee.FirstName ?? "";
                    TxtMiddleName.Text = employee.MiddleName ?? "";
                    TxtLastName.Text = employee.LastName ?? "";
                    TxtDepartment.Text = employee.Department ?? "";
                    TxtPosition.Text = employee.Position ?? "";
                    TxtEmail.Text = employee.Email ?? "";
                    TxtPhone.Text = employee.Phone ?? "";
                    foreach (ComboBoxItem item in CmbRole.Items)
                        if (item.Content?.ToString() == employee.Role) { CmbRole.SelectedItem = item; break; }
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

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(TxtFirstName.Text)) { MessageBox.Show("First Name required"); TxtFirstName.Focus(); return false; }
            if (string.IsNullOrWhiteSpace(TxtLastName.Text)) { MessageBox.Show("Last Name required"); TxtLastName.Focus(); return false; }
            if (string.IsNullOrWhiteSpace(TxtDepartment.Text)) { MessageBox.Show("Department required"); TxtDepartment.Focus(); return false; }
            if (string.IsNullOrWhiteSpace(TxtPosition.Text)) { MessageBox.Show("Position required"); TxtPosition.Focus(); return false; }
            if (string.IsNullOrWhiteSpace(TxtEmail.Text)) { MessageBox.Show("Email required"); TxtEmail.Focus(); return false; }
            if (string.IsNullOrWhiteSpace(TxtPhone.Text)) { MessageBox.Show("Phone required"); TxtPhone.Focus(); return false; }
            if (CmbRole.SelectedItem == null) { MessageBox.Show("Role required"); CmbRole.Focus(); return false; }
            if (_isAddingNew && string.IsNullOrWhiteSpace(TxtEmployeeID.Text)) { MessageBox.Show("Employee ID required"); TxtEmployeeID.Focus(); return false; }
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}