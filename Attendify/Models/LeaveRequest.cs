using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;

namespace Attendify.Models
{
    public class LeaveRequest
    {
        public int LeaveID { get; set; }
        public int EmployeeID { get; set; }
        public string EmpName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; } // Pending / Approved / Rejected
        public string Department { get; set; }
        public string Position { get; set; }
        public string Email { get; set; }
    }
}

