using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace RSJWYFamework.Runtiem.Utiltiy
{
    public static partial class Utility
    {
        public class RSA
        {
            /// <summary>
            /// 验证证书
            /// device_cert_verify证书验证签名函数，实现签名和验证签名
            /// /*设备证书签名*/
            /// </summary>
            /// <param name="signdata">已签名的数据</param>
            /// <param name="signature">要验证的签名数据。</param>
            /// <param name="certs_byte">证书</param>
            /// <returns></returns>
            public static bool verifyDevice(byte[] signdata, byte[] signature, byte[] certs_byte)
            {
                bool result = false;
                //byte[] certs_byte = cert;
                X509Certificate2Collection Collection = new X509Certificate2Collection();
                //导入证书
                Collection.Import(certs_byte);
                RSACryptoServiceProvider rsa;

                for (int i = 0; i < Collection.Count; i++)
                {
                    //检查确认证书开头主题
                    if (Collection[i].Subject.StartsWith("CN=DEVICEID"))
                    {
                        //将当前证书的公钥转换为RSACryptoServiceProvider类型，并赋值给rsa变量。
                        rsa = (RSACryptoServiceProvider)Collection[i].PublicKey.Key;
                        result = rsa.VerifyData(signdata, SHA1.Create(), signature);
                        break;
                    }
                }

                return result;
            }
        }
    }
}