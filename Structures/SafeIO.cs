using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapMarkers.Structures
{
    public struct SafeIO
    {
        BinaryWriter Writer = null!;
        BinaryReader Reader = null!;

        long DataStartPos = 0;
        int ExpectedDataLength = 0;

        public SafeIO()
        { 
        }

        public static SafeIO SafeWrite(BinaryWriter writer)
        {
            writer.Write((int)0);
            return new SafeIO
            {
                Writer = writer,
                DataStartPos = writer.BaseStream.Position
            };
        }

        public static SafeIO SafeRead(BinaryReader reader)
        {
            int expectedDataLength = reader.ReadInt32();
            return new SafeIO
            {
                Reader = reader,
                ExpectedDataLength = expectedDataLength,
                DataStartPos = reader.BaseStream.Position
            };
        }

        public void EndWrite()
        {
            long currentPos = Writer.BaseStream.Position;
            int dataLength = (int)(currentPos - DataStartPos);

            Writer.BaseStream.Seek(DataStartPos - 4, SeekOrigin.Begin);
            Writer.Write(dataLength);
            Writer.BaseStream.Seek(currentPos, SeekOrigin.Begin);
        }

        public void EndRead(out int positionError)
        {
            int dataLength = (int)(Reader.BaseStream.Position - DataStartPos);
            positionError = dataLength - ExpectedDataLength;

            if (positionError != 0)
                Reader.BaseStream.Seek(DataStartPos + ExpectedDataLength, SeekOrigin.Begin);
        }
    }
}
