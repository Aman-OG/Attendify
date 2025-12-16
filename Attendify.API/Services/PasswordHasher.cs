using BCrypt.Net;

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
            // BCrypt automatically handles salt generation and includes it in the hash
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string hashedPassword, string password)
        {
            // BCrypt.Verify handles the comparison securely
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}