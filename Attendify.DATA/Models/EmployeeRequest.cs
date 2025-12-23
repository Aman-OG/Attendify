using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Attendify.DATA.Models
{
    public class EmployeeRequest
    {
        [Key]
        public int RequestID { get; set; }

        public string? EmpCode { get; set; }

        public string? Date { get; set; }

        public string Type { get; set; } = string.Empty;   // Late / Absence / Correction / Other

        public string Message { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";    // Pending / Approved / Rejected

        public string? AdminReply { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation (optional)
        [ForeignKey("EmployeeID")]
        public Employee Employee { get; set; }
    }
}

