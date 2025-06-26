using Assets.RSJWYFamework.Runtiem.Logger;
using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Assets.RSJWYFamework.Runtiem.Utiltiy
{
    public partial class Utiltiy
    {

        /// <summary>
        /// AES加密
        /// </summary>
        public static class AESTool
        {
            private static string AESHead = "AESEncrypt";

            /// <summary>
            /// 加密初始化向量
            /// </summary>
            private const string btIV = "USDRPueWLTspLozzHXksCg==";

            /// <summary>
            /// 加密盐值
            /// </summary>
            private const string salt = "w4PLa847ZtM3oLXuYZhf+g==";


            /// <summary>
            /// 文件加密，传入文件路径
            /// </summary>
            public static void AESFileEncrypt(string path, string EncryptKey)
            {
                if (!File.Exists(path))
                    return;

                try
                {
                    using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        if (fs != null)
                        {
                            byte[] headBuff = new byte[10];
                            fs.Read(headBuff, 0, 10);
                            string headTag = Encoding.UTF8.GetString(headBuff);
                            if (headTag == AESHead)
                            {
#if UNITY_EDITOR
                                APPLogger.Error($"{path}已经加密过了！");
#endif
                                return;
                            }

                            fs.Seek(0, SeekOrigin.Begin);
                            byte[] buffer = new byte[fs.Length];
                            fs.Read(buffer, 0, Convert.ToInt32(fs.Length));
                            fs.Seek(0, SeekOrigin.Begin);
                            fs.SetLength(0);
                            byte[] headBuffer = Encoding.UTF8.GetBytes(AESHead);
                            fs.Write(headBuffer, 0, 10);
                            byte[] EncBuffer = AESEncrypt(buffer, EncryptKey);
                            fs.Write(EncBuffer, 0, EncBuffer.Length);
                        }
                    }
                }
                catch (Exception e)
                {
                    //Console.ForegroundColor = ConsoleColor.Red;
                    //RSJWYLogger.LogError(RSJWYFameworkEnum.Utility, $"无法加密文件：\n{e}");
                    throw new APPException($"无法加密文件：{path}", e);
                }
            }

            /// <summary>
            /// 文件解密，传入文件路径  (改动了加密文件，不合适运行时)
            /// </summary>
            public static void AESFileDecrypt(string path, string EncryptKey)
            {
                if (!File.Exists(path))
                    return;

                try
                {
                    using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        if (fs != null)
                        {
                            byte[] headBuff = new byte[10];
                            fs.Read(headBuff, 0, headBuff.Length);
                            string headTag = Encoding.UTF8.GetString(headBuff);
                            if (headTag == AESHead)
                            {
                                byte[] buffer = new byte[fs.Length - headBuff.Length];
                                fs.Read(buffer, 0, Convert.ToInt32(fs.Length - headBuff.Length));
                                fs.Seek(0, SeekOrigin.Begin);
                                fs.SetLength(0);
                                byte[] EncBuffer = AESDecrypt(buffer, EncryptKey);
                                fs.Write(EncBuffer, 0, EncBuffer.Length);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new APPException($"无法解密文件：{path}", e);
                }
            }

            /// <summary>
            /// 文件解密，传入文件路径,返回字节
            /// </summary>
            public static byte[] AESFileByteDecrypt(string path, string EncryptKey)
            {
                if (!File.Exists(path))
                    return null;

                byte[] EncBuffer = null;
                try
                {
                    using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        if (fs != null)
                        {
                            byte[] headBuff = new byte[10];
                            fs.Read(headBuff, 0, headBuff.Length);
                            string headTag = Encoding.UTF8.GetString(headBuff);
                            if (headTag == AESHead)
                            {
                                byte[] buffer = new byte[fs.Length - headBuff.Length];
                                fs.Read(buffer, 0, Convert.ToInt32(fs.Length - headBuff.Length));
                                EncBuffer = AESDecrypt(buffer, EncryptKey);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new APPException($"无法加密数据", e);
                }

                return EncBuffer;
            }

            /// <summary>
            /// AES 加密(高级加密标准，是下一代的加密算法标准，速度快，安全级别高，目前 AES 标准的一个实现是 Rijndael 算法)
            /// </summary>
            /// <param name="EncryptString">待加密密文</param>
            /// <param name="EncryptKey">加密密钥</param>
            public static string AESEncrypt(string EncryptString, string EncryptKey)
            {
                return Convert.ToBase64String(AESEncrypt(Encoding.Default.GetBytes(EncryptString), EncryptKey));
            }

            /// <summary>
            /// AES 加密(高级加密标准，是下一代的加密算法标准，速度快，安全级别高，目前 AES 标准的一个实现是 Rijndael 算法)
            /// </summary>
            /// <param name="EncryptString">待加密密文</param>
            /// <param name="EncryptKey">加密密钥</param>
            public static byte[] AESEncrypt(byte[] EncryptByte, string EncryptKey)
            {
                if (EncryptByte == null || EncryptByte.Length == 0)
                {
                    throw new ArgumentException("要加密的数据不得为空", nameof(EncryptByte));
                }

                if (string.IsNullOrEmpty(EncryptKey))
                {
                    throw new ArgumentException("秘钥不得为空", nameof(EncryptKey));
                }

                byte[] m_btIV = Convert.FromBase64String(btIV);
                byte[] m_salt = Convert.FromBase64String(salt);
                using (Rijndael m_AESProvider = Rijndael.Create())
                using (MemoryStream m_stream = new MemoryStream())
                {
                    try
                    {
                        PasswordDeriveBytes pdb = new PasswordDeriveBytes(EncryptKey, m_salt);
                        using (ICryptoTransform transform = m_AESProvider.CreateEncryptor(pdb.GetBytes(32), m_btIV))
                        using (CryptoStream m_csstream = new CryptoStream(m_stream, transform, CryptoStreamMode.Write))
                        {
                            m_csstream.Write(EncryptByte, 0, EncryptByte.Length);
                            m_csstream.FlushFinalBlock();
                        }

                        return m_stream.ToArray();
                    }
                    catch (Exception ex) when (ex is IOException || ex is CryptographicException ||
                                               ex is ArgumentException)
                    {
                        throw new APPException(ex);
                    }
                }
            }


            /// <summary>
            /// AES 解密(高级加密标准，是下一代的加密算法标准，速度快，安全级别高，目前 AES 标准的一个实现是 Rijndael 算法)
            /// </summary>
            /// <param name="DecryptString">待解密密文</param>
            /// <param name="DecryptKey">解密密钥</param>
            public static string AESDecrypt(string DecryptString, string DecryptKey)
            {
                return Convert.ToBase64String(AESDecrypt(Encoding.Default.GetBytes(DecryptString), DecryptKey));
            }

            /// <summary>
            /// AES 解密(高级加密标准，是下一代的加密算法标准，速度快，安全级别高，目前 AES 标准的一个实现是 Rijndael 算法)
            /// </summary>
            /// <param name="DecryptByte">待解密字节流</param>
            /// <param name="DecryptKey">解密密钥</param>
            public static byte[] AESDecrypt(byte[] DecryptByte, string DecryptKey)
            {
                if (DecryptByte == null || DecryptByte.Length == 0)
                {
                    throw new ArgumentException("要解密的数据不得为空", nameof(DecryptByte));
                }

                if (string.IsNullOrEmpty(DecryptKey))
                {
                    throw new ArgumentException("秘钥不得为空", nameof(DecryptKey));
                }

                byte[] m_strDecrypt;
                byte[] m_btIV = Convert.FromBase64String(btIV);
                byte[] m_salt = Convert.FromBase64String(salt);

                using (Rijndael m_AESProvider = Rijndael.Create())
                using (MemoryStream m_stream = new MemoryStream())
                {
                    try
                    {
                        PasswordDeriveBytes pdb = new PasswordDeriveBytes(DecryptKey, m_salt);
                        using (ICryptoTransform transform = m_AESProvider.CreateDecryptor(pdb.GetBytes(32), m_btIV))
                        using (CryptoStream m_csstream = new CryptoStream(m_stream, transform, CryptoStreamMode.Write))
                        {
                            m_csstream.Write(DecryptByte, 0, DecryptByte.Length);
                            m_csstream.FlushFinalBlock();
                            m_strDecrypt = m_stream.ToArray();
                        }
                    }
                    catch (CryptographicException ex)
                    {
                        throw new APPException ("解密过程中出现加密异常", ex);
                    }
                    catch (IOException ex)
                    {
                        throw new APPException("解密过程中出现输入输出异常", ex);
                    }
                    catch (Exception ex)
                    {
                        throw new APPException("解密过程中发生未知错误", ex);
                    }
                }

                return m_strDecrypt;
            }

        }
    }
}