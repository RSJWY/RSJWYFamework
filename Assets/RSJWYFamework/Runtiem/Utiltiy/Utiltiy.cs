using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RSJWYFamework.Runtiem
{
    public static  partial class Utiltiy
    {
        /// <summary>
        /// 16进制的字符串转为字节数组
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static byte[] ConvertHexStringToByteArray(string hexString)
        {
            // 确保字符串长度是偶数
            if (hexString.Length % 2 != 0)
            {
                throw new ArgumentException("The hex string length must be even.", nameof(hexString));
            }

            byte[] bytes = new byte[hexString.Length / 2];
            for (int i = 0; i < hexString.Length; i += 2)
            {
                // 将每两个字符转换为一个字节
                string hexByte = hexString.Substring(i, 2);
                bytes[i / 2] = Convert.ToByte(hexByte, 16);
            }
            return bytes;
        }

        /// <summary>
        /// 字符串转JSON
        /// </summary>
        /// <param name="JsonTxT"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T LoadJson<T>(string JsonTxT)
        {
            return JsonConvert.DeserializeObject<T>(JsonTxT);
        }

        /// <summary>
        /// 二维数组转一维
        /// </summary>
        /// <param name="jaggedArray"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static byte[] ConvertJaggedArrayToOneDimensional(byte[][] jaggedArray)
        {
            if (jaggedArray == null)
            {
                throw new ArgumentNullException(nameof(jaggedArray), "输入的数组不能为空。");
            }

            int totalLength = 0;
            foreach (var subArray in jaggedArray)
            {
                if (subArray == null)
                {
                    throw new ArgumentException("所有子数组都不能为null。", nameof(jaggedArray));
                }

                totalLength += subArray.Length;
            }

            byte[] result = new byte[totalLength];
            int offset = 0;
            foreach (var subArray in jaggedArray)
            {
                Array.Copy(subArray, 0, result, offset, subArray.Length);
                offset += subArray.Length;
            }

            return result;
        }

        /// <summary>
        /// uint转码为4位字节数组
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] UIntToByteArray(uint value)
        {
            byte[] byteArray = new byte[4];
            byteArray[0] = (byte)((value >> 24) & 0xFF);
            byteArray[1] = (byte)((value >> 16) & 0xFF);
            byteArray[2] = (byte)((value >> 8) & 0xFF);
            byteArray[3] = (byte)(value & 0xFF);
            return byteArray;
        }
        /// <summary>
        /// 把编码的4位uint数组转为uint
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static uint ByteArrayToUInt(byte[] byteArray)
        {
            return ((uint)byteArray[0] << 24) | ((uint)byteArray[1] << 16) | ((uint)byteArray[2] << 8) | byteArray[3];
        }
        /// <summary>
        /// 洗牌
        /// </summary>
        /// <param name="list"></param>
        /// <typeparam name="T"></typeparam>
        public static void Shuffle<T>(List<T> list)
        {
            System.Random rng = new System.Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }
    }
}