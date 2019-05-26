using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CameraImporter.Shared;

namespace CameraImporter.Utils
{
    public class EncryptionUtil
    {
        private readonly string _key;

        public EncryptionUtil(string key)
        {
            _key = key;
        }

        public string Decrypt(string value)
        {
            try
            {
                var result = Transform(Convert.FromBase64String(value), GetAlgorithm().CreateDecryptor());
                return Encoding.Unicode.GetString(result);
            }
            catch
            {
                throw new Exception(ExceptionMessage.DecryptContentFailed);
            }
        }

        private static byte[] Transform(byte[] bytes, ICryptoTransform cryptoTransform)
        {
            var ms = new MemoryStream();
            var cs = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write);
            cs.Write(bytes, 0, bytes.Length);
            cs.Close();
            return ms.ToArray();
        }

        private SymmetricAlgorithm GetAlgorithm()
        {
            var key = new Rfc2898DeriveBytes(_key, Encoding.ASCII.GetBytes(_key));
            var algorithm = new RijndaelManaged();
            algorithm.Key = key.GetBytes(algorithm.KeySize / 8);
            algorithm.IV = key.GetBytes(algorithm.BlockSize / 8);
            return algorithm;
        }
    }
}
