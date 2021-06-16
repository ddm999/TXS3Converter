using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Syroot.BinaryData.Memory;
using System.Linq;

namespace GTTools.Formats.Entities
{
    [DebuggerDisplay("Index: {MeshIndex}, {MeshParams}")]
    public class MDL3MeshInfo
    {
        public uint MeshIndex { get; private set; }
        public string[] MeshParams { get; private set; }
        public string MeshName { get; private set; }

        public static MDL3MeshInfo FromStream(ref SpanReader sr)
        {
            MDL3MeshInfo meshInfo = new();
            uint strOffset = sr.ReadUInt32();
            meshInfo.MeshIndex = sr.ReadUInt32();

            int curPos = sr.Position;
            sr.Position = (int)strOffset;

            string meshParamString = sr.ReadString0();
            // first will be empty so skip it
            meshInfo.MeshParams = meshParamString.Split("|");
            //string shapeshape = meshInfo.MeshParams[^1];
            meshInfo.MeshName = meshInfo.MeshParams[^2];

            sr.Position = curPos;
            return meshInfo;
        }
    }
}
