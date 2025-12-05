using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Attendify.DATA.Models
{
    public class AttendancePerShift
    {
        [Key]
        public int AttendanceShiftID { get; set; }

        // Foreign keys
        public int AttendanceID { get; set; }
        public int ShiftID { get; set; }

        public string? CheckInTime { get; set; }
        public string? Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties (optional but recommended)
        [ForeignKey("AttendanceID")]
        public Attendance Attendance { get; set; }

        [ForeignKey("ShiftID")]
        public Shift Shift { get; set; }
    }
}

