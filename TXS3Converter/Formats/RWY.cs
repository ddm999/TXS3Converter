using Syroot.BinaryData.Core;
using Syroot.BinaryData.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTTools.Formats
{
    class RWY
    {
        public struct Vertex
        {
            public float X;
            public float Y;
            public float Z;
        }

        public struct VertexRot
        {
            public float X;
            public float Y;
            public float Z;
            public float R; // rotation in radians
        }

        public struct Checkpoint
        {
            public CheckpointHalf left;
            public CheckpointHalf right;
        }

        public struct CheckpointHalf
        {
            public Vertex vertStart;
            public Vertex vertEnd;
            public float pos; // distance through track
        }

        public enum RoadSurface
        {
            Unknown = -1,
            Zero = 0,
            Tarmac,
            Guide,
            Green,
            Sand,
            Gravel,
            Dirt,
            Water,
            Stone,
            Wood,
            Pave,
            Guide1,
            Guide2,
            Guide3,
            Pebble,
            Beach,
            MAX_ROADSURFACES
        }

        public static RoadSurface Rotation(RoadSurface n)
        {
            //TODO: finish this
            return n switch
            {
                RWY.RoadSurface.Tarmac => RWY.RoadSurface.Unknown,
                RWY.RoadSurface.Guide => RWY.RoadSurface.Tarmac,
                RWY.RoadSurface.Green => RWY.RoadSurface.Guide,
                RWY.RoadSurface.Sand => RWY.RoadSurface.Unknown,
                RWY.RoadSurface.Gravel => RWY.RoadSurface.Unknown,
                RWY.RoadSurface.Dirt => RWY.RoadSurface.Unknown,
                RWY.RoadSurface.Water => RWY.RoadSurface.Tarmac,
                RWY.RoadSurface.Stone => RWY.RoadSurface.Unknown,
                RWY.RoadSurface.Wood => RWY.RoadSurface.Guide,
                RWY.RoadSurface.Pave => RWY.RoadSurface.Gravel,
                RWY.RoadSurface.Guide1 => RWY.RoadSurface.Sand,
                RWY.RoadSurface.Guide2 => RWY.RoadSurface.Unknown,
                RWY.RoadSurface.Guide3 => RWY.RoadSurface.Unknown,
                RWY.RoadSurface.Pebble => RWY.RoadSurface.Unknown,
                RWY.RoadSurface.Beach => RWY.RoadSurface.Unknown,
                _ => n,
            };
        }

        public static string RoadSurfaceToString(RoadSurface n)
        {
            return n switch {
                RWY.RoadSurface.Tarmac => "TARMAC",
                RWY.RoadSurface.Guide => "GUIDE",
                RWY.RoadSurface.Green => "GREEN",
                RWY.RoadSurface.Sand => "SAND",
                RWY.RoadSurface.Gravel => "GRAVEL",
                RWY.RoadSurface.Dirt => "DIRT",
                RWY.RoadSurface.Water => "WATER",
                RWY.RoadSurface.Stone => "STONE",
                RWY.RoadSurface.Wood => "WOOD",
                RWY.RoadSurface.Pave => "PAVE",
                RWY.RoadSurface.Guide1 => "GUIDE1",
                RWY.RoadSurface.Guide2 => "GUIDE2",
                RWY.RoadSurface.Guide3 => "GUIDE3",
                RWY.RoadSurface.Pebble => "PEBBLE",
                RWY.RoadSurface.Beach => "BEACH",
                _ => $"UNKNOWN{n}",
            };
        }

        public struct RoadFace
        {
            public Vertex A;
            public Vertex B;
            public Vertex C;
            public byte surface;
            public byte flag2;
            public byte flag3;
            public byte flag4;
            public ushort unknown;
        }

        public struct BoundaryMesh
        {
            public List<Vertex> verts;
            public List<ushort> unknowns;
        }

        public enum RWYVersions
        {
            Old = 0x00020000,
            New_PS2 = 0x00040003,
            New_PS3 = 0x00040004
        }

        public const string MAGIC = "5WNR";

        public RWYVersions Version;
        public float TrackLength;

        public Vertex BoundsStart; // size 2, assumed to be bounds?
        public Vertex BoundsEnd;
        public List<VertexRot> StartingGrid;
        public List<VertexRot> PitBoxes;
        public List<VertexRot> PitUnknowns;
        public List<Checkpoint> Checkpoints;
        public List<uint> CheckpointList; // ??? wtf is this
        public List<Vertex> CutTrack;
        public List<RoadFace> RoadFaces;
        public List<BoundaryMesh> BoundaryMeshes;
        public List<uint> BoundaryList; // ??? same as TrackSectionList

        public static RWY FromFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("File does not exist");

            byte[] file = File.ReadAllBytes(path);

            var sr = new SpanReader(file, endian: Endian.Big);

            if (sr.ReadStringRaw(4) != MAGIC)
                throw new InvalidDataException("Not a valid runway file.");

            RWY rwy = new();

            uint baseOffset = sr.ReadUInt32();
            uint size = sr.ReadUInt32();

            //HACKHACK: cast to enum if valid
            try {
                rwy.Version = (RWYVersions)sr.ReadUInt32();
            } catch {
                throw new InvalidDataException("Unsupported runway version.");
            }

            if (rwy.Version == RWYVersions.Old)
            {
                throw new InvalidDataException("Old runways (v2.0) are not currently supported.");
            } else if (rwy.Version == RWYVersions.New_PS3)
            {
                Console.WriteLine("Warning: New PS3 runways (v4.4) will have incorrect road surface types.");
            }

            sr.Position += 0x4;

            rwy.TrackLength = sr.ReadSingle();

            sr.Position += 0x8;

            Vertex boundsStart;
            boundsStart.X = sr.ReadSingle();
            boundsStart.Y = sr.ReadSingle();
            boundsStart.Z = sr.ReadSingle();
            rwy.BoundsStart = boundsStart;

            Vertex boundsEnd;
            boundsEnd.X = sr.ReadSingle();
            boundsEnd.Y = sr.ReadSingle();
            boundsEnd.Z = sr.ReadSingle();
            rwy.BoundsEnd = boundsEnd;

            sr.Position += 0x14;

            uint gridCount = sr.ReadUInt32();
            uint gridOffset = sr.ReadUInt32();
            uint checkpointCount = sr.ReadUInt32();
            uint checkpointOffset = sr.ReadUInt32();
            uint checkpointListCount = sr.ReadUInt32();
            uint checkpointListOffset = sr.ReadUInt32();
            sr.Position += 0x10;
            uint unknownCount = sr.ReadUInt32();
            uint unknownOffset = sr.ReadUInt32();
            uint roadVertCount = sr.ReadUInt32();
            uint roadVertOffset = sr.ReadUInt32();
            uint roadFaceCount = sr.ReadUInt32();
            uint roadFaceOffset = sr.ReadUInt32();
            sr.Position += 0x8;
            uint unreadableCount = sr.ReadUInt32();
            uint unreadableOffset = sr.ReadUInt32();
            uint boundaryVertCount = sr.ReadUInt32();
            uint boundaryVertOffset = sr.ReadUInt32();
            uint boundaryListCount = sr.ReadUInt32();
            uint boundaryListOffset = sr.ReadUInt32();
            uint pitBoxCount = sr.ReadUInt32();
            uint pitBoxOffset = sr.ReadUInt32();
            sr.Position += 0xC;
            uint pitUnknownOffset = sr.ReadUInt32();

            rwy.StartingGrid = new();
            sr.Position = (int)gridOffset;
            for (uint i=0; i<gridCount; i++)
            {
                VertexRot vertr;
                vertr.X = sr.ReadSingle();
                vertr.Y = sr.ReadSingle();
                vertr.Z = sr.ReadSingle();
                vertr.R = sr.ReadSingle();

                rwy.StartingGrid.Add(vertr);
            }

            rwy.Checkpoints = new();
            sr.Position = (int)checkpointOffset;
            for (uint i=0; i<checkpointCount; i++)
            {
                CheckpointHalf left;
                Vertex lvs;
                lvs.X = sr.ReadSingle();
                lvs.Y = sr.ReadSingle();
                lvs.Z = sr.ReadSingle();
                left.vertStart = lvs;
                Vertex lve;
                lve.X = sr.ReadSingle();
                lve.Y = sr.ReadSingle();
                lve.Z = sr.ReadSingle();
                left.vertEnd = lve;
                left.pos = sr.ReadSingle();

                CheckpointHalf right;
                Vertex rvs;
                rvs.X = sr.ReadSingle();
                rvs.Y = sr.ReadSingle();
                rvs.Z = sr.ReadSingle();
                right.vertStart = rvs;
                Vertex rve;
                rve.X = sr.ReadSingle();
                rve.Y = sr.ReadSingle();
                rve.Z = sr.ReadSingle();
                right.vertEnd = rve;
                right.pos = sr.ReadSingle();

                Checkpoint trackSection;
                trackSection.left = left;
                trackSection.right = right;

                rwy.Checkpoints.Add(trackSection);
            }

            rwy.CheckpointList = new();
            sr.Position = (int)checkpointListOffset;
            for (uint i = 0; i < checkpointListCount; i++)
            {
                rwy.CheckpointList.Add(sr.ReadUInt16());
            }

            rwy.CutTrack = new();
            for (uint i = 0; i < unknownCount; i++)
            {
                sr.Position = (int)(unknownOffset + (i * 0x20) + 0x8);

                Vertex vert;
                vert.X = sr.ReadSingle();
                vert.Y = sr.ReadSingle();
                vert.Z = sr.ReadSingle();

                rwy.CutTrack.Add(vert);
            }

            List<Vertex> roadVerts = new();
            sr.Position = (int)roadVertOffset;
            for (uint i = 0; i < roadVertCount; i++)
            {
                Vertex vert;
                vert.X = sr.ReadSingle();
                vert.Y = sr.ReadSingle();
                vert.Z = sr.ReadSingle();

                roadVerts.Add(vert);
            }

            rwy.RoadFaces = new();
            for (uint i = 0; i < roadFaceCount; i++)
            {
                sr.Position = (int)(roadFaceOffset + (i * 0x10));

                RoadFace rf;
                sr.Position += 0x1;
                int a = sr.ReadUInt16();
                rf.surface = sr.ReadByte();
                sr.Position += 0x1;
                int b = sr.ReadUInt16();
                rf.flag2 = sr.ReadByte();
                sr.Position += 0x1;
                int c = sr.ReadUInt16();
                rf.flag3 = sr.ReadByte();
                rf.unknown = sr.ReadUInt16();
                rf.flag4 = sr.ReadByte();

                rf.A = roadVerts[a];
                rf.B = roadVerts[b];
                rf.C = roadVerts[c];

                rwy.RoadFaces.Add(rf);
            }

            rwy.BoundaryMeshes = new();
            BoundaryMesh boundaryMesh = new();
            boundaryMesh.verts = new();
            boundaryMesh.unknowns = new();
            int n = 1;
            short lastVertCount = 0x0;
            for (uint i = 0; i < boundaryVertCount; i++)
            {
                sr.Position = (int)(boundaryVertOffset + (i * 0x10));

                Vertex vert;
                vert.X = sr.ReadSingle();
                vert.Y = sr.ReadSingle();
                vert.Z = sr.ReadSingle();
                boundaryMesh.verts.Add(vert);

                short vertCount = sr.ReadInt16();

                boundaryMesh.unknowns.Add(sr.ReadUInt16());

                if (vertCount < 0)
                {
                    if (boundaryMesh.verts.Count != -1 * vertCount || lastVertCount != -1 * vertCount)
                        Console.WriteLine($"RWY: Boundary submesh {n} sizing is erroneous. It may be incorrect.\n" +
                                          $"     Expected {-1 * vertCount}, actual {boundaryMesh.verts.Count}, initial {lastVertCount}.");

                    rwy.BoundaryMeshes.Add(boundaryMesh);
                    n++;
                    boundaryMesh = new();
                    boundaryMesh.verts = new();
                    boundaryMesh.unknowns = new();
                }
                else if (boundaryMesh.verts.Count == 1) // ie. contains this vert
                {
                    lastVertCount = vertCount;
                }
            }
            if (boundaryMesh.verts.Count != 0)
            {
                Console.WriteLine($"RWY: Boundary submesh {n} sizing is erroneous. It may be incorrect.\n" +
                                  $"     Expected end, actual {boundaryMesh.verts.Count}, initial {lastVertCount}.");
                rwy.BoundaryMeshes.Add(boundaryMesh);
            }

            rwy.BoundaryList = new();
            sr.Position = (int)boundaryListOffset;
            for (uint i = 0; i < boundaryListCount; i++)
            {
                rwy.BoundaryList.Add(sr.ReadUInt16());
            }

            rwy.PitBoxes = new();
            sr.Position = (int)pitBoxOffset;
            for (uint i = 0; i < pitBoxCount; i++)
            {
                VertexRot vertr;
                vertr.X = sr.ReadSingle();
                vertr.Y = sr.ReadSingle();
                vertr.Z = sr.ReadSingle();
                vertr.R = sr.ReadSingle();

                rwy.PitBoxes.Add(vertr);
            }

            rwy.PitUnknowns = new();
            if (pitUnknownOffset != 0) // don't get these from the header dumbass
            {
                sr.Position = (int)pitUnknownOffset;
                try
                {
                    for (uint i = 0; i < pitBoxCount; i++) //TODO: is this always pitBoxCount - 1??
                    {
                        VertexRot vertr;
                        vertr.X = sr.ReadSingle();
                        vertr.Y = sr.ReadSingle();
                        vertr.Z = sr.ReadSingle();
                        vertr.R = sr.ReadSingle();

                        rwy.PitUnknowns.Add(vertr);
                    }
                }
                catch
                {
                    Console.WriteLine($"RWY debug: Pit unknowns count is {rwy.PitUnknowns.Count}");
                }
            }

            return rwy;
        }

        public static void EditFile(string path)
        {

        }
    }
}
