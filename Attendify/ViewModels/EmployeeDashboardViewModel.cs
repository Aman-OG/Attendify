using System.ComponentModel;

public class EmployeeDashboardViewModel : INotifyPropertyChanged
{
    private string _currentPageTitle = "Dashboard";
    private string _currentDate = "";
    private string _currentTime = "";
    private string _currentShift = "";
    private string _employeeName = "";
    private string _fullName = "";
    private string _department = "";
    private string _position = "";

    public event PropertyChangedEventHandler PropertyChanged;

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

    public string CurrentTime
    {
        get => _currentTime;
        set { _currentTime = value; OnPropertyChanged(nameof(CurrentTime)); }
    }

    public string CurrentShift
    {
        get => _currentShift;
        set { _currentShift = value; OnPropertyChanged(nameof(CurrentShift)); }
    }

    public string EmployeeName
    {
        get => _employeeName;
        set { _employeeName = value; OnPropertyChanged(nameof(EmployeeName)); }
    }

    public string FullName
    {
        get => _fullName;
        set { _fullName = value; OnPropertyChanged(nameof(FullName)); }
    }

    public string Department
    {
        get => _department;
        set { _department = value; OnPropertyChanged(nameof(Department)); }
    }

    public string Position
    {
        get => _position;
        set { _position = value; OnPropertyChanged(nameof(Position)); }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}