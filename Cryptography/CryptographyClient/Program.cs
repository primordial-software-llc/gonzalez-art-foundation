using System;
using System.IO;
using System.Linq;
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

        /// <summary>
        /// Requires a file with an ending with ".encrypted" for example: "secrets.zip.encrypted".
        /// Creates a file ending with ".decrypted-2017-12-19" for example: "secrets.zip.decrypted-2017-12-19T08-08-11Z".
        /// If the file "secrets.zip.decrypted-2017-12-19T08-08-11Z" had already existed, it would be overwritten.
        /// </summary>
        /// <returns>Returns the path to the encrypted file.</returns>
        private static string Decrypt(string path, string password)
        {
            var originalPath = path.Substring(0, path.Length - ENCRYPTED_EXT.Length);
            var originalFileName = originalPath.Split('\\').Last();
            var encryptedData = File.ReadAllBytes(path);
            var encrypted = SymmetricKeyCryptography.Decrypt(
                encrypted: Convert.ToBase64String(encryptedData),
                keyText: password,
                initializationVectorText: originalFileName);
            var today = DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ssZ");
            var safeWritePath = originalPath + $".decrypted-{today}";
            File.WriteAllBytes(safeWritePath, Convert.FromBase64String(encrypted));
            return safeWritePath;
        }

        /// <summary>
        /// Produces a file ending with ".encrypted".
        /// WARNING: The file can't not be renmaed or the file can't be decrypted. If the original filename is changed and forgotten the data is lost.
        /// </summary>
        /// <param name="path">Full path to file.</param>
        /// <param name="password">Password to encrpt file.</param>
        /// <returns>Returns the path to the encrypted file.</returns>
        private static string Encrypt(string path, string password)
        {
            var fileName = path.Split('\\').Last();
            var data = File.ReadAllBytes(path);
            var encrypted = SymmetricKeyCryptography.Encrypt(
                data: Convert.ToBase64String(data),
                keyText: password,
                initializationVectorText: fileName);
            var encryptedPath = path + ".encrypted";
            File.WriteAllBytes(encryptedPath, Convert.FromBase64String(encrypted));
            return encryptedPath;
        }
    }
}
