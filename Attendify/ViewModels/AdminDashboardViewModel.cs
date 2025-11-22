using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;


using Attendify.Models;

using System.Collections.ObjectModel;


namespace Attendify.ViewModels
{
    public class AdminDashboardViewModel : INotifyPropertyChanged
    {
        private string _currentPageTitle = "Employee Attendance - Day";
        private string _currentDate = "--";
        private string _currentShift = "Morning Shift";
        private string _currentTime = "00 : 00 : 00";

        public ObservableCollection<Employee> Employees { get; set; }

        public string CurrentPageTitle
        {
            get => _currentPageTitle;
            set { _currentPageTitle = value; OnPropertyChanged(nameof(CurrentPageTitle)); }
        }

        public string CurrentDate
        {
            get => _currentDate;
            set { _currentDate = value; OnPropertyChanged(nameof(CurrentDate)); }
        }

        public string CurrentShift
        {
            get => _currentShift;
            set { _currentShift = value; OnPropertyChanged(nameof(CurrentShift)); }
        }

        public string CurrentTime
        {
            get => _currentTime;
            set { _currentTime = value; OnPropertyChanged(nameof(CurrentTime)); }
        }

        public AdminDashboardViewModel()
        {
            Employees = new ObservableCollection<Employee>
            {
                new Employee { No = "01", EmpID = "emp10002", FirstName = "Aman", LastName = "Baye",
                             Email = "am@gmail.com", Department = "HR", Position = "Manager", Status = "Active" },
                new Employee { No = "02", EmpID = "emp10003", FirstName = "Teddy", LastName = "K",
                             Email = "teddy@g.com", Department = "Software", Position = "Dev", Status = "Attended" }
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}