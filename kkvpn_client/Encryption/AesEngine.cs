using kkvpn_client.Communication;
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
        private Dictionary<int, byte[]> Keys;
        private RijndaelManaged Aes;

        public void Initialize()
        {
            Aes = new RijndaelManaged();
            Aes.KeySize = 256;
            Aes.BlockSize = 128;
            Aes.Padding = PaddingMode.ANSIX923;
            Aes.Mode = CipherMode.CBC;

            Keys = new Dictionary<int, byte[]>();
        }

        public byte[] Encrypt(byte[] data, int? key)
        {
            if (key == null)
            {
                return null;
            }

            using (MemoryStream memoryStreamEncrypt = new MemoryStream())
            {
                Aes.GenerateIV();
                byte[] keyBytes = Keys[key ?? -1];
                using (CryptoStream stream = new CryptoStream(memoryStreamEncrypt, Aes.CreateEncryptor(keyBytes, Aes.IV), CryptoStreamMode.Write))
                {
                    stream.Write(data, 0, data.Length);
                }

                return memoryStreamEncrypt.ToArray();
            }
        }

        public byte[] Decrypt(byte[] data, int? key)
        {
            if (key == null)
            {
                return null;
            }

            using (MemoryStream memoryStreamData = new MemoryStream(data))
            {
                byte[] decryptedData = new byte[data.Length];
                byte[] keyBytes = Keys[key ?? -1];
                using (CryptoStream stream = new CryptoStream(memoryStreamData, Aes.CreateDecryptor(keyBytes, null), CryptoStreamMode.Read))    //!!!!!!!!!!!!! IV!
                {
                    stream.Read(decryptedData, 0, data.Length);
                }

                return decryptedData;
            }
        }

        public int AddKeyToStore(byte[] key)
        {
            if (key == null)
            {
                return -1;
            }

            int dictKey = key.GetHashCode();
            Keys.Add(dictKey, key);

            return dictKey;
        }

        public void DeleteKeyIfInStore(int? key)
        {
            if (key == null)
            {
                return;
            }

            if (Keys.ContainsKey(key ?? -1))
            {
                Keys.Remove(key ?? -1);
            }
        }
    }
}
