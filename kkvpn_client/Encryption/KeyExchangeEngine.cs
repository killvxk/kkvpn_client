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
        private int DSAKeySize = 1024;

        private ECDiffieHellmanCng DiffieHellman;
        private DSACryptoServiceProvider DSA;
        private byte[] KeyMaterial;
        private byte[] DSASignature;
        private byte[] PublicKey;

        public KeyExchangeEngine()
        {
            DiffieHellman = new ECDiffieHellmanCng(256);
            DSA = new DSACryptoServiceProvider(DSAKeySize);
        }

        public void InitializeKey()
        {
            RandomNumberGenerator RNG = new RNGCryptoServiceProvider();

            DiffieHellman.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
            DiffieHellman.HashAlgorithm = CngAlgorithm.Sha256;

            byte[] Seed = new byte[256];
            RNG.GetBytes(Seed);
            DiffieHellman.Seed = Seed;

            PublicKey = DSA.ExportCspBlob(false);
            KeyMaterial = DiffieHellman.PublicKey.ToByteArray();
            DSASignature = DSA.SignData(KeyMaterial);
        }

        public byte[] GetKeyMaterial()
        {
            return KeyMaterial;
        }

        public byte[] GetDSASignature()
        {
            return DSASignature;
        }

        public byte[] GetDerivedKey(byte[] Material, byte[] Signature, byte[] PublicKey)
        {
            using (DSACryptoServiceProvider verifyDSA = new DSACryptoServiceProvider())
            {
                verifyDSA.ImportCspBlob(PublicKey);
                if (!verifyDSA.VerifyData(Material, Signature))
                {
                    return null;
                }
            }

            return DiffieHellman.DeriveKeyMaterial(
                ECDiffieHellmanCngPublicKey.FromByteArray(
                    Material, 
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
            DSA.FromXmlString(textReader.ReadToEnd());
            textReader.Close();
        }
    }
}
