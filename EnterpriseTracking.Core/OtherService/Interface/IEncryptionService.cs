namespace EnterpriseTracking.Core.OtherService.Interface
{
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string encryptedText);
    }
}
