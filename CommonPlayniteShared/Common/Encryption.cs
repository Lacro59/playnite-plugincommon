using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CommonPlayniteShared.Common
{
    /// <summary>
    /// Based on https://www.codegrepper.com/code-examples/csharp/encrypt+text+file+in+C%23+with+key
    /// </summary>
    public static class Encryption
    {
        public static byte[] GenerateRandomSalt()
        {
            byte[] data = new byte[32];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                for (int i = 0; i < 10; i++)
                {
                    rng.GetBytes(data);
                }
            }

            return data;
        }

        public static void EncryptToFile(string filePath, string content, Encoding encoding, string password)
        {
            byte[] salt = GenerateRandomSalt();
            using (var outFile = new FileStream(filePath, FileMode.Create))
            {
                var passwordBytes = Encoding.UTF8.GetBytes(password);
                using (var AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;
                    AES.Padding = PaddingMode.PKCS7;
                    AES.Mode = CipherMode.CFB;
                    using (var key = new Rfc2898DeriveBytes(passwordBytes, salt, 1000))
                    {
                        AES.Key = key.GetBytes(AES.KeySize / 8);
                        AES.IV = key.GetBytes(AES.BlockSize / 8);
                    }

                    outFile.Write(salt, 0, salt.Length);
                    using (var cs = new CryptoStream(outFile, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        var byteContent = encoding.GetBytes(content);
                        cs.Write(byteContent, 0, byteContent.Length);
                        cs.FlushFinalBlock();
                        cs.Close();
                        outFile.Close();
                    }
                }
            }
        }

        public static string DecryptFromFileOld(string inputFile, Encoding encoding, string password)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var salt = new byte[32];

            // Open file with shared read/write access
            using (var fsCrypt = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fsCrypt.Read(salt, 0, salt.Length);

                using (var AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;
                    AES.Padding = PaddingMode.PKCS7;
                    AES.Mode = CipherMode.CFB;

                    using (var key = new Rfc2898DeriveBytes(passwordBytes, salt, 1000))
                    {
                        AES.Key = key.GetBytes(AES.KeySize / 8);
                        AES.IV = key.GetBytes(AES.BlockSize / 8);
                    }

                    using (var cs = new CryptoStream(fsCrypt, AES.CreateDecryptor(), CryptoStreamMode.Read))
                    using (var reader = new StreamReader(cs, encoding))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }

        /// <summary>
        /// Decrypts content from a file using AES encryption without locking the file for writing.
        /// Reads the entire file into memory first to avoid file handle conflicts.
        /// </summary>
        /// <param name="inputFile">Path to the encrypted file</param>
        /// <param name="encoding">Text encoding to use for the decrypted content</param>
        /// <param name="password">Password used for decryption</param>
        /// <returns>Decrypted string content</returns>
        /// <exception cref="FileNotFoundException">Thrown when the input file doesn't exist</exception>
        /// <exception cref="CryptographicException">Thrown when decryption fails</exception>
        public static string DecryptFromFile(string inputFile, Encoding encoding, string password)
        {
            // Read entire file content into memory to avoid file locking
            byte[] encryptedData = File.ReadAllBytes(inputFile);

            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var salt = new byte[32];

            // Extract salt from the beginning of the file
            Array.Copy(encryptedData, 0, salt, 0, salt.Length);

            using (var AES = new RijndaelManaged())
            {
                AES.KeySize = 256;
                AES.BlockSize = 128;
                AES.Padding = PaddingMode.PKCS7;
                AES.Mode = CipherMode.CFB;

                using (var key = new Rfc2898DeriveBytes(passwordBytes, salt, 1000))
                {
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);
                }

                // Create memory stream with encrypted data (excluding salt)
                using (var memoryStream = new MemoryStream(encryptedData, 32, encryptedData.Length - 32))
                using (var cs = new CryptoStream(memoryStream, AES.CreateDecryptor(), CryptoStreamMode.Read))
                using (var reader = new StreamReader(cs, encoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
