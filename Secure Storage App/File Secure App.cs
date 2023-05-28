using System;
using System.IO;
using System.Security.Cryptography;
using System.Data.SqlClient;

namespace SecureFileStorage
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Please enter the path of the file to encrypt:");
            string filePath = Console.ReadLine();

            // Check if file exists
            if (!File.Exists(filePath))
            {
                Console.WriteLine("File does not exist.");
                return;
            }

            // Check if file is malicious
            if (IsMalicious(filePath))
            {
                Console.WriteLine("File is malicious.");
                return;
            }

            // Get password to encrypt file
            Console.WriteLine("Please enter a password to encrypt the file:");
            string password = Console.ReadLine();

            // Encrypt file with AES 256
            byte[] encryptedFile = EncryptFile(filePath, password);

            // Save encrypted file to database
            string connectionString = "Server=<server>;Database=<database>;User Id=<user>;Password=<password>;";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string sql = "INSERT INTO EncryptedFiles (FileName, EncryptedFile) VALUES (@FileName, @EncryptedFile)";
                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@FileName", Path.GetFileName(filePath));
                command.Parameters.AddWithValue("@EncryptedFile", encryptedFile);
                command.ExecuteNonQuery();
                connection.Close();
            }

            Console.WriteLine("File encrypted and stored in database.");

            // Ask user if they want to decrypt the file
            Console.WriteLine("Would you like to decrypt the file? (y/n)");
            string response = Console.ReadLine();

            if (response.ToLower() == "y")
            {
                // Get password to decrypt file
                Console.WriteLine("Please enter the password to decrypt the file:");
                string decryptionPassword = Console.ReadLine();

                // Get encrypted file from database
                byte[] encryptedFileFromDB = GetEncryptedFileFromDatabase(Path.GetFileName(filePath));

                // Decrypt file with AES 256
                byte[] decryptedFile = DecryptFile(encryptedFileFromDB, decryptionPassword);

                // Save decrypted file to disk
                string decryptedFilePath = Path.Combine(Path.GetDirectoryName(filePath), "decrypted_" + Path.GetFileName(filePath));
                File.WriteAllBytes(decryptedFilePath, decryptedFile);

                Console.WriteLine("File decrypted and saved to " + decryptedFilePath);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static bool IsMalicious(string filePath)
        {
            // Check if file is malicious here
            return false;
        }

        static byte[] EncryptFile(string filePath, string password)
        {
            byte[] encryptedData;

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.GenerateIV();
                aes.GenerateKey();

                byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes(password);
                byte[] salt = new byte[8];
                new Random().NextBytes(salt);

                Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(keyBytes, salt, 10000);

                aes.Key = pbkdf2.GetBytes(32);
                aes.IV = pbkdf2.GetBytes(16);

                using (FileStream inputFileStream = new FileStream(filePath, FileMode.Open))
                using (MemoryStream outputStream = new MemoryStream())
                using (CryptoStream cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    outputStream.Write(salt, 0, salt.Length);

                    inputFileStream.CopyTo(cryptoStream);
                    cryptoStream.FlushFinalBlock();

                    encryptedData = outputStream.ToArray();
                }
            }

