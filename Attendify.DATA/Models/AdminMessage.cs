using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;

namespace Attendify.DATA.Models
{
    public class AdminMessage
    {
        [Key]
        public int MessageID { get; set; }

        public string Type { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
