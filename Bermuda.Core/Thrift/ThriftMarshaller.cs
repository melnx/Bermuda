using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Thrift.Protocol;
using System.IO;
using Thrift.Transport;
using System.Threading;

namespace Bermuda.Core.Thrift
{
    public static class ThriftMarshaller
    {
        //public static T Deserialize<T>(byte[] payload)
        //  where T : TBase, new()
        //{
        //    using (MemoryStream stream = new MemoryStream(payload))
        //    {
        //        ThreadLocal<TProtocol> protocol = new ThreadLocal<TProtocol>( () => new TBinaryProtocol(new TStreamTransport(stream, stream)));
        //        T t = new T();
        //        t.Read(protocol.Value);
        //        return t;
        //    }
        //}

        public static T Deserialize<T>(byte[] payload)
          where T : TBase, new()
        {
            using (MemoryStream stream = new MemoryStream(payload))
            {
                TProtocol protocol = new TBinaryProtocol(new TStreamTransport(stream, stream));
                T t = new T();
                t.Read(protocol);
                return t;
            }
        }

        public static byte[] Serialize<T>(T objectToSerialize)
          where T : TBase
        {
            using(MemoryStream stream = new MemoryStream())
            {
                ThreadLocal<TProtocol> protocol = new ThreadLocal<TProtocol>(() => new TBinaryProtocol(new TStreamTransport(stream, stream)));
                objectToSerialize.Write(protocol.Value);
                return stream.ToArray();
            }
        }

        public static void SerializeToStream<T>(T objectToSerialize, Stream outStream)
          where T : TBase
        {
            using (MemoryStream stream = new MemoryStream())
            {
                ThreadLocal<TProtocol> protocol = new ThreadLocal<TProtocol>( () => new TBinaryProtocol(new TStreamTransport(stream, outStream)));
                objectToSerialize.Write(protocol.Value);
            }
        }
    }
}
