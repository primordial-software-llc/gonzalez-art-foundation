using Cryptograpy;
using NUnit.Framework;

namespace CryptographyTests
{
    class SymmetricKeyCryptographyTests
    {
        private SymmetricKeyCryptography PrivateKeyCrypto => new SymmetricKeyCryptography();

        [TestCase("I just want to chew gum.", "this-is-a-secret", "another-secret-container.txt", "1Ye4sukV/bi/fTnybrQNWofrVsLPg4CxWjP+nJWGMH8=")]
        [TestCase("I just want to chew gum.", "this-is-a-secret", "unique-to-be-made-secret-container.txt", "LHcViZCsD1Mhb8K+RYLcZmVbKmgYbfPQmAkeKvfM0RE=")]
        public void Encrypt_Decrypt_Cycle(string text, string keyText, string textFileName, string expectedCipher)
        {
            var encrypted = PrivateKeyCrypto.Encrypt(text, keyText, textFileName);
            Assert.AreEqual(expectedCipher, encrypted);

            var decrypted = PrivateKeyCrypto.Decrypt(encrypted, keyText, textFileName);
            Assert.AreEqual(text, decrypted);
        }
    }
}
