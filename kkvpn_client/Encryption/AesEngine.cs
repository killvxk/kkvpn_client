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
            Aes.Padding = PaddingMode.ISO10126;
            MemoryStreamEncrypt = new MemoryStream();
            MemoryStreamDecrypt = new MemoryStream();
            CryptoStreamEncrypt = new Dictionary<int, CryptoStream>();
            CryptoStreamDecrypt = new Dictionary<int, CryptoStream>();

            Aes.Mode = CipherMode.CBC;

            return true;
        }

        public byte[] Encrypt(byte[] data, int? key)
        {
            if (key == null)
            {
                return null;
            }
            CryptoStream stream = CryptoStreamEncrypt[key ?? -1];

            stream.Write(data, 0, data.Length);
            stream.Flush();

            return MemoryStreamEncrypt.ToArray();
        }

        public byte[] Decrypt(byte[] data, int? key)
        {
            if (key == null)
            {
                return null;
            }
            CryptoStream stream = CryptoStreamDecrypt[key ?? -1];

            stream.Write(data, 0, data.Length);
            stream.Flush();

            return MemoryStreamDecrypt.ToArray();
        }

        public int AddKeyToStore(byte[] key)
        {
            if (key == null)
            {
                return -1;
            }

            int dictKey = key.GetHashCode();

            CryptoStreamEncrypt.Add(dictKey, new CryptoStream(MemoryStreamEncrypt, Aes.CreateEncryptor(), CryptoStreamMode.Write));
            CryptoStreamDecrypt.Add(dictKey, new CryptoStream(MemoryStreamEncrypt, Aes.CreateDecryptor(), CryptoStreamMode.Write));

            return dictKey;
        }

        public void DeleteKeyIfInStore(int? key)
        {
            if (key == null)
            {
                return;
            }

            if (CryptoStreamEncrypt.ContainsKey(key ?? -1))
            {
                CryptoStreamEncrypt.Remove(key ?? -1);
            }

            if (CryptoStreamDecrypt.ContainsKey(key ?? -1))
            {
                CryptoStreamDecrypt.Remove(key ?? -1);
            }
        }
    }
}
