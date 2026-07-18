using EnterpriseTrackingAuthPipelineService.Interface;
using System.Security.Cryptography;
using System.Text;

namespace EnterpriseTrackingAuthPipelineService.Service
{
    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        public EncryptionService()
        {
            // You should have a 32-byte key in config or securely derived
            var keyString = "3z6vZ7Jw2c9Zz+qZz3Jr5U4qgC0l6Z2J5w5fF7zK9hA=";
            _key = Convert.FromBase64String(keyString);

        }

        public string Encrypt(string plainText)
        {
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
            RandomNumberGenerator.Fill(nonce);

            byte[] cipherText;
            byte[] tag;

            using (AesGcm aesGcm = new AesGcm(_key))
            {
                byte[] encryptedData = new byte[plainTextBytes.Length];
                byte[] tagBuffer = new byte[AesGcm.TagByteSizes.MaxSize];

                aesGcm.Encrypt(nonce, plainTextBytes, encryptedData, tagBuffer);

                cipherText = new byte[nonce.Length + encryptedData.Length + tagBuffer.Length];
                Buffer.BlockCopy(nonce, 0, cipherText, 0, nonce.Length);
                Buffer.BlockCopy(encryptedData, 0, cipherText, nonce.Length, encryptedData.Length);
                Buffer.BlockCopy(tagBuffer, 0, cipherText, nonce.Length + encryptedData.Length, tagBuffer.Length);
            }

            return Convert.ToBase64String(cipherText);
        }

        public string Decrypt(string encryptedText)
        {
            try
            {
                byte[] cipherText = Convert.FromBase64String(encryptedText);

                int nonceSize = AesGcm.NonceByteSizes.MaxSize;
                int tagSize = AesGcm.TagByteSizes.MaxSize;

                if (cipherText.Length < nonceSize + tagSize)
                    throw new ArgumentException("Cipher text too short.");

                int encryptedDataLength = cipherText.Length - nonceSize - tagSize;

                byte[] nonce = new byte[nonceSize];
                byte[] tag = new byte[tagSize];
                byte[] encryptedData = new byte[encryptedDataLength];

                Buffer.BlockCopy(cipherText, 0, nonce, 0, nonceSize);
                Buffer.BlockCopy(cipherText, nonceSize, encryptedData, 0, encryptedDataLength);
                Buffer.BlockCopy(cipherText, nonceSize + encryptedDataLength, tag, 0, tagSize);

                byte[] decryptedData = new byte[encryptedData.Length];

                using (AesGcm aesGcm = new AesGcm(_key))
                {
                    aesGcm.Decrypt(nonce, encryptedData, tag, decryptedData);
                }

                return Encoding.UTF8.GetString(decryptedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Decrypt Error]: {ex.Message}");
                throw;
            }

           
        }
    }
}
