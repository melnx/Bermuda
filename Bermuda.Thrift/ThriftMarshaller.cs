using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Thrift.Protocol;
using System.IO;
using Thrift.Transport;

namespace Bermuda.Thrift
{
    public static class ThriftMarshaller
    {
        public static T deserialize<T>(byte[] payload)
          where T : TBase, new()
        {

            MemoryStream stream = new MemoryStream(payload);
            TProtocol protocol = new TBinaryProtocol(
              new TStreamTransport(stream, stream));
            T t = new T();
            t.Read(protocol);
            return t;
        }

        public static byte[] serialize<T>(T objectToSerialize)
          where T : TBase
        {

            MemoryStream stream = new MemoryStream();
            TProtocol protocol = new TBinaryProtocol(
              new TStreamTransport(stream, stream));
            objectToSerialize.Write(protocol);
            return stream.ToArray();
        }
    }
}
