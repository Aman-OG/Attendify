// Attendify/ViewModels/AdminDashboardViewModel.cs
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Attendify.DATA.Models; // ← Your real Employee model with EmpCode & IsActive

namespace Attendify.ViewModels
{
    public class AdminDashboardViewModel : INotifyPropertyChanged
    {
        private string _currentPageTitle = "Employee Attendance - Day";
        private string _currentDate = "--";
        private string _currentShift = "Morning Shift";
        private string _currentTime = "00 : 00 : 00";

        public ObservableCollection<EmployeeDisplayItem> Employees { get; set; } = new();

        public string CurrentPageTitle
        {
            get => _currentPageTitle;
            set { _currentPageTitle = value; OnPropertyChanged(); }
        }

        public string CurrentDate
        {
            get => _currentDate;
            set { _currentDate = value; OnPropertyChanged(); }
        }

        public string CurrentShift
        {
            get => _currentShift;
            set { _currentShift = value; OnPropertyChanged(); }
        }

        public string CurrentTime
        {
            get => _currentTime;
            set { _currentTime = value; OnPropertyChanged(); }
        }

        public AdminDashboardViewModel()
        {
            LoadSampleEmployees();
        }

        private void LoadSampleEmployees()
        {
            // Sample data - will be replaced with real API call later
            var employees = new[]
            {
                new Employee { EmpCode = "EMP10001", FirstName = "Aman",    LastName = "Baye",     Email = "am@gmail.com",     Department = "HR",        Position = "Manager",      IsActive = true },
                new Employee { EmpCode = "EMP10002", FirstName = "Teddy",   LastName = "K",        Email = "teddy@g.com",      Department = "Software",  Position = "Developer",    IsActive = true },
                new Employee { EmpCode = "EMP10003", FirstName = "Selam",   LastName = "Tadesse",  Email = "selam@co.et",      Department = "Finance",   Position = "Accountant",   IsActive = false },
                new Employee { EmpCode = "EMP10004", FirstName = "Dawit",   LastName = "Kebede",   Email = "dawit@co.et",      Department = "Marketing", Position = "Coordinator", IsActive = true }
            };

            // Convert to display items with row number (No)
            var displayItems = employees.Select((emp, index) => new EmployeeDisplayItem
            {
                No = (index + 1).ToString("D2"),           // 01, 02, 03...
                EmpID = emp.EmpCode,                       // ← EmpCode becomes EmpID in UI
                FirstName = emp.FirstName,
                LastName = emp.LastName,
                Email = emp.Email ?? "",
                Department = emp.Department ?? "",
                Position = emp.Position ?? "",
                Status = emp.IsActive ? "Active" : "Inactive"  // ← IsActive → Status text
            });

            Employees = new ObservableCollection<EmployeeDisplayItem>(displayItems);
            OnPropertyChanged(nameof(Employees));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Small DTO just for the dashboard display
    public class EmployeeDisplayItem
    {
        public string No { get; set; } = "";
        public string EmpID { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Department { get; set; } = "";
        public string Position { get; set; } = "";
        public string Status { get; set; } = "";  // "Active" or "Inactive"
    }
}