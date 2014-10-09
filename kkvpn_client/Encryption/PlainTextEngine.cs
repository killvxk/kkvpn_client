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

        public byte[] Encrypt(byte[] Data, int key)
        {
            return Data;
        }

        public byte[] Decrypt(byte[] Data, int key)
        {
            return Data;
        }

        public int AddKeyToStore(byte[] key)
        {
            return 0;
        }

        public void DeleteKeyIfInStore(int key)
        {

        }
    }
}
