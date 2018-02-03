using System;
using System.IO;
using System.Linq;
using System.Text;
using IndexBackend;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace SlideshowCreator
{
    class SecretsInSourceCodeCheck
    {

        private void CreateEncryptedSecretAndVerifyDecryption(string secret)
        {
            var privateConfig = PrivateConfig.CreateFromPersonalJson();
            var encryptedSecret = privateConfig.CreateEncryptedValueWithPadding(secret);
            var decrypted = privateConfig.Decrypt(encryptedSecret);
            Assert.AreEqual(secret, decrypted);
            Console.WriteLine(encryptedSecret);
        }

        [Test]
        public void Check_For_Secrets_In_Source_Code()
        {
            var files = Directory.EnumerateFiles(
                "C:\\Users\\peon\\Desktop\\projects",
                "*.*",
                SearchOption.AllDirectories
            ).ToList();
            files.Remove(PrivateConfig.PersonalJson);
            files.RemoveAll(x => x.Contains(@"\.git\"));
            
            var gitIgnore = File.ReadAllLines("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\.gitignore");
            Assert.IsTrue(gitIgnore.Contains(PrivateConfig.PersonalJson.Split('\\').Last()));

            var memexGitIgnore = File.ReadAllLines("C:\\Users\\peon\\Desktop\\projects\\Memex\\.gitignore");
            Assert.IsTrue(memexGitIgnore.Contains(PrivateConfig.PersonalJson.Split('\\').Last()));

            var nestGitIgnore = File.ReadAllLines("C:\\Users\\peon\\Desktop\\projects\\Nest\\.gitignore");
            Assert.IsTrue(nestGitIgnore.Contains("Nest/aws-lambda-tools-defaults.json"));

            var secrets = File.ReadAllText(PrivateConfig.PersonalJson);
            var secretsJson = JObject.Parse(secrets);

            var privateConfig = PrivateConfig.CreateFromPersonalJson();

            int filesChecked = 0;
            foreach (string file in files)
            {
                try
                {
                    var sourceCode = File.ReadAllText(file, Encoding.UTF8);
                    foreach (var secretJson in secretsJson)
                    {
                        if (secretJson.Value.ToString().Equals("tgonzalez-nest", StringComparison.OrdinalIgnoreCase) ||
                            secretJson.Value.ToString().Equals("twilio-rekognition", StringComparison.OrdinalIgnoreCase))
                        {
                            continue; // Config files pointing into all other settings and I can't set environment variables right now.
                        }
                        CheckForSecret(sourceCode, file, secretJson.Value.ToString());
                    }

                    //CheckForSecret(sourceCode, file, privateConfig.DecryptedIp);
                    //CheckForSecret(sourceCode, file, privateConfig.NestDecryptedProductId);
                    CheckForSecret(sourceCode, file, privateConfig.NestDecryptedProductSecret);
                    CheckForSecret(sourceCode, file, privateConfig.NestDecryptedAuthUrl);
                    CheckForSecret(sourceCode, file, privateConfig.NestDecryptedAccessToken);

                    filesChecked += 1;
                }
                catch (IOException e)
                {
                    if (e.Message.Contains("\\.vs\\")) // This whole folder is gitignored.
                    {
                        Console.WriteLine("Skipped visual studio file: " + e.Message);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            Console.WriteLine($"checked {filesChecked} of {files.Count} files.");
        }

        private void CheckForSecret(string sourceCode, string sourceCodeFile, string secret)
        {
            secret = secret.ToLower();
            if (secret.Equals("snapshots", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            sourceCode = sourceCode.ToLower();
            if (sourceCode.Contains(secret))
            {
                throw new Exception($"Secret {secret} discovered in source code at: " + sourceCodeFile);
            }
        }
    }
}
