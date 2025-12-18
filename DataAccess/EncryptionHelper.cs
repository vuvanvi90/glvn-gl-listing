using System;
using System.Security.Cryptography;
using System.Text;

namespace DataAccess
{
    public class EncryptionHelper
    {
        private static byte[] bInitVector = new byte[]
        {
            4,
            7,
            4,
            1,
            6,
            1,
            8,
            1
        };

        private static string strCryptKey = "GAMUDALAND";

        public static string EncryptStr(string strPassword)
        {
            TripleDESCryptoServiceProvider tripleDESCryptoServiceProvider = new TripleDESCryptoServiceProvider();
            MD5CryptoServiceProvider mD5CryptoServiceProvider = new MD5CryptoServiceProvider();
            string result;
            try
            {
                byte[] bytes = Encoding.ASCII.GetBytes(strPassword);
                tripleDESCryptoServiceProvider.Key = mD5CryptoServiceProvider.ComputeHash(Encoding.ASCII.GetBytes(EncryptionHelper.strCryptKey));
                tripleDESCryptoServiceProvider.IV = EncryptionHelper.bInitVector;
                string text = Convert.ToBase64String(tripleDESCryptoServiceProvider.CreateEncryptor().TransformFinalBlock(bytes, 0, bytes.Length));
                result = text;
            }
            catch
            {
                result = "";
            }
            finally
            {
                tripleDESCryptoServiceProvider.Clear();
                mD5CryptoServiceProvider.Clear();
            }
            return result;
        }

        public static string DecryptStr(string strPassword)
        {
            TripleDESCryptoServiceProvider tripleDESCryptoServiceProvider = new TripleDESCryptoServiceProvider();
            MD5CryptoServiceProvider mD5CryptoServiceProvider = new MD5CryptoServiceProvider();
            string result;
            try
            {
                byte[] array = Convert.FromBase64String(strPassword);
                tripleDESCryptoServiceProvider.Key = mD5CryptoServiceProvider.ComputeHash(Encoding.ASCII.GetBytes(EncryptionHelper.strCryptKey));
                tripleDESCryptoServiceProvider.IV = EncryptionHelper.bInitVector;
                string @string = Encoding.ASCII.GetString(tripleDESCryptoServiceProvider.CreateDecryptor().TransformFinalBlock(array, 0, array.Length));
                result = @string;
            }
            catch
            {
                result = "";
            }
            finally
            {
                tripleDESCryptoServiceProvider.Clear();
                mD5CryptoServiceProvider.Clear();
            }
            return result;
        }
    }
}
