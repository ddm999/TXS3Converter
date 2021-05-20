using Syroot.BinaryData.Core;
using Syroot.BinaryData.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

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
                RoadSurface.Tarmac => "TARMAC",
                RoadSurface.Guide => "GUIDE",
                RoadSurface.Green => "GREEN",
                RoadSurface.Sand => "SAND",
                RoadSurface.Gravel => "GRAVEL",
                RoadSurface.Dirt => "DIRT",
                RoadSurface.Water => "WATER",
                RoadSurface.Stone => "STONE",
                RoadSurface.Wood => "WOOD",
                RoadSurface.Pave => "PAVE",
                RoadSurface.Guide1 => "GUIDE1",
                RoadSurface.Guide2 => "GUIDE2",
                RoadSurface.Guide3 => "GUIDE3",
                RoadSurface.Pebble => "PEBBLE",
                RoadSurface.Beach => "BEACH",
                _ => $"UNKNOWN{n}",
            };
        }

        public static RoadSurface StringToRoadSurface(string n)
        {
            return n switch
            {
                "TARMAC" => RoadSurface.Tarmac,
                "GUIDE" => RoadSurface.Guide,
                "GREEN" => RoadSurface.Green,
                "SAND" => RoadSurface.Sand,
                "GRAVEL" => RoadSurface.Gravel,
                "DIRT" => RoadSurface.Dirt,
                "WATER" => RoadSurface.Water,
                "STONE" => RoadSurface.Stone,
                "WOOD" => RoadSurface.Wood,
                "PAVE" => RoadSurface.Pave,
                "GUIDE1" => RoadSurface.Guide1,
                "GUIDE2" => RoadSurface.Guide2,
                "GUIDE3" => RoadSurface.Guide3,
                "PEBBLE" => RoadSurface.Pebble,
                "BEACH" => RoadSurface.Beach,
                _ => RoadSurface.Unknown,
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
        
        static RWY LoadRWY(ref ReadOnlySpan<byte> span)
        {
            var sr = new SpanReader(span, endian: Endian.Big);

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

                if (i == 0 && rf.surface != (int)RoadSurface.Tarmac)
                    Console.WriteLine("Non-tarmac first road face: road surface types may be incorrect.");
                
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

        public static RWY FromFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("File does not exist");

            byte[] file = File.ReadAllBytes(path);
            ReadOnlySpan<byte> span = file;

            return LoadRWY(ref span);
        }

        public static void EditFile(string path, byte[] edits, ReverseOBJ.BigChungus chung)
        {
            byte[] bytes = File.ReadAllBytes(path);
            var sr = new SpanReader(bytes, endian: Endian.Big);
            var sw = new SpanWriter(bytes, endian: Endian.Big);

            if (sr.ReadStringRaw(4) != MAGIC)
                throw new InvalidDataException("Not a valid runway file.");

            uint baseOffset = sr.ReadUInt32();
            uint size = sr.ReadUInt32();

            RWYVersions Version;

            //HACKHACK: cast to enum if valid
            try
            {
                Version = (RWYVersions)sr.ReadUInt32();
            }
            catch
            {
                throw new InvalidDataException("Unsupported runway version.");
            }

            if (Version == RWYVersions.Old)
            {
                throw new InvalidDataException("Old runways (v2.0) are not currently supported.");
            }
            else if (Version == RWYVersions.New_PS3)
            {
                throw new InvalidDataException("New PS3 runways (v4.4) are not currently supported for editing.");
            }

            sr.Position += 0x4;
            
            //TODO: Calculate this from checkpoints?
            float trackLength = sr.ReadSingle();

            sw.Position = 0x20;
            if (edits[0] == 1) // edit bounds
            {
                sw.WriteSingle(chung.BoundsStart.X);
                sw.WriteSingle(chung.BoundsStart.Y);
                sw.WriteSingle(chung.BoundsStart.Z);
                sw.WriteSingle(chung.BoundsEnd.X);
                sw.WriteSingle(chung.BoundsEnd.Y);
                sw.WriteSingle(chung.BoundsEnd.Z);
            }
            else if (edits[0] == 2) // "delete" bounds, -1mil to +1mil all axes
            {
                sw.WriteSingle(-1000000.0f);
                sw.WriteSingle(-1000000.0f);
                sw.WriteSingle(-1000000.0f);
                sw.WriteSingle(1000000.0f);
                sw.WriteSingle(1000000.0f);
                sw.WriteSingle(1000000.0f);
            }

            sw.Position = 0x4C;
            sr.Position = 0x4C;
            if (edits[1] == 2) // delete grid
            {
                sw.WriteUInt32(0);
                sw.WriteUInt32(0);
            }
            uint gridCount = sr.ReadUInt32();
            uint gridOffset = sr.ReadUInt32();

            sw.Position = 0x54;
            sr.Position = 0x54;
            uint checkpointCount = sr.ReadUInt32();
            uint checkpointOffset = sr.ReadUInt32();
            uint checkpointListCount = sr.ReadUInt32();
            uint checkpointListOffset = sr.ReadUInt32();

            sw.Position = 0x74;
            sr.Position = 0x74;
            if (edits[6] == 2) // delete cuttracks
            {
                sw.WriteUInt32(0);
                sw.WriteUInt32(0);
            }
            uint cuttrackCount = sr.ReadUInt32();
            uint cuttrackOffset = sr.ReadUInt32();

            sw.Position = 0x7C;
            sr.Position = 0x7C;
            uint roadVertCount = sr.ReadUInt32();
            uint roadVertOffset = sr.ReadUInt32();
            uint roadFaceCount = sr.ReadUInt32();
            uint roadFaceOffset = sr.ReadUInt32();

            sw.Position = 0x94;
            sr.Position = 0x94;
            uint unreadableCount = sr.ReadUInt32();
            uint unreadableOffset = sr.ReadUInt32();

            sw.Position = 0x9C;
            sr.Position = 0x9C;
            if (edits[8] == 2) // delete boundaries
            {
                sw.WriteUInt32(0);
                sw.WriteUInt32(0);
                sw.WriteUInt32(0);
                sw.WriteUInt32(0);
            }
            uint boundaryVertCount = sr.ReadUInt32();
            uint boundaryVertOffset = sr.ReadUInt32();
            uint boundaryListCount = sr.ReadUInt32();
            uint boundaryListOffset = sr.ReadUInt32();

            sw.Position = 0xAC;
            sr.Position = 0xAC;
            if (edits[2] == 2) // delete pitboxes
            {
                sw.WriteUInt32(0);
                sw.WriteUInt32(0);
            }
            uint pitBoxCount = sr.ReadUInt32();
            uint pitBoxOffset = sr.ReadUInt32();

            sw.Position = 0xC0;
            sr.Position = 0xC0;
            uint pitUnknownOffset = sr.ReadUInt32();

            if (edits[1] == 1) // edit grid
            {
                sw.Position = (int)gridOffset;
                for (int i = 0; i < gridCount; i++)
                {
                    sw.WriteSingle(chung.StartingGrid[i].X);
                    sw.WriteSingle(chung.StartingGrid[i].Y);
                    sw.WriteSingle(chung.StartingGrid[i].Z);
                    //TODO: grid rotation
                    sw.Position += 0x4;
                }
            }

            if (edits[4] == 1) // edit checkpoints
            {
                sw.Position = (int)checkpointOffset;
                for (int i = 0; i < checkpointCount; i++)
                {
                    sw.WriteSingle(chung.Checkpoints[i].left.vertStart.X);
                    sw.WriteSingle(chung.Checkpoints[i].left.vertStart.Y);
                    sw.WriteSingle(chung.Checkpoints[i].left.vertStart.Z);
                    sw.WriteSingle(chung.Checkpoints[i].left.vertEnd.X);
                    sw.WriteSingle(chung.Checkpoints[i].left.vertEnd.Y);
                    sw.WriteSingle(chung.Checkpoints[i].left.vertEnd.Z);
                    //TODO: track position
                    sw.Position += 0x4;
                    sw.WriteSingle(chung.Checkpoints[i].right.vertStart.X);
                    sw.WriteSingle(chung.Checkpoints[i].right.vertStart.Y);
                    sw.WriteSingle(chung.Checkpoints[i].right.vertStart.Z);
                    sw.WriteSingle(chung.Checkpoints[i].right.vertEnd.X);
                    sw.WriteSingle(chung.Checkpoints[i].right.vertEnd.Y);
                    sw.WriteSingle(chung.Checkpoints[i].right.vertEnd.Z);
                    //TODO: track position
                    sw.Position += 0x4;
                }
            }
            else if (edits[4] == 2) // delete checkpoints
            {
                Console.WriteLine("del_checkpoints not implemented");
            }

            if (edits[5] == 1) // edit checkpoint list
            {
                //TODO: hell
                /*sw.Position = (int)checkpointListOffset;
                for (uint i = 0; i < checkpointListCount; i++)
                {
                }*/
                Console.WriteLine("edit_checkpointlist not implemented");
            }
            else if (edits[5] == 2) // "delete" (reset) checkpoint list
            {
                sw.Position = 0x5C; // checkpointListCount
                sw.WriteUInt32(checkpointCount);

                sw.Position = (int)checkpointListOffset;
                for (ushort i = 0; i < checkpointListCount; i++)
                {
                    if (i < checkpointCount)
                        sw.WriteUInt16(i);
                    else
                        sw.WriteUInt16(0);
                }
            }

            if (edits[6] == 1) // edit cuttracks
            {
                for (int i = 0; i < cuttrackCount; i++)
                {
                    sw.Position = (int)(cuttrackOffset + (i * 0x20) + 0x8);

                    sw.WriteSingle(chung.CutTrack[i].X);
                    sw.WriteSingle(chung.CutTrack[i].Y);
                    sw.WriteSingle(chung.CutTrack[i].Z);
                }
            }

            if (edits[7] == 1) // edit road
            {
                //TODO: this is fucked, honestly
                // the offset adjustment system needs to exist for this to work

                /*sw.Position = (int)roadVertOffset;
                for (uint i = 0; i < roadVertCount; i++)
                {
                    vert.X = sr.ReadSingle();
                    vert.Y = sr.ReadSingle();
                    vert.Z = sr.ReadSingle();
                }
                for (uint i = 0; i < roadFaceCount; i++)
                {
                    sr.Position = (int)(roadFaceOffset + (i * 0x10));
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
                }*/
                Console.WriteLine("edit_road not implemented");
            }
            else if (edits[7] == 2) // "delete" road
            {
                sw.Position = 0x7C; // roadVertCount
                sw.WriteUInt32(4);
                sw.Position = (int)roadVertOffset;
                // 0: -1, 0,-1
                sw.WriteSingle(-100000.0f);
                sw.WriteSingle(0.0f);
                sw.WriteSingle(-100000.0f);
                // 1: +1, 0,-1
                sw.WriteSingle(100000.0f);
                sw.WriteSingle(0.0f);
                sw.WriteSingle(-100000.0f);
                // 2: +1, 0,+1
                sw.WriteSingle(100000.0f);
                sw.WriteSingle(0.0f);
                sw.WriteSingle(100000.0f);
                // 3: -1, 0,+1
                sw.WriteSingle(-100000.0f);
                sw.WriteSingle(0.0f);
                sw.WriteSingle(100000.0f);

                sw.Position = 0x84; // roadFaceCount
                sw.WriteUInt32(2);
                sw.Position = (int)roadFaceOffset;
                // 0: 0,1,2
                sw.WriteByte(0);    // zero
                sw.WriteUInt16(0);  // vert 1
                sw.WriteByte(1);    // surface
                sw.WriteByte(0);    // zero
                sw.WriteUInt16(1);  // vert 2
                sw.WriteByte(0);    // flag
                sw.WriteByte(0);    // zero
                sw.WriteUInt16(2);  // vert 3
                sw.WriteByte(0);    // flag2
                sw.WriteUInt16(0);  // unknown
                sw.WriteByte(0);    // flag3
                sw.WriteByte(0);    // zero
                // 1: 0,2,3
                sw.WriteByte(0);    // zero
                sw.WriteUInt16(0);  // vert 1
                sw.WriteByte(1);    // surface
                sw.WriteByte(0);    // zero
                sw.WriteUInt16(2);  // vert 2
                sw.WriteByte(0);    // flag
                sw.WriteByte(0);    // zero
                sw.WriteUInt16(3);  // vert 3
                sw.WriteByte(0);    // flag2
                sw.WriteUInt16(0);  // unknown
                sw.WriteByte(0);    // flag3
                sw.WriteByte(0);    // zero
            }

            if (edits[8] == 1) // edit boundary
            {
                int n = 0;
                for (int x = 0; x < chung.BoundaryMeshes.Count; x++)
                {
                    for (int y = 0; y < chung.BoundaryMeshes[x].verts.Count; y++)
                    {
                        sw.Position = (int)(boundaryVertOffset + (n * 0x10));

                        sw.WriteSingle(chung.BoundaryMeshes[x].verts[y].X);
                        sw.WriteSingle(chung.BoundaryMeshes[x].verts[y].Y);
                        sw.WriteSingle(chung.BoundaryMeshes[x].verts[y].Z);

                        if (y == 0)
                            sw.WriteInt16((short)chung.BoundaryMeshes[x].verts.Count);
                        else if (y == chung.BoundaryMeshes[x].verts.Count - 1)
                            sw.WriteInt16((short)-chung.BoundaryMeshes[x].verts.Count);
                        else
                            sw.WriteInt16(0);

                        // unknown but I'm pretty sure nothing goes wrong if it's always 0
                        sw.WriteInt16(0);
                        n++;
                    }
                }
            }

            if (edits[5] == 1) // edit boundary list
            {
                //TODO: hell
                /*sw.Position = (int)boundaryListOffset;
                for (uint i = 0; i < boundaryListCount; i++)
                {
                }*/
                Console.WriteLine("edit_boundarylist not implemented");
            }
            else if (edits[5] == 2) // "delete" (reset) boundary list
            {
                //TODO: this is known to not work with internal boundaries
                sw.Position = 0xA4; // boundaryListCount
                sw.WriteUInt32(boundaryVertCount);

                sw.Position = (int)boundaryListOffset;
                for (ushort i = 0; i < boundaryListCount; i++)
                {
                    if (i < boundaryVertCount)
                        sw.WriteUInt16(i);
                    else
                        sw.WriteUInt16(0);
                }
            }

            if (edits[2] == 1) // edit pitboxes
            {
                sw.Position = (int)pitBoxOffset;
                for (int i = 0; i < pitBoxCount; i++)
                {
                    sw.WriteSingle(chung.PitBoxes[i].X);
                    sw.WriteSingle(chung.PitBoxes[i].Y);
                    sw.WriteSingle(chung.PitBoxes[i].Z);
                    //TODO: pitbox rotation
                    sw.Position += 0x4;
                }
            }

            if (edits[3] == 1) // edit pitunknowns
            {
                sw.Position = (int)pitUnknownOffset;
                for (int i = 0; i < pitBoxCount; i++)
                {
                    sw.WriteSingle(chung.PitUnknowns[i].X);
                    sw.WriteSingle(chung.PitUnknowns[i].Y);
                    sw.WriteSingle(chung.PitUnknowns[i].Z);
                    //TODO: pitunk rotation
                    sw.Position += 0x4;
                }
            }

            File.WriteAllBytes(path, bytes);
        }
    }
}
