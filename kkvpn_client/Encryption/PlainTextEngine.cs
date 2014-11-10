using kkvpn_client.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kkvpn_client
{
    class PlainTextEngine : IEncryptionEngine
    {
        public bool Initialize()
        {
            return true;
        }

        public EncryptedData Encrypt(byte[] Data, int? key)
        {
            return new EncryptedData(Data, null, 0);
        }

        public byte[] Decrypt(EncryptedData Data, int? key)
        {
            return Data.Data;
        }

        public int AddKeyToStore(byte[] key)
        {
            return 0;
        }

        public void DeleteKeyIfInStore(int? key)
        {

        }
    }
}
