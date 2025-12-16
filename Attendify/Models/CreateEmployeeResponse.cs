namespace Attendify.Models
{
    public class CreateEmployeeResponse
    {
        public int EmployeeID { get; set; }
        public string EmpCode { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = "";
        public string? Department { get; set; }
        public string? Position { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Role { get; set; }
        public bool IsActive { get; set; }
        public string GeneratedPassword { get; set; } = "";
        public string Message { get; set; } = "";
    }
}
