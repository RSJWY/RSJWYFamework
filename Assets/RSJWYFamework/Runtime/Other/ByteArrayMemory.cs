using System;
using System.Diagnostics.CodeAnalysis;

namespace RSJWYFamework.Runtime
{

    public class ByteArrayMemory : IDisposable
    {
        /// <summary>
        /// 默认大小
        /// </summary>
        public const int default_Size = 1024;
        /// <summary>
        /// 初始大小
        /// </summary>
        private int m_InitSize = 0;
        /// <summary>
        /// 缓冲区，存储数据的位置
        /// </summary>
        public Memory<byte> Bytes;
        /// <summary>
        /// 原始数组，防止被垃圾回收
        /// </summary>
        private byte[] _rawDataBuffer;
        
        /// <summary>
        /// 标记对象是否已被释放
        /// </summary>
        private bool _disposed = false;
        
        /// <summary>
        /// 开始读索引
        /// </summary>
        public int ReadIndex = 0;//开始读索引
        /// <summary>
        /// 已经写入的索引
        /// </summary>
        public int WriteIndex = 0;//已经写入的索引
        /// <summary>
        /// 容量
        /// </summary>
        private int Capacity = 0;

        /// <summary>
        /// 剩余空间
        /// </summary>
        public int Remain { get { return Capacity - WriteIndex; } }

        /// <summary>
        /// 允许读取的数据长度
        /// </summary>
        public int Readable { get { return WriteIndex - ReadIndex; } }

        /// <summary>
        /// 长度
        /// </summary>
        public int Length => Bytes.Length;
        
        /// <summary>
        /// 无参构造函数
        /// </summary>
        public ByteArrayMemory()
        {
            _rawDataBuffer = new byte[default_Size];
            Bytes = new Memory<byte>(_rawDataBuffer);
            Capacity = default_Size;
            m_InitSize = default_Size;
            ReadIndex = 0;
            WriteIndex = 0;
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="defaultBytes">默认数据</param>
        public ByteArrayMemory([NotNull]byte[] defaultBytes)
        {
            if (defaultBytes == null)
                throw new ArgumentNullException(nameof(defaultBytes));
            _rawDataBuffer = defaultBytes;
            Bytes = new Memory<byte>(defaultBytes);
            Capacity = defaultBytes.Length;
            m_InitSize = defaultBytes.Length;
            ReadIndex = 0;
            WriteIndex = defaultBytes.Length;
        }

        /// <summary>
        /// 检查是否需要扩容
        /// </summary>
        /// <param name="check_size">检查大小（需要的大小）</param>
        public void CheckAndMoveBytes(int check_size)
        {
            ThrowIfDisposed();
            if (Remain < check_size)
            {
                // 可读空间不足，需要移动数据
                MoveBytesToFront();
                // 移动后再次检查是否需要扩容
                if (Remain < check_size)
                {
                    //扩容到需要的
                    ReSize(check_size);
                }
            }
        }
        /// <summary>
        /// 数据前移
        /// </summary>
        public void MoveBytesToFront()
        {
            ThrowIfDisposed();
            if (ReadIndex <= 0)
            {
                return;
            }
            
            // 如果有可读数据，需要安全地移动到缓冲区前端
            if (Readable > 0)
            {
                // 使用Buffer.BlockCopy避免内存重叠问题
                // 它能正确处理源和目标区域重叠的情况
                Buffer.BlockCopy(_rawDataBuffer, ReadIndex, _rawDataBuffer, 0, Readable);
            }
            
            //写入长度等于总长度
            WriteIndex = Readable;
            ReadIndex = 0;
        }
        /// <summary>
        /// 重设尺寸
        /// </summary>
        /// <param name="size">想要的存储空间</param>
        public void ReSize(int size)
        {
            if (ReadIndex < 0 || size < Readable || size < m_InitSize)
            {
                return;
            }
            
            // 计算新的容量大小
            int newCapacity = Capacity;
            while (newCapacity < size)
            {
                // 小于4KB时翻倍增长
                if (newCapacity < 4096)
                {
                    newCapacity *= 2;
                }
                // 大于4KB时增长1.5倍
                else
                {
                    newCapacity = (int)(newCapacity * 1.5);
                }
            }
            
            // 确保最小容量
            newCapacity = Math.Max(newCapacity, default_Size);
            
            //创建新存储空间
            var newBuffer = new byte[newCapacity];
            //拷贝现有数据到新空间
            Bytes.Slice(ReadIndex, Readable).CopyTo(newBuffer);
            // 更新缓冲区引用
            _rawDataBuffer = newBuffer;
            Bytes = _rawDataBuffer.AsMemory();
            Capacity = newCapacity;
            //更新写入索引，指向当前可读数据的末尾
            WriteIndex = Readable;
            //更新读取索引，指向新的开始位置
            ReadIndex = 0;
        }
        /// <summary>
        /// 获取剩余可用切片
        /// ReadIndex-Readable
        /// </summary>
        /// <returns></returns>
        public Memory<byte> GetRemainingSlices()
        {
            ThrowIfDisposed();
            return Bytes.Slice(ReadIndex, Readable);
        }

        /// <summary>
        /// 设置数据到Memory
        /// </summary>
        /// <param name="buffer">数组</param>
        /// <param name="offset">起始偏移</param>
        /// <param name="length">读取长度</param>
        public void SetBytes([NotNull]byte[] buffer,int offset,int length)
        {
            ThrowIfDisposed();
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (offset + length > buffer.Length)
                throw new ArgumentException("The sum of offset and length is greater than the buffer length.");
            // 检查是否需要扩容
            if (Remain < length)
            {
                //翻倍扩容
                ReSize(length);
            }
            // 获取目标 Memory<byte> 的一个切片（Slice），从 WriteIndex 开始，长度为 BytesTransferred
            Memory<byte> target = Bytes.Slice(WriteIndex, length);
            // 从 buffer 获取数据源的 Span<byte>
            Span<byte> source = new Span<byte>(buffer, offset, length);
            // 使用 Span.CopyTo 方法将数据从 source 复制到 target
            source.CopyTo(target.Span);
            // 更新 WriteIndex 以反映添加的数据量
            WriteIndex += length;
        }

        /// <summary>
        /// 获取指定长度的字节数组
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public Memory<byte> GetlengthBytes(int length)
        {
            ThrowIfDisposed();
            return Bytes.Slice(ReadIndex, length);
        }

        /// <summary>
        /// 获取大于等于指定值的最小2的幂
        /// </summary>
        private static int GetNextPowerOfTwo(int value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "必须是非负数");
            
            if (value == 0)
                return 1;
                
            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            return value + 1;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的具体实现
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                    _rawDataBuffer = null;
                    Bytes = Memory<byte>.Empty;
                }
                
                // 重置状态
                Capacity = 0;
                m_InitSize = 0;
                ReadIndex = 0;
                WriteIndex = 0;
                _disposed = true;
            }
        }

        /// <summary>
        /// 检查对象是否已被释放
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ByteArrayMemory));
            }
        }
    }
}
