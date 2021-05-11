using System;
using System.Collections.Generic;
using System.IO;
using Syroot.BinaryData.Core;
using Syroot.BinaryData.Memory;

using GTTools.Formats.Entities;

namespace GTTools.Formats
{
    class MDL3FloatyMap
    {
        public float[] QuadFloat;
        public List<MDL3Mesh.Vertex> Verts = new();

        public static MDL3FloatyMap FromStream(ref SpanReader sr, int n = 0)
        {
            var floatyMap = new MDL3FloatyMap();

            float f1 = sr.ReadSingle();
            float f2 = sr.ReadSingle();
            float f3 = sr.ReadSingle();
            float f4 = sr.ReadSingle();

            uint floatyCount = sr.ReadUInt32();
            uint floatyOffset = sr.ReadUInt32();

            uint instOffset = sr.ReadUInt32();
            uint instCount = sr.ReadUInt32();

            sr.Position += 0xE;
            ushort unkVal = sr.ReadUInt16();

            floatyMap.QuadFloat = new float[] { f1, f2, f3, f4 };

            for (int i = 0; i < floatyCount; i++)
            {
                sr.Position = (int)(floatyOffset + i * 0x0C);
                MDL3Mesh.Vertex vert = new();
                vert.X = sr.ReadSingle();
                vert.Y = sr.ReadSingle();
                vert.Z = sr.ReadSingle();
                floatyMap.Verts.Add(vert);
            }

            return floatyMap;
        }
    }
}
