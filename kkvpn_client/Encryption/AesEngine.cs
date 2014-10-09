using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace kkvpn_client
{
    class AesEngine : IEncryptionEngine
    {
        RijndaelManaged Aes;
        MemoryStream MemoryStreamEncrypt;
        MemoryStream MemoryStreamDecrypt;
        Dictionary<int, CryptoStream> CryptoStreamEncrypt;
        Dictionary<int, CryptoStream> CryptoStreamDecrypt;

        public bool Initialize()
        {
            Aes = new RijndaelManaged();

            Aes.KeySize = 256;
            Aes.BlockSize = 128;
            MemoryStreamEncrypt = new MemoryStream();
            MemoryStreamDecrypt = new MemoryStream();
            CryptoStreamEncrypt = new Dictionary<int, CryptoStream>();
            CryptoStreamDecrypt = new Dictionary<int, CryptoStream>();

            Aes.Mode = CipherMode.CBC;

            return true;
        }

        public byte[] Encrypt(byte[] data, int key)
        {
            CryptoStream stream = CryptoStreamEncrypt[key];

            stream.Flush();
            stream.Write(data, 0, data.Length);

            return MemoryStreamEncrypt.ToArray();
        }

        public byte[] Decrypt(byte[] data, int key)
        {
            CryptoStream stream = CryptoStreamDecrypt[key];

            stream.Flush();
            stream.Write(data, 0, data.Length);

            return MemoryStreamDecrypt.ToArray();
        }

        public int AddKeyToStore(byte[] key)
        {
            int dictKey = key.GetHashCode();

            CryptoStreamEncrypt.Add(dictKey, new CryptoStream(MemoryStreamEncrypt, Aes.CreateEncryptor(), CryptoStreamMode.Write));
            CryptoStreamDecrypt.Add(dictKey, new CryptoStream(MemoryStreamEncrypt, Aes.CreateDecryptor(), CryptoStreamMode.Write));

            return dictKey;
        }

        public void DeleteKeyIfInStore(int key)
        {
            if (CryptoStreamEncrypt.ContainsKey(key))
            {
                CryptoStreamEncrypt.Remove(key);
            }

            if (CryptoStreamDecrypt.ContainsKey(key))
            {
                CryptoStreamDecrypt.Remove(key);
            }
        }
    }
}
