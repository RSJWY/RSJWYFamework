using System;
using System.Linq;
using System.Text;

namespace RSJWYFamework.Runtime
{
    public static partial class Utility
    {
        /// <summary>
        /// ModBusCRC16校验值计算
        /// </summary>
        public static class ModbusCRC16
        {
            /// <summary>
            /// 计算字符串的CRC16校验值并整合为一组
            /// </summary>
            /// <param name="hexString">输入的指令值</param>
            /// <returns></returns>
            public static (string hex, byte[] hexByte) AddCRC16ToHexString(string hexString)
            {
                // 去除字符串中的空格并转换为字节数组
                //byte[] data = hexString.Split(' ').Select(s => Convert.ToByte(s, 16)).ToArray();

                byte[] data = ConvertHexStringToByteArray(hexString);
                // 计算CRC16校验码
                ushort crc = CalculateCRC16(data);

                // 将CRC16结果转换为字节并添加到原数据后
                byte[] crcBytes = BitConverter.GetBytes(crc);
                /*if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(crcBytes); // 如果系统是小端序，需要翻转字节顺序
            }*/

                // 将数据和CRC拼接起来
                byte[] fullData = data.Concat(crcBytes).ToArray();


                // 转换为带空格的16进制字符串
                StringBuilder result = new StringBuilder();
                foreach (var b in fullData)
                {
                    result.Append(b.ToString("X2") + " ");
                }

                // 返回结果字符串，去除末尾多余的空格
                return (result.ToString().TrimEnd(), fullData);
            }

            /// <summary>
            /// 转为16进制数组
            /// </summary>
            /// <param name="hexString">16进制字符串数据（可不带空格）</param>
            /// <returns>返回16进制数组</returns>
            public static byte[] ConvertHexStringToByteArray(string hexString)
            {
                // 移除字符串中的空格
                hexString = hexString.Replace(" ", "");

                // 计算字节数组的长度
                int byteCount = hexString.Length / 2;

                // 初始化字节数组
                byte[] byteArray = new byte[byteCount];

                // 逐对处理16进制字符，将其转换为字节
                for (int i = 0; i < byteCount; i++)
                {
                    byteArray[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
                }

                return byteArray;
            }

            /// <summary>
            /// 计算CRC16
            /// </summary>
            /// <param name="data">计算数组的CRC16校验值</param>
            /// <returns>得到的CRC16校验值</returns>
            public static ushort CalculateCRC16(byte[] data)
            {
                ushort crc = 0xFFFF;

                foreach (byte b in data)
                {
                    crc ^= b;

                    for (int i = 0; i < 8; i++)
                    {
                        if ((crc & 1) != 0)
                        {
                            crc >>= 1;
                            crc ^= 0xA001;
                        }
                        else
                        {
                            crc >>= 1;
                        }
                    }
                }

                return crc;
            }
        }
    }
}
