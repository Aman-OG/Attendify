// Attendify.API/Services/PasswordHasher.cs
using System.Security.Cryptography;
using System.Text;

namespace Attendify.API.Services
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string hashedPassword, string password);
    }

    public class PasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        public bool VerifyPassword(string hashedPassword, string password)
        {
            var hashedInput = HashPassword(password);
            return hashedPassword == hashedInput;
        }
    }
}