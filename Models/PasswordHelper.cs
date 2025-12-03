using System.Security.Cryptography;
using System.Text;

namespace WarehouseManagementSystem.Models
{
    public static class PasswordHelper
    {
        // 將純文字密碼轉為 SHA256 雜湊字串
        public static string HashPassword(string plainText)
        {
            if (plainText == null)
                return null;

            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(plainText);
                byte[] hash = sha.ComputeHash(bytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
