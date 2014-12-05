//using kkvpn_client.Communication;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Security.Cryptography;
//using System.Text;
//using System.Threading.Tasks;

//namespace kkvpn_client
//{
//    class AesEngine : IEncryptionEngine
//    {
//        private const int AesKeySize = 256;
//        private const int AesBlockSize = 128;

//        private Dictionary<int, byte[]> Keys;
//        private AesManaged Aes;

//        public void Initialize()
//        {
//            Aes = new AesManaged();
//            Aes.KeySize = AesKeySize;
//            Aes.BlockSize = AesBlockSize;
//            Aes.Padding = PaddingMode.PKCS7;
//            Aes.Mode = CipherMode.CBC;

//            Keys = new Dictionary<int, byte[]>();
//        }

//        public byte[] Encrypt(byte[] data, int? key)
//        {
//            if (key == null)
//            {
//                return null;
//            }

//            using (MemoryStream memoryStreamEncrypt = new MemoryStream())
//            {
//                Aes.GenerateIV();
//                byte[] keyBytes = Keys[key ?? -1];
//                using (CryptoStream stream = new CryptoStream(memoryStreamEncrypt, Aes.CreateEncryptor(keyBytes, Aes.IV), CryptoStreamMode.Write))
//                {
//                    stream.Write(data, 0, data.Length);
//                }

//                return ConcatanateIvAndMessage(Aes.IV, memoryStreamEncrypt.ToArray());
//            }
//        }

//        public byte[] Decrypt(byte[] data, int? key)
//        {
//            if (key == null)
//            {
//                return null;
//            }

//            byte[][] div = DivideIntoIvAndMessage(data);

//            using (MemoryStream memoryStreamData = new MemoryStream(div[1]))
//            {
//                byte[] keyBytes = Keys[key ?? -1];
//                byte[] decryptedData = new byte[div[1].Length];

//                using (CryptoStream stream = new CryptoStream(memoryStreamData, Aes.CreateDecryptor(keyBytes, div[0]), CryptoStreamMode.Read))
//                {
//                    stream.Read(decryptedData, 0, data.Length);
//                }

//                return decryptedData;
//            }
//        }

//        public int AddKeyToStore(byte[] key)
//        {
//            if (key == null)
//            {
//                return -1;
//            }

//            int dictKey = key.GetHashCode();
//            Keys.Add(dictKey, key);

//            return dictKey;
//        }

//        public void DeleteKeyIfInStore(int? key)
//        {
//            if (key == null)
//            {
//                return;
//            }

//            if (Keys.ContainsKey(key ?? -1))
//            {
//                Keys.Remove(key ?? -1);
//            }
//        }

//        private byte[] ConcatanateIvAndMessage(byte[] IV, byte[] message)
//        {
//            byte[] result = new byte[IV.Length + message.Length];

//            Array.Copy(IV, 0, result, 0, IV.Length);
//            Array.Copy(message, 0, result, IV.Length, message.Length);

//            return result;
//        }

//        private byte[][] DivideIntoIvAndMessage(byte[] data)
//        {
//            byte[][] result = new byte[2][];

//            result[0] = new byte[AesBlockSize >> 3];
//            result[1] = new byte[data.Length - (AesBlockSize >> 3)];

//            Array.Copy(data, 0, result[0], 0, result[0].Length);
//            Array.Copy(data, result[0].Length, result[1], 0, result[1].Length);

//            return result;
//        }
//    }
//}
