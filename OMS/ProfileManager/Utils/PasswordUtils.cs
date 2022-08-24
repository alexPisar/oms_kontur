using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ProfileManager.Utils
{
    public class PasswordUtils
    {
        public string MD5Password(string password)
        {
            string salt = "ePyQqtTpr1CZy0eGrEu@$cf#$%hSHuzZW6c&62xrNr13#9LCxG";
            byte[] hashenc = new MD5CryptoServiceProvider().ComputeHash(Encoding.ASCII.GetBytes(salt + password));
            password = "";
            foreach (var b in hashenc)
                password += b.ToString("x2");
            return password.ToUpper();
        }
    }
}
