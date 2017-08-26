using System;
using System.IO;
using Cryptography;

namespace CryptographyClient
{
    class Program
    {
        private const string ENCRYPTED_EXT = ".encrypted";

        static void Main(string[] args)
        {
            const string EXAMPLE_PATH = "FILE_NAME_FULL_PATH";
            const string EXAMPLE_PASSWORD = "PASSWORD";
            const string EXAMPLE_PASSWORD2 = "PASSWORD AGAIN";

            if (args.Length == 0 || args.Length < 3)
            {
                Console.WriteLine("CAUTION: Encrypt only which you can suffer the loss of e.g. backups or transient communications. This software is licensed under Apache-2.0 and provides no warranty of any kind (unless required by law).");
                Console.WriteLine($"{Environment.NewLine}Usage: CryptographyClient.exe [{EXAMPLE_PATH}] [{EXAMPLE_PASSWORD}] [{EXAMPLE_PASSWORD2}]");
                Console.WriteLine($"{Environment.NewLine}Only the following arguments may be provided and they are required: {EXAMPLE_PATH}, {EXAMPLE_PASSWORD} {EXAMPLE_PASSWORD2}");
                return;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine("File not found: " + args[0]);
                return;
            }

            if (!args[1].Equals(args[2]))
            {
                Console.WriteLine("Passwords don't match.");
                return;
            }

            if (args[0].ToLower().EndsWith($"{ENCRYPTED_EXT}"))
            {
                var output = Decrypt(args[0], args[1]);
                Console.WriteLine($"New decrypted file created {output}");
            }
            else
            {
                var output = Encrypt(args[0], args[1]);
                Console.WriteLine($"New encrypted file created {output}");
            }
        }
        
        private static string Decrypt(string path, string password)
        {
            var originalPath = path.Substring(0, path.Length - ENCRYPTED_EXT.Length);
            var encryptedData = File.ReadAllText(path);
            var encrypted = new SymmetricKeyCryptography().Decrypt(encryptedData, password, originalPath);
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var safeWritePath = originalPath + $".decrypted-{today}";
            File.WriteAllText(safeWritePath, encrypted);
            return safeWritePath;
        }

        private static string Encrypt(string path, string password)
        {
            var data = File.ReadAllText(path);
            var encrypted = new SymmetricKeyCryptography().Encrypt(data, password, path);
            var encryptedPath = path + ".encrypted";
            File.WriteAllText(encryptedPath, encrypted);
            return encryptedPath;
        }
    }
}
