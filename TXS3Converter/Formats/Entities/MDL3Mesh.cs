using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Syroot.BinaryData.Memory;

namespace GTTools.Formats.Entities
{
    public class MDL3Mesh
    {
        public struct Vertex
        {
            public float X;
            public float Y;
            public float Z;
        }

        public struct Face
        {
            public ushort A;
            public ushort B;
            public ushort C;
        }

        public Vertex[] Verts = Array.Empty<Vertex>();
        public Face[] Faces = Array.Empty<Face>();
        public Vertex[] BBox;

        public static MDL3Mesh FromStream(ref SpanReader sr, MDL3FVF[] fvf = null, int n = 0)
        {
            var meshInfo = new MDL3Mesh();

            uint flag1 = sr.ReadByte();   //unk flag 1
            uint flag2 = sr.ReadByte();   //unk flag 2
            uint fvfIndex = sr.ReadUInt16();
            uint textureIndex = sr.ReadUInt16();         // index into the embedded texture info?
            uint null1 = sr.ReadUInt16();                // 0x08, seems to have no effect
            uint vertexCount = sr.ReadUInt32();          // UNUSED by game
            uint vertexOffset = sr.ReadUInt32();         // null for PS3 tracks, unsure how they find the data

            uint null3 = sr.ReadUInt32();                // 0x00000000?
            uint facesDataLength = sr.ReadUInt32();      // UNUSED by game
            uint facesOffset = sr.ReadUInt32();          // null for PS3 tracks, unsure how they find the data

            uint null5 = sr.ReadUInt32();                // 0x00000000?
            uint null6 = sr.ReadUInt32();                // 0x0000FFFF
            uint facesCount = sr.ReadUInt32();           // UNUSED by game
            uint boxOffset = sr.ReadUInt32();            // 8 non-FVF vertices that define a bounding 6-face for the object: used for culling
            uint unkOffset2 = sr.ReadUInt32();           // 0x00000000?

            if (vertexOffset > sr.Length || facesOffset > sr.Length || boxOffset > sr.Length)
            {
                //Console.WriteLine($"Mesh in obj_{n} cannot be loaded: uses other memory.");
                return meshInfo;
            }

            if (fvf != null)
            {
                // get the vertex data length by using index into the fvf list
                uint fvfDataLength = fvf[fvfIndex].dataLength;

                if (vertexCount > 0 && vertexOffset != 0)
                {
                    meshInfo.Verts = new Vertex[vertexCount];
                    int curPos = sr.Position;
                    for (int i = 0; i < vertexCount; i++)
                    {
                        sr.Position = (int)(vertexOffset + i*fvfDataLength);
                        meshInfo.Verts[i].X = sr.ReadSingle();
                        meshInfo.Verts[i].Y = sr.ReadSingle();
                        meshInfo.Verts[i].Z = sr.ReadSingle();
                    }
                    sr.Position = curPos;
                }
            }

            // unsure if these are possible, but no harm in correcting
            if (facesCount > 0 && facesDataLength == 0)
                facesDataLength = facesCount * 3;
            else if (facesCount == 0 && facesDataLength > 0)
                facesCount = facesDataLength / 3;

            if (facesDataLength > 0 && facesOffset != 0)
            {
                meshInfo.Faces = new Face[facesCount];
                int curPos = sr.Position;
                sr.Position = (int)facesOffset;
                bool badFace = false;
                for (int i = 0; i < facesCount; i++)
                {
                    if (sr.Position != sr.Length)
                    {
                        ushort a = sr.ReadUInt16();
                        ushort b = sr.ReadUInt16();
                        ushort c = sr.ReadUInt16();
                        if (a <= vertexCount && b <= vertexCount && c <= vertexCount)
                        {
                            meshInfo.Faces[i].A = a;
                            meshInfo.Faces[i].B = b;
                            meshInfo.Faces[i].C = c;
                        }
                        else
                            badFace = true;
                    }
                    else
                    {
                        badFace = true;
                        break;
                    }
                }

                if (badFace)
                    Console.WriteLine($"Model's obj_{n} will have missing faces (tristrips not supported).");

                sr.Position = curPos;
            }

            if (boxOffset > 0)
            {
                meshInfo.BBox = new Vertex[8];
                int curPos = sr.Position;
                sr.Position = (int)boxOffset;
                for (int i = 0; i < 8; i++)
                {
                    meshInfo.BBox[i].X = sr.ReadSingle();
                    meshInfo.BBox[i].Y = sr.ReadSingle();
                    meshInfo.BBox[i].Z = sr.ReadSingle();
                }

                sr.Position = curPos;
            }

            return meshInfo;
        }
    }
}
