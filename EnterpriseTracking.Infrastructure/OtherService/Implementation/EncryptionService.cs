using EnterpriseTracking.Core.OtherService.Interface;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace EnterpriseTracking.Infrastructure.OtherService.Implementation
{
    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        public EncryptionService(IConfiguration config)
        {
            // You should have a 32-byte key in config or securely derived
            var keyString = config["EncryptionSettings:Key"];
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

            /*byte[] cipherText = Convert.FromBase64String(encryptedText);

            byte[] nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
            byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];
            byte[] encryptedData = new byte[cipherText.Length - nonce.Length - tag.Length];

            Buffer.BlockCopy(cipherText, 0, nonce, 0, nonce.Length);
            Buffer.BlockCopy(cipherText, nonce.Length, encryptedData, 0, encryptedData.Length);
            Buffer.BlockCopy(cipherText, nonce.Length + encryptedData.Length, tag, 0, tag.Length);

            byte[] decryptedData;

            using (AesGcm aesGcm = new AesGcm(_key))
            {
                decryptedData = new byte[encryptedData.Length];
                aesGcm.Decrypt(nonce, encryptedData, tag, decryptedData);
            }
            return Encoding.UTF8.GetString(decryptedData);*/
        }
    }
}
