using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Syroot.BinaryData.Memory;

namespace GTTools.Formats.Entities
{
    [DebuggerDisplay("Type: {Type}, {Flag}, Data: {DataStart}, {DataLength}")]
    public class PACBInfo
    {
        public uint Type { get; private set; }
        public uint Flag { get; private set; }
        public int DataStart { get; private set; }
        public int DataLength { get; private set; }

        public static PACBInfo FromStream(ref SpanReader sr)
        {
            PACBInfo pacbInfo = new();

            pacbInfo.Type = sr.ReadUInt32();
            pacbInfo.Flag = sr.ReadUInt32();
            pacbInfo.DataStart = sr.ReadInt32();
            pacbInfo.DataLength = sr.ReadInt32();

            return pacbInfo;
        }

        public void AddOffset(int offset)
        {
            DataStart += offset;
        }
    }
}
