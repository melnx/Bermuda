using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace Bermuda
{
    class Serializer
    {
        public static TestStruct FromBinaryReaderBlock<TestStruct>(BinaryReader br) where TestStruct : struct
        {
            //Read byte array

            byte[] buff = br.ReadBytes(Marshal.SizeOf(typeof(TestStruct)));
            //Make sure that the Garbage Collector doesn't move our buffer 

            GCHandle handle = GCHandle.Alloc(buff, GCHandleType.Pinned);
            //Marshal the bytes

            TestStruct s =
              (TestStruct)Marshal.PtrToStructure(handle.AddrOfPinnedObject(),
              typeof(TestStruct));
            handle.Free();//Give control of the buffer back to the GC 

            return s;
        }

        public static TestStruct FromFileStream<TestStruct>(FileStream fs) where TestStruct : struct
        {
            //Create Buffer

            byte[] buff = new byte[Marshal.SizeOf(typeof(TestStruct))];
            int amt = 0;
            //Loop until we've read enough bytes (usually once) 

            while (amt < buff.Length)
                amt += fs.Read(buff, amt, buff.Length - amt); //Read bytes 

            //Make sure that the Garbage Collector doesn't move our buffer 

            GCHandle handle = GCHandle.Alloc(buff, GCHandleType.Pinned);
            //Marshal the bytes

            TestStruct s =
              (TestStruct)Marshal.PtrToStructure(handle.AddrOfPinnedObject(),
              typeof(TestStruct));
            handle.Free();//Give control of the buffer back to the GC 

            return s;
        }

        public static byte[] ToByteArray<TestStruct>(TestStruct obj) where TestStruct : struct
        {
            byte[] buff = new byte[Marshal.SizeOf(typeof(TestStruct))];//Create Buffer

            GCHandle handle = GCHandle.Alloc(buff, GCHandleType.Pinned);//Hands off GC

            //Marshal the structure

            Marshal.StructureToPtr(obj, handle.AddrOfPinnedObject(), false);
            handle.Free();
            return buff;
        }

    }
}
