using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace Attendify.Services
{
    public static class DatabaseService
    {
        private static string dbFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database");
        private static string dbPath = Path.Combine(dbFolder, "attendify.db");
        private static string connectionString = $"Data Source={dbPath}";

        public static string ConnectionString => connectionString;

        public static void InitializeDatabase()
        {
            try
            {
                if (!Directory.Exists(dbFolder))
                    Directory.CreateDirectory(dbFolder);

                if (!File.Exists(dbPath))
                    File.Create(dbPath).Close();

                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();

                    string createEmployeeTable = @"
                        CREATE TABLE IF NOT EXISTS Employees (
                            EmployeeID INTEGER PRIMARY KEY AUTOINCREMENT,
                            FirstName TEXT NOT NULL,
                            MiddleName TEXT,
                            LastName TEXT NOT NULL,
                            Department TEXT,
                            Position TEXT,
                            Email TEXT UNIQUE,
                            PasswordHash TEXT,
                            Role TEXT DEFAULT 'Employee',
                            Phone TEXT,
                            IsActive INTEGER DEFAULT 1,
                            CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
                        );
                    ";

                    string createAttendanceTable = @"
                        CREATE TABLE IF NOT EXISTS Attendance (
                            AttendanceID INTEGER PRIMARY KEY AUTOINCREMENT,
                            EmployeeID INTEGER NOT NULL,
                            Date TEXT NOT NULL,
                            CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                            FOREIGN KEY(EmployeeID) REFERENCES Employees(EmployeeID)
                        );
                    ";

                    string createLeavesTable = @"
                        CREATE TABLE IF NOT EXISTS Leaves (
                            LeaveID INTEGER PRIMARY KEY AUTOINCREMENT,
                            EmployeeID INTEGER NOT NULL,
                            FromDate TEXT,
                            ToDate TEXT,
                            Reason TEXT,
                            Status TEXT,
                            CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                            FOREIGN KEY(EmployeeID) REFERENCES Employees(EmployeeID)
                        );
                    ";

                    string createAttendanceRulesTable = @"
                        CREATE TABLE IF NOT EXISTS AttendanceRules (
                            RuleID INTEGER PRIMARY KEY AUTOINCREMENT,
                            DayOfWeek TEXT NOT NULL,
                            StartTime TEXT NOT NULL,
                            EndTime TEXT NOT NULL,
                            GracePeriodMinutes INTEGER DEFAULT 0,
                            CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
                        );
                    ";

                    string createShiftsTable = @"
                        CREATE TABLE IF NOT EXISTS Shifts (
                            ShiftID INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL,
                            StartTime TEXT NOT NULL,
                            EndTime TEXT NOT NULL,
                            GracePeriodMinutes INTEGER DEFAULT 0,
                            CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
                        );
                    ";

                    string createAttendancePerShiftTable = @"
                        CREATE TABLE IF NOT EXISTS AttendancePerShift (
                            AttendanceShiftID INTEGER PRIMARY KEY AUTOINCREMENT,
                            AttendanceID INTEGER NOT NULL,
                            ShiftID INTEGER NOT NULL,
                            CheckInTime TEXT,
                            Status TEXT,
                            CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                            FOREIGN KEY(AttendanceID) REFERENCES Attendance(AttendanceID),
                            FOREIGN KEY(ShiftID) REFERENCES Shifts(ShiftID)
                        );
                    ";

                    string createMessagesTable = @"
                        CREATE TABLE IF NOT EXISTS AdminMessages (
                            MessageID INTEGER PRIMARY KEY AUTOINCREMENT,
                            Title TEXT,
                            Body TEXT NOT NULL,
                            IsActive INTEGER DEFAULT 1,
                            CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
                        );
                    ";

                    string createEmployeeRequestTable = @"
                        CREATE TABLE IF NOT EXISTS EmployeeRequests (
                            RequestID INTEGER PRIMARY KEY AUTOINCREMENT,
                            EmployeeID INTEGER NOT NULL,
                            Date TEXT,
                            Type TEXT NOT NULL,     -- Late, Absence, Correction, Other
                            Message TEXT NOT NULL,  
                            Status TEXT DEFAULT 'Pending',   -- Pending / Approved / Rejected
                            AdminReply TEXT,
                            CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                            FOREIGN KEY(EmployeeID) REFERENCES Employees(EmployeeID)
                        );
                    ";

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = createEmployeeTable;
                        command.ExecuteNonQuery();

                        command.CommandText = createAttendanceTable;
                        command.ExecuteNonQuery();

                        command.CommandText = createLeavesTable;
                        command.ExecuteNonQuery();

                        command.CommandText = createAttendanceRulesTable;
                        command.ExecuteNonQuery();

                        command.CommandText = createShiftsTable;
                        command.ExecuteNonQuery();

                        command.CommandText = createAttendancePerShiftTable;
                        command.ExecuteNonQuery();

                        command.CommandText = createMessagesTable;
                        command.ExecuteNonQuery();

                        command.CommandText = createEmployeeRequestTable;
                        command.ExecuteNonQuery();
                    }

                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Database initialization failed: " + ex.Message);
            }
        }
    }
}
