using kkvpn_client.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kkvpn_client
{
    interface IEncryptionEngine
    {
        bool Initialize();
        EncryptedData Encrypt(byte[] data, int? key);
        byte[] Decrypt(EncryptedData data, int? key);
        int AddKeyToStore(byte[] key);
        void DeleteKeyIfInStore(int? key);
    }
}
