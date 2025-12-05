using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attendify.DATA.Models

{
    public class Employee
    {
        public int EmployeeID { get; set; }   // PK
        public string FirstName { get; set; } = null!;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = null!;
        public string? Department { get; set; }
        public string? Position { get; set; }
        public string? Email { get; set; }
        public string? PasswordHash { get; set; }
        public string Role { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Attendance>? Attendance { get; set; }
        public ICollection<Leave>? Leaves { get; set; }
        public ICollection<EmployeeRequest>? EmployeeRequests { get; set; }
    }
}

