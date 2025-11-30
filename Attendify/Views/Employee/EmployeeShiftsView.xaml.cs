using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Attendify.Views.Employee
{
    public partial class EmployeeShiftsView : UserControl
    {
        public EmployeeShiftsView()
        {
            InitializeComponent();
            Loaded += EmployeeShiftsView_Loaded;
        }

        private void EmployeeShiftsView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCalendarData();
        }

        private void LoadCalendarData()
        {
            var calendarDays = new List<CalendarDay>();
            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);

            // Add empty days for the start of the month
            int startDay = (int)firstDayOfMonth.DayOfWeek;
            for (int i = 0; i < startDay; i++)
            {
                calendarDays.Add(new CalendarDay { DayNumber = "", ShiftInfo = "" });
            }

            // Add days of the month
            for (int day = 1; day <= daysInMonth; day++)
            {
                var currentDate = new DateTime(today.Year, today.Month, day);
                var calendarDay = new CalendarDay
                {
                    DayNumber = day.ToString(),
                    DayNumberColor = Brushes.White
                };

                // Set current day style
                if (currentDate == today)
                {
                    calendarDay.DayStyle = (Style)FindResource("CurrentDayStyle");
                }
                else
                {
                    calendarDay.DayStyle = (Style)FindResource("CalendarDayStyle");
                }

                // Assign shifts based on day of week
                if (currentDate.DayOfWeek >= DayOfWeek.Monday && currentDate.DayOfWeek <= DayOfWeek.Friday)
                {
                    calendarDay.ShiftInfo = "Morning\n08:00-14:00";
                    calendarDay.ShiftColor = new SolidColorBrush(Color.FromRgb(56, 176, 0)); // #38b000
                    if (currentDate != today)
                    {
                        calendarDay.DayStyle = (Style)FindResource("ShiftDayStyle");
                    }
                }
                else if (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    calendarDay.ShiftInfo = "Evening\n14:00-22:00";
                    calendarDay.ShiftColor = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // #FF9800
                    if (currentDate != today)
                    {
                        calendarDay.DayStyle = (Style)FindResource("ShiftDayStyle");
                    }
                }

                calendarDays.Add(calendarDay);
            }

            CalendarGrid.ItemsSource = calendarDays;
        }

        private void PreviousMonth_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Previous month navigation coming soon!", "Navigation",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Next month navigation coming soon!", "Navigation",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public class CalendarDay
    {
        public string DayNumber { get; set; }
        public Brush DayNumberColor { get; set; }
        public string ShiftInfo { get; set; }
        public Brush ShiftColor { get; set; }
        public Style DayStyle { get; set; }
    }
}