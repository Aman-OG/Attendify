using Attendify.API.Data;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace Attendify.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // GET: api/reports
        [HttpGet]
        public async Task<ActionResult<ReportResponse>> GetReport(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var (queryStartDate, queryEndDate) = ValidateDates(startDate, endDate);
                var response = new ReportResponse
                {
                    DateRange = new DateRange
                    {
                        StartDate = queryStartDate,
                        EndDate = queryEndDate
                    }
                };

                response.KpiStats = await GetKpiStats(queryStartDate, queryEndDate);
                response.AttendanceTrend = await GetAttendanceTrend();
                response.LeaveDistribution = await GetLeaveDistribution(queryStartDate, queryEndDate);
                response.DepartmentLeave = await GetDepartmentLeave(queryStartDate, queryEndDate);
                response.PerformanceGauge = await GetPerformanceGauge(queryStartDate, queryEndDate);
                response.QuickSummary = await GetQuickSummary(queryStartDate, queryEndDate);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/reports/export/attendance/pdf?startDate=&endDate=
        [HttpGet("export/attendance/pdf")]
        public async Task<IActionResult> ExportAttendancePdf(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var (queryStartDate, queryEndDate) = ValidateDates(startDate, endDate);

                var attendanceData = await GetAttendanceReportData(queryStartDate, queryEndDate);
                var pdfBytes = GenerateAttendancePdf(attendanceData, queryStartDate, queryEndDate);

                return File(pdfBytes, "application/pdf",
                    $"Attendance_Report_{queryStartDate:yyyyMMdd}_{queryEndDate:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating PDF: {ex.Message}");
            }
        }

        // GET: api/reports/export/leave/excel?startDate=&endDate=
        [HttpGet("export/leave/excel")]
        public async Task<IActionResult> ExportLeaveExcel(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var (queryStartDate, queryEndDate) = ValidateDates(startDate, endDate);

                var leaveData = await GetLeaveReportData(queryStartDate, queryEndDate);
                var excelBytes = GenerateLeaveExcel(leaveData, queryStartDate, queryEndDate);

                return File(excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Leave_Summary_{queryStartDate:yyyyMMdd}_{queryEndDate:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating Excel: {ex.Message}");
            }
        }

        // GET: api/reports/export/employees/csv
        [HttpGet("export/employees/csv")]
        public async Task<IActionResult> ExportEmployeesCsv()
        {
            try
            {
                var employees = await _context.Employees
                    .Where(e => e.IsActive)
                    .Select(e => new EmployeeExportDto
                    {
                        EmployeeID = e.EmployeeID,
                        EmpCode = e.EmpCode,
                        FirstName = e.FirstName,
                        LastName = e.LastName,
                        Email = e.Email,
                        Department = e.Department,
                        Position = e.Position,
                        Phone = e.Phone,
                        IsActive = e.IsActive
                    })
                    .ToListAsync();

                var csvContent = GenerateEmployeeCsv(employees);

                return File(csvContent, "text/csv", $"Employee_List_{DateTime.Today:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating CSV: {ex.Message}");
            }
        }

        // GET: api/reports/export/analytics/pdf?startDate=&endDate=
        [HttpGet("export/analytics/pdf")]
        public async Task<IActionResult> ExportAnalyticsPdf(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var (queryStartDate, queryEndDate) = ValidateDates(startDate, endDate);

                var report = new ReportResponse
                {
                    DateRange = new DateRange { StartDate = queryStartDate, EndDate = queryEndDate },
                    KpiStats = await GetKpiStats(queryStartDate, queryEndDate),
                    LeaveDistribution = await GetLeaveDistribution(queryStartDate, queryEndDate),
                    QuickSummary = await GetQuickSummary(queryStartDate, queryEndDate)
                };

                var pdfBytes = GenerateAnalyticsPdf(report);

                return File(pdfBytes, "application/pdf",
                    $"Analytics_Report_{queryStartDate:yyyyMMdd}_{queryEndDate:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating Analytics PDF: {ex.Message}");
            }
        }

        #region Private Methods

        private (DateTime, DateTime) ValidateDates(DateTime? startDate, DateTime? endDate)
        {
            var queryStartDate = startDate ?? DateTime.Today.AddDays(-30);
            var queryEndDate = endDate ?? DateTime.Today;

            if (queryEndDate < queryStartDate)
                queryEndDate = queryStartDate.AddDays(30);

            return (queryStartDate, queryEndDate);
        }

        private async Task<KpiStats> GetKpiStats(DateTime startDate, DateTime endDate)
        {
            var totalEmployees = await _context.Employees
                .Where(e => e.IsActive)
                .CountAsync();

            var today = DateTime.Today;
            var todayPresent = await _context.Attendance
                .Where(a => a.Date.Date == today && (a.Status == "Present" || a.Status == "Late"))
                .CountAsync();

            var todayAttendancePercentage = totalEmployees > 0
                ? Math.Round((todayPresent * 100.0) / totalEmployees, 1)
                : 0;

            var pendingLeaves = await _context.LeaveRequests
                .Where(l => l.Status == "Pending" && l.StartDate <= endDate && l.EndDate >= startDate)
                .CountAsync();

            var totalAttendance = await _context.Attendance
                .Where(a => a.Date >= startDate && a.Date <= endDate && (a.Status == "Present" || a.Status == "Late"))
                .CountAsync();

            var totalExpected = totalEmployees * (endDate - startDate).Days;
            var attendanceRate = totalExpected > 0
                ? Math.Round((totalAttendance * 100.0) / totalExpected, 1)
                : 0;

            return new KpiStats
            {
                TotalEmployees = totalEmployees,
                TodayAttendancePercentage = todayAttendancePercentage,
                PendingLeaves = pendingLeaves,
                AttendanceRate = attendanceRate
            };
        }

        private async Task<AttendanceTrend> GetAttendanceTrend()
        {
            var trend = new AttendanceTrend();
            var totalEmployees = await _context.Employees.Where(e => e.IsActive).CountAsync();

            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Today.AddDays(-i))
                .OrderBy(d => d)
                .ToList();

            foreach (var day in last7Days)
            {
                var dayAttendance = await _context.Attendance
                    .Where(a => a.Date.Date == day.Date && (a.Status == "Present" || a.Status == "Late"))
                    .CountAsync();

                var percentage = totalEmployees > 0
                    ? Math.Round((dayAttendance * 100.0) / totalEmployees, 1)
                    : 0;

                trend.Labels.Add(day.ToString("ddd"));
                trend.Values.Add(percentage);
            }

            return trend;
        }

        private async Task<LeaveDistribution> GetLeaveDistribution(DateTime startDate, DateTime endDate)
        {
            var leaves = await _context.LeaveRequests
                .Where(l => l.StartDate <= endDate && l.EndDate >= startDate)
                .GroupBy(l => l.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return new LeaveDistribution
            {
                Approved = leaves.FirstOrDefault(l => l.Status == "Approved")?.Count ?? 0,
                Pending = leaves.FirstOrDefault(l => l.Status == "Pending")?.Count ?? 0,
                Rejected = leaves.FirstOrDefault(l => l.Status == "Rejected")?.Count ?? 0,
                Cancelled = leaves.FirstOrDefault(l => l.Status == "Cancelled")?.Count ?? 0
            };
        }

        private async Task<DepartmentLeave> GetDepartmentLeave(DateTime startDate, DateTime endDate)
        {
            var departmentLeaves = await _context.LeaveRequests
                .Where(l => l.StartDate <= endDate && l.EndDate >= startDate)
                .Join(_context.Employees,
                    leave => leave.EmpCode,
                    emp => emp.EmpCode,
                    (leave, emp) => new { emp.Department })
                .GroupBy(x => x.Department)
                .Select(g => new { Department = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            var result = new DepartmentLeave();
            foreach (var dept in departmentLeaves)
            {
                result.Departments.Add(dept.Department ?? "Unknown");
                result.LeaveCounts.Add(dept.Count);
            }

            return result;
        }

        private async Task<PerformanceGauge> GetPerformanceGauge(DateTime startDate, DateTime endDate)
        {
            var totalEmployees = await _context.Employees.Where(e => e.IsActive).CountAsync();
            var totalDays = (endDate - startDate).Days > 0 ? (endDate - startDate).Days : 1;

            var totalPresent = await _context.Attendance
                .Where(a => a.Date >= startDate && a.Date <= endDate && a.Status == "Present")
                .CountAsync();

            var totalLate = await _context.Attendance
                .Where(a => a.Date >= startDate && a.Date <= endDate && a.Status == "Late")
                .CountAsync();

            var totalExpected = totalEmployees * totalDays;
            var attendancePercentage = totalExpected > 0
                ? Math.Round((totalPresent * 100.0) / totalExpected, 1)
                : 0;

            var onTimePercentage = (totalPresent + totalLate) > 0
                ? Math.Round((totalPresent * 100.0) / (totalPresent + totalLate), 1)
                : 0;

            var totalLeaves = await _context.LeaveRequests
                .Where(l => l.Status == "Approved" && l.StartDate <= endDate && l.EndDate >= startDate)
                .CountAsync();

            var leavePercentage = totalExpected > 0
                ? Math.Round((totalLeaves * 100.0) / totalExpected, 1)
                : 0;

            return new PerformanceGauge
            {
                AttendancePercentage = attendancePercentage,
                OnTimePercentage = onTimePercentage,
                LeavePercentage = leavePercentage
            };
        }

        private async Task<QuickSummary> GetQuickSummary(DateTime startDate, DateTime endDate)
        {
            var totalEmployees = await _context.Employees.Where(e => e.IsActive).CountAsync();
            var totalDays = (endDate - startDate).Days > 0 ? (endDate - startDate).Days : 1;

            var totalPresent = await _context.Attendance
                .Where(a => a.Date >= startDate && a.Date <= endDate && (a.Status == "Present" || a.Status == "Late"))
                .CountAsync();

            var averageDailyAttendance = Math.Round((totalPresent * 100.0) / (totalEmployees * totalDays), 1);

            var totalLeaveRequests = await _context.LeaveRequests
                .Where(l => l.StartDate <= endDate && l.EndDate >= startDate)
                .CountAsync();

            var mostActiveDepartment = await _context.LeaveRequests
                .Where(l => l.StartDate <= endDate && l.EndDate >= startDate)
                .Join(_context.Employees,
                    leave => leave.EmpCode,
                    emp => emp.EmpCode,
                    (leave, emp) => emp.Department)
                .GroupBy(d => d)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync();

            var bestAttendanceDepartment = await _context.Attendance
                .Where(a => a.Date >= startDate && a.Date <= endDate && a.Status == "Present")
                .Join(_context.Employees,
                    att => att.EmpCode,
                    emp => emp.EmpCode,
                    (att, emp) => emp.Department)
                .GroupBy(d => d)
                .Select(g => new { Department = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Select(x => x.Department)
                .FirstOrDefaultAsync();

            return new QuickSummary
            {
                Period = $"{startDate:MMM dd} - {endDate:MMM dd}",
                AverageDailyAttendance = averageDailyAttendance,
                TotalLeaveRequests = totalLeaveRequests,
                MostActiveDepartment = mostActiveDepartment ?? "N/A",
                BestAttendanceDepartment = bestAttendanceDepartment ?? "N/A"
            };
        }

        private async Task<List<AttendanceExportDto>> GetAttendanceReportData(DateTime startDate, DateTime endDate)
        {
            return await _context.Attendance
                .Where(a => a.Date >= startDate && a.Date <= endDate)
                .Join(_context.Employees,
                    attendance => attendance.EmpCode,
                    employee => employee.EmpCode,
                    (attendance, employee) => new AttendanceExportDto
                    {
                        Date = attendance.Date,
                        EmployeeID = employee.EmployeeID,
                        EmployeeName = $"{employee.FirstName} {employee.LastName}",
                        Department = employee.Department,
                        Status = attendance.Status,
                        CheckInTime = attendance.CheckInTime ?? "N/A"
                    })
                .OrderByDescending(a => a.Date)
                .ThenBy(a => a.Department)
                .ToListAsync();
        }

        private async Task<List<LeaveExportDto>> GetLeaveReportData(DateTime startDate, DateTime endDate)
        {
            return await _context.LeaveRequests
                .Where(l => l.StartDate <= endDate && l.EndDate >= startDate)
                .Join(_context.Employees,
                    leave => leave.EmpCode,
                    employee => employee.EmpCode,
                    (leave, employee) => new LeaveExportDto
                    {
                        LeaveID = leave.LeaveID,
                        EmployeeID = employee.EmployeeID,
                        EmployeeName = $"{employee.FirstName} {employee.LastName}",
                        Department = employee.Department,
                        LeaveType = leave.LeaveType,
                        StartDate = leave.StartDate,
                        EndDate = leave.EndDate,
                        Days = leave.Days,
                        Status = leave.Status,
                        Reason = leave.Reason,
                        AppliedDate = leave.AppliedDate
                    })
                .OrderByDescending(l => l.AppliedDate)
                .ThenBy(l => l.Status)
                .ToListAsync();
        }

        private byte[] GenerateAttendancePdf(List<AttendanceExportDto> data, DateTime startDate, DateTime endDate)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header()
                        .AlignCenter()
                        .Text("ATTENDIFY - ATTENDANCE REPORT")
                        .SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Spacing(10);

                            // Report info
                            x.Item().Text($"Period: {startDate:dd/MM/yyyy} to {endDate:dd/MM/yyyy}");
                            x.Item().Text($"Generated on: {DateTime.Now:dd/MM/yyyy HH:mm}");
                            x.Item().Text($"Total Records: {data.Count}");

                            // Table
                            if (data.Any())
                            {
                                x.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(); // Date
                                        columns.RelativeColumn(); // Employee ID
                                        columns.RelativeColumn(2); // Name
                                        columns.RelativeColumn(); // Department
                                        columns.RelativeColumn(); // Status
                                        columns.RelativeColumn(); // Check-in
                                    });

                                    // Header
                                    table.Header(header =>
                                    {
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Date");
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Employee ID");
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Employee Name");
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Department");
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Status");
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Check-in Time");
                                    });

                                    // Data rows
                                    foreach (var item in data)
                                    {
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Date.ToString("dd/MM/yyyy"));
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.EmployeeID);
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.EmployeeName);
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Department);
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Status);
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.CheckInTime);
                                    }
                                });
                            }
                            else
                            {
                                x.Item().Text("No attendance records found for the selected period.").Italic();
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        private byte[] GenerateLeaveExcel(List<LeaveExportDto> data, DateTime startDate, DateTime endDate)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Leave Summary");

                // Title
                worksheet.Cell(1, 1).Value = "ATTENDIFY - LEAVE SUMMARY REPORT";
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.Blue;
                worksheet.Range(1, 1, 1, 11).Merge();

                worksheet.Cell(2, 1).Value = $"Period: {startDate:dd/MM/yyyy} to {endDate:dd/MM/yyyy}";
                worksheet.Cell(3, 1).Value = $"Generated on: {DateTime.Now:dd/MM/yyyy HH:mm}";
                worksheet.Cell(4, 1).Value = $"Total Leave Requests: {data.Count}";

                // Headers
                int row = 6;
                string[] headers = { "Leave ID", "Employee ID", "Employee Name", "Department", "Leave Type",
                                   "Start Date", "End Date", "Days", "Status", "Reason", "Applied Date" };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(row, i + 1).Value = headers[i];
                    worksheet.Cell(row, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
                    worksheet.Cell(row, i + 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                // Data
                row++;
                foreach (var item in data)
                {
                    worksheet.Cell(row, 1).Value = item.LeaveID;
                    worksheet.Cell(row, 2).Value = item.EmployeeID;
                    worksheet.Cell(row, 3).Value = item.EmployeeName;
                    worksheet.Cell(row, 4).Value = item.Department;
                    worksheet.Cell(row, 5).Value = item.LeaveType;
                    worksheet.Cell(row, 6).Value = item.StartDate.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 7).Value = item.EndDate.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 8).Value = item.Days;
                    worksheet.Cell(row, 9).Value = item.Status;
                    worksheet.Cell(row, 10).Value = item.Reason;
                    worksheet.Cell(row, 11).Value = item.AppliedDate.ToString("dd/MM/yyyy");

                    // Add borders to data cells
                    for (int col = 1; col <= 11; col++)
                    {
                        worksheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }
                    row++;
                }

                // Auto fit columns
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        private byte[] GenerateEmployeeCsv(List<EmployeeExportDto> employees)
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                // Write header
                writer.WriteLine("EmployeeID,EmpCode,FirstName,LastName,Email,Department,Position,Phone,IsActive");

                // Write data
                foreach (var emp in employees)
                {
                    writer.WriteLine($"{emp.EmployeeID},{emp.EmpCode},{emp.FirstName},{emp.LastName}," +
                                    $"{emp.Email},{emp.Department},{emp.Position},{emp.Phone},{emp.IsActive}");
                }

                writer.Flush();
                return stream.ToArray();
            }
        }

        private byte[] GenerateAnalyticsPdf(ReportResponse report)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header()
                        .AlignCenter()
                        .Text("ATTENDIFY - ANALYTICS REPORT")
                        .SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Spacing(15);

                            // Report info
                            x.Item().Text($"Period: {report.DateRange.StartDate:dd/MM/yyyy} to {report.DateRange.EndDate:dd/MM/yyyy}");
                            x.Item().Text($"Generated on: {DateTime.Now:dd/MM/yyyy HH:mm}");

                            // KPI Stats
                            x.Item().Text("KEY PERFORMANCE INDICATORS").SemiBold().FontSize(14);
                            x.Item().Grid(grid =>
                            {
                                grid.Columns(2);
                                grid.Spacing(10);

                                grid.Item().Background(Colors.Grey.Lighten4).Padding(10).Text($"Total Employees: {report.KpiStats.TotalEmployees}");
                                grid.Item().Background(Colors.Grey.Lighten4).Padding(10).Text($"Today's Attendance: {report.KpiStats.TodayAttendancePercentage}%");
                                grid.Item().Background(Colors.Grey.Lighten4).Padding(10).Text($"Pending Leaves: {report.KpiStats.PendingLeaves}");
                                grid.Item().Background(Colors.Grey.Lighten4).Padding(10).Text($"Attendance Rate: {report.KpiStats.AttendanceRate}%");
                            });

                            // Leave Distribution
                            x.Item().Text("LEAVE DISTRIBUTION").SemiBold().FontSize(14);
                            x.Item().Grid(grid =>
                            {
                                grid.Columns(2);
                                grid.Spacing(10);

                                grid.Item().Background(Colors.Green.Lighten4).Padding(10).Text($"Approved: {report.LeaveDistribution.Approved}");
                                grid.Item().Background(Colors.Yellow.Lighten4).Padding(10).Text($"Pending: {report.LeaveDistribution.Pending}");
                                grid.Item().Background(Colors.Red.Lighten4).Padding(10).Text($"Rejected: {report.LeaveDistribution.Rejected}");
                                grid.Item().Background(Colors.Orange.Lighten4).Padding(10).Text($"Cancelled: {report.LeaveDistribution.Cancelled}");
                            });

                            // Quick Summary
                            x.Item().Text("QUICK SUMMARY").SemiBold().FontSize(14);
                            x.Item().Grid(grid =>
                            {
                                grid.Columns(2);
                                grid.Spacing(10);

                                grid.Item().Padding(10).Text($"Period: {report.QuickSummary.Period}");
                                grid.Item().Padding(10).Text($"Average Daily Attendance: {report.QuickSummary.AverageDailyAttendance}%");
                                grid.Item().Padding(10).Text($"Total Leave Requests: {report.QuickSummary.TotalLeaveRequests}");
                                grid.Item().Padding(10).Text($"Most Active Department: {report.QuickSummary.MostActiveDepartment}");
                                grid.Item().Padding(10).Text($"Best Attendance Department: {report.QuickSummary.BestAttendanceDepartment}");
                            });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        #endregion
    }

    #region DTO Classes

    public class ReportResponse
    {
        public DateRange DateRange { get; set; } = new();
        public KpiStats KpiStats { get; set; } = new();
        public AttendanceTrend AttendanceTrend { get; set; } = new();
        public LeaveDistribution LeaveDistribution { get; set; } = new();
        public DepartmentLeave DepartmentLeave { get; set; } = new();
        public PerformanceGauge PerformanceGauge { get; set; } = new();
        public QuickSummary QuickSummary { get; set; } = new();
    }

    public class DateRange
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string PeriodDisplay => $"{StartDate:MMM dd} - {EndDate:MMM dd}";
        public int Days => (EndDate - StartDate).Days;
    }

    public class KpiStats
    {
        public int TotalEmployees { get; set; }
        public double TodayAttendancePercentage { get; set; }
        public int PendingLeaves { get; set; }
        public double AttendanceRate { get; set; }
    }

    public class AttendanceTrend
    {
        public List<string> Labels { get; set; } = new();
        public List<double> Values { get; set; } = new();
    }

    public class LeaveDistribution
    {
        public int Approved { get; set; }
        public int Pending { get; set; }
        public int Rejected { get; set; }
        public int Cancelled { get; set; }
    }

    public class DepartmentLeave
    {
        public List<string> Departments { get; set; } = new();
        public List<int> LeaveCounts { get; set; } = new();
    }

    public class PerformanceGauge
    {
        public double AttendancePercentage { get; set; }
        public double OnTimePercentage { get; set; }
        public double LeavePercentage { get; set; }
    }

    public class QuickSummary
    {
        public string Period { get; set; } = string.Empty;
        public double AverageDailyAttendance { get; set; }
        public int TotalLeaveRequests { get; set; }
        public string MostActiveDepartment { get; set; } = string.Empty;
        public string BestAttendanceDepartment { get; set; } = string.Empty;
    }

    public class AttendanceExportDto
    {
        public DateTime Date { get; set; }
        public string EmployeeID { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CheckInTime { get; set; } = string.Empty;
    }

    public class LeaveExportDto
    {
        public int LeaveID { get; set; }
        public string EmployeeID { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string LeaveType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Days { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime AppliedDate { get; set; }
    }

    public class EmployeeExportDto
    {
        public string EmployeeID { get; set; } = string.Empty;
        public string EmpCode { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    #endregion
}