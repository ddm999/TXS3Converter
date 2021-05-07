using System;
using System.Collections.Generic;
using System.Text;

using Syroot.BinaryData.Memory;

namespace GTTools.Formats.Entities
{
    /// <summary>
    /// Flexible Vertex Format
    /// https://docs.microsoft.com/en-us/windows/win32/direct3d9/d3dfvf
    /// </summary>
    public class MDL3FVF
    {
        public string str;
        public uint dataLength;

        public static MDL3FVF FromStream(ref SpanReader sr)
        {
            MDL3FVF fvf = new MDL3FVF();

            var strOffset = sr.ReadUInt32();
            var unkIndex = sr.ReadUInt32();
            var unkOffset = sr.ReadUInt32();
            sr.Position += 0x8;
            var unkOffset2 = sr.ReadUInt32();
            var unkFlag1 = sr.ReadByte();
            fvf.dataLength = sr.ReadByte();
            sr.Position += 0x5A;
            var unkOffset3 = sr.ReadUInt32();

            sr.Position = (int)strOffset;
            fvf.str = sr.ReadString0();

            return fvf;
        }
    }
}
