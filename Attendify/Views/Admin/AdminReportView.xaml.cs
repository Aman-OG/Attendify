using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Attendify.ViewModels;

namespace Attendify.Views.UserControls
{
    public partial class ReportsView : UserControl
    {
        private readonly AdminReportViewModel _viewModel;

        public ReportsView()
        {
            InitializeComponent();

            _viewModel = new AdminReportViewModel();
            DataContext = _viewModel;

            // Set default dates in DatePickers
            StartDatePicker.SelectedDate = _viewModel.StartDate;
            EndDatePicker.SelectedDate = _viewModel.EndDate;

            // Load initial data
            Loaded += async (s, e) => await LoadInitialDataAsync();
        }

        private async Task LoadInitialDataAsync()
        {
            await _viewModel.LoadReportsAsync();
        }

        private async void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                GlassMessageBox.Show("Please select both start and end dates.", "Date Range Required");
                return;
            }

            if (StartDatePicker.SelectedDate > EndDatePicker.SelectedDate)
            {
                GlassMessageBox.Show("Start date cannot be after end date.", "Invalid Date Range");
                return;
            }

            _viewModel.StartDate = StartDatePicker.SelectedDate.Value;
            _viewModel.EndDate = EndDatePicker.SelectedDate.Value;

            await _viewModel.LoadReportsAsync();
        }
    }
}