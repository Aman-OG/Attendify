using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attendify.DATA.Models

{
    public class Shift
    {
        public int ShiftID { get; set; }   // PK

        public string Name { get; set; } = null!;
        public string StartTime { get; set; } = null!;
        public string EndTime { get; set; } = null!;
        public int GracePeriodMinutes { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<AttendancePerShift>? AttendancePerShifts { get; set; }
    }
}

