using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attendify.Models
{
    public class Employee
    {
        public string EmployeeID { get; set; } = "";
        public string EmpID { get; set; } = ""; // Add this
        public string No { get; set; } = ""; // Add this
        public string FirstName { get; set; } = "";
        public string MiddleName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Department { get; set; } = "";
        public string Position { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Status { get; set; } = "Active"; // Add this with default value
    }
}