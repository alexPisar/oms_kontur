using System;
using System.Security.Cryptography;
using System.IO;

namespace Cryptography.Tools
{
    public abstract class SymmetricAlgoritm
    {
        public static byte[] Encrypt(string strText, SymmetricAlgorithm key)
        {
            MemoryStream ms = new MemoryStream();

            CryptoStream crypstream = new CryptoStream(ms, key.CreateEncryptor(key.Key, key.IV), CryptoStreamMode.Write);
            StreamWriter sw = new StreamWriter(crypstream);

            sw.WriteLine(strText);
            sw.Close();
            crypstream.Close();

            byte[] buffer = ms.ToArray();
            ms.Close();

            return buffer;
        }

        public static string Decrypt(byte[] encryptText, SymmetricAlgorithm key)
        {
            MemoryStream ms = new MemoryStream(encryptText);
            CryptoStream crypstream = new CryptoStream(ms, key.CreateDecryptor(key.Key, key.IV), CryptoStreamMode.Read);

            StreamReader sr = new StreamReader(crypstream);
            string val = sr.ReadToEnd();

            sr.Close();
            crypstream.Close();
            ms.Close();

            return val;
        }
    }
}
