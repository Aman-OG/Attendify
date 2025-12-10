using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attendify.DATA.Models

{
    public class Leave
    {
        public int LeaveID { get; set; }       // PK
        public string EmpCode { get; set; }    // FK

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? ReasonTitle { get; set; }
        public string? Detail { get; set; }
        public string? Status { get; set; }
        public string? AdminResponse { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Employee? Employee { get; set; }
    }
}
