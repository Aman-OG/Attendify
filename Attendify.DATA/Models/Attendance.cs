using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attendify.DATA.Models

{
    public class Attendance
    {
        public int AttendanceID { get; set; }   // PK

       
        public string EmpCode { get; set; }   // FK
        public DateTime Date { get; set; }

        public string Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Employee? Employee { get; set; }
        public ICollection<AttendancePerShift>? AttendancePerShifts { get; set; }
    }
}
