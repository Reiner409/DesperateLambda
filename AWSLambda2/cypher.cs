using System.IO;
using System.Text;
using System.Security.Cryptography;
using System;
using Org.BouncyCastle;


namespace EncDec
{
    class Cifratore
    {

    }
    public static class EncryptionHelper
    {
        public static string Encrypt(string password)
        {
            //Algoritmo di cifratura: SHA3
            var hashAlgorithm = new Org.BouncyCastle.Crypto.Digests.Sha3Digest(512);

            // Choose correct encoding based on your usecase
            byte[] input = Encoding.ASCII.GetBytes(password);

            hashAlgorithm.BlockUpdate(input, 0, input.Length);

            byte[] result = new byte[64]; // 512 / 8 = 64
            hashAlgorithm.DoFinal(result, 0);

            string hashString = BitConverter.ToString(result);
            hashString =  hashString.Replace("-", "").ToLowerInvariant();
            return hashString;

        }
    }
}