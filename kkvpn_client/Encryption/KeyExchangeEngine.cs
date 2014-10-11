using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace kkvpn_client
{
    class KeyExchangeEngine
    {
        private ECDiffieHellmanCng DiffieHellman;
        private RSACryptoServiceProvider RSA;
        private byte[] KeyMaterial;
        private byte[] PublicKey;
        private bool OAEP;

        public KeyExchangeEngine(bool OAEP)
        {
            DiffieHellman = new ECDiffieHellmanCng(256);
            RSA = new RSACryptoServiceProvider(1024);

            this.OAEP = OAEP;
        }

        public void InitializeKey()
        {
            RandomNumberGenerator RNG = new RNGCryptoServiceProvider();

            DiffieHellman.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
            DiffieHellman.HashAlgorithm = CngAlgorithm.Sha256;

            byte[] Seed = new byte[256];
            RNG.GetBytes(Seed);
            DiffieHellman.Seed = Seed;

            PublicKey = RSA.ExportCspBlob(false);
            KeyMaterial = RSA.Encrypt(DiffieHellman.PublicKey.ToByteArray(), OAEP);
        }

        public byte[] GetKeyMaterial()
        {
            return KeyMaterial;
        }

        public byte[] GetDerivedKey(byte[] Material, byte[] PublicKey)
        {
            byte[] MaterialDecrypted;

            RSAParameters Params = new RSAParameters();
            Params.P = PublicKey;

            RSA.ImportParameters(Params);
            MaterialDecrypted = RSA.Decrypt(Material, OAEP);

            return DiffieHellman.DeriveKeyMaterial(
                ECDiffieHellmanCngPublicKey.FromByteArray(
                    MaterialDecrypted, 
                    CngKeyBlobFormat.EccPublicBlob
                    )
                );
        }

        public byte[] GetPublicKey()
        {
            return PublicKey;
        }

        private void ReadRSAKey(string File)
        {
            TextReader textReader = new StreamReader(File);
            RSA.FromXmlString(textReader.ReadToEnd());
            textReader.Close();
        }
    }
}
