using System;
using System.IO;
using System.Security.Cryptography;

namespace FileEncryption
{
    class Program
    {
        static void Main(string[] args)
        {
            // Enter the path to the file you want to encrypt here
            string filePath = "PATH_TO_FILE";

            // Enter a password to use for encryption (make sure to keep it secure!)
            string password = "MY_PASSWORD";

            // Generate a random salt value
            byte[] salt = GenerateSalt();

            // Derive a key and IV from the password and salt using PBKDF2
            byte[] key, iv;
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, 10000))
            {
                key = deriveBytes.GetBytes(32);
                iv = deriveBytes.GetBytes(16);
            }

            // Encrypt the file using AES 256 in CBC mode with PKCS7 padding
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var inputFile = File.OpenRead(filePath))
                using (var outputFile = File.Create(filePath + ".enc"))
                using (var cryptoStream = new CryptoStream(outputFile, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    // Write the salt to the beginning of the output file
                    outputFile.Write(salt, 0, salt.Length);

                    // Encrypt the file contents and write them to the output file
                    inputFile.CopyTo(cryptoStream);
                }
            }

            Console.WriteLine("File encrypted.");

            // Decrypt the file using the same password, salt, key, and IV
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var inputFile = File.OpenRead(filePath + ".enc"))
                using (var outputFile = File.Create(filePath + ".dec"))
                using (var cryptoStream = new CryptoStream(inputFile, aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    // Read the salt from the beginning of the input file
                    byte[] saltBuffer = new byte[16];
                    inputFile.Read(saltBuffer, 0, saltBuffer.Length);

                    // Decrypt the file contents and write them to the output file
                    cryptoStream.CopyTo(outputFile);
                }
            }

            Console.WriteLine("File decrypted.");
        }

        static byte[] GenerateSalt()
        {
            var salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }
    }
}
