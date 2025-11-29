using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Attendify.Views.Employee
{
    public class EmployeeDashboardViewModel : INotifyPropertyChanged
    {
        private string _currentPageTitle = "Dashboard Overview";
        private string _currentDate = "";
        private string _currentTime = "";
        private string _currentShift = "";
        private object _currentView;

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

        public string CurrentTime
        {
            get => _currentTime;
            set { _currentTime = value; OnPropertyChanged(); }
        }

        public string CurrentShift
        {
            get => _currentShift;
            set { _currentShift = value; OnPropertyChanged(); }
        }

        public object CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}