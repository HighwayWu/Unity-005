using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace UnityNetwork
{
    public class NetPacket
    {
        public const int INT32_LEN = 4;
        public const int headerLength = 4;
        public const int max_body_length = 512;

        // 总长度
        public int Length
        {
            get
            {
                return headerLength + bodyLength;
            }
        }

        // 数据长度
        public int bodyLength { get; set; }

        public byte[] bytes { get; set; }

        // 发送数据包的socket
        public Socket socket;

        // 从网络上读取到的数据长度
        public int readLength { get; set; }

        public NetPacket()
        {
            readLength = 0;
            bodyLength = 0;
            bytes = new byte[headerLength + max_body_length];
        }

        // 重置参数
        public void Reset()
        {
            readLength = 0;
            bodyLength = 0;
        }

        // 序列化对象
        public byte[] Serialize<T>(T t)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                try
                {
                    // 创建序列化类
                    BinaryFormatter bf = new BinaryFormatter();

                    // 序列化到stream中
                    bf.Serialize(stream, t);
                    stream.Seek(0, SeekOrigin.Begin);
                    return stream.ToArray();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return null;
                }
            }
        }

        // 反序列化
        public T Deserialize<T>(byte[] bs)
        {
            using (MemoryStream stream = new MemoryStream(bs))
            {
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    T t = (T)bf.Deserialize(stream);
                    return t;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Deserialize: " + e.Message);
                    return default(T);
                }
            }
        }

        // 开始写入数据, 最先调用此函数
        public void BeginWrite(string msg)
        {
            bodyLength = 0;
            WriteString(msg);
        }

        public void WriteInt(int number)
        {
            if (bodyLength + INT32_LEN > max_body_length)
                return;
            byte[] bs = System.BitConverter.GetBytes(number);
            bs.CopyTo(bytes, headerLength + bodyLength);
            bodyLength += INT32_LEN;
        }

        public void WriteString(string str)
        {
            int len = System.Text.Encoding.UTF8.GetByteCount(str);
            this.WriteInt(len);
            if (bodyLength + len > max_body_length)
                return;

            System.Text.Encoding.UTF8.GetBytes(str, 0, str.Length, bytes, headerLength + bodyLength);
            bodyLength += len;
        }

        // 写入byte数组
        public void WriteStream(byte[] bs)
        {
            WriteInt(bs.Length);
            if (bodyLength + bs.Length > max_body_length)
                return;
            bs.CopyTo(bytes, headerLength + bodyLength);
            bodyLength += bs.Length;
        }

        // 写入一个序列化的对象
        public void WriteObject<T>(T t)
        {
            byte[] bs = Serialize<T>(t);
            WriteStream(bs);
        }

        // 将数据长度转化为4个字节存到byte数组最前面
        public void EncodeHeader()
        {
            byte[] bs = System.BitConverter.GetBytes(bodyLength);

            bs.CopyTo(bytes, 0);
        }

        // 开始读取
        public void BeginRead(out string msg)
        {
            bodyLength = 0;
            ReadString(out msg);
        }

        public void ReadInt(out int number)
        {
            number = 0;
            if (bodyLength + INT32_LEN > max_body_length)
                return;
            number = System.BitConverter.ToInt32(bytes, headerLength + bodyLength);
            bodyLength += INT32_LEN;
        }

        public void ReadString(out string str)
        {
            str = "";
            int len = 0;
            ReadInt(out len);
            if (bodyLength + len > max_body_length)
                return;
            str = Encoding.UTF8.GetString(bytes, headerLength + bodyLength, (int)len);
            bodyLength += len;
        }

        // 读取byte数组
        public byte[] ReadStream()
        {
            int size = 0;
            ReadInt(out size);
            if (bodyLength + INT32_LEN > max_body_length)
                return null;
            byte[] bs = new byte[size];
            for (int i = 0; i < size; i++)
            {
                bs[i] = bytes[headerLength + bodyLength + i];
            }
            bodyLength += size;
            return bs;
        }


        // 反序列化byte数组
        public T ReadObject<T>()
        {
            byte[] bs = ReadStream();
            if (bs == null)
                return default(T);
            return Deserialize<T>(bs);
        }

        // 从前四个字节中解析出数据长度
        public void DecodeHeader()
        {
            bodyLength = System.BitConverter.ToInt32(bytes, 0);
        }
    }
}