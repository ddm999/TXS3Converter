using System;
using System.IO;
using System.Collections.Generic;

using GTTools.Formats;
using GTTools.Formats.Entities;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection.Emit;

namespace GTTools
{
    class ReverseOBJ
    {
        public enum ObjType
        {
            Unknown = -1,
            MDL,
            RWY
        }

        public static ObjType GetObjType(string[] lines)
        {
            foreach (string line in lines)
            {
                if (line.StartsWith("o "))
                {
                    if (line.Contains("rwy"))
                        return ObjType.RWY;
                    else if (line.Contains("mdl"))
                        return ObjType.MDL;
                    // break on first object, even if it's not an identifier
                    return ObjType.Unknown;
                }
            }
            return ObjType.Unknown;
        }

        public static void FromFile(string path, string newpath)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("File does not exist");

            if (newpath == "" || newpath == path)
            {
                Console.WriteLine("An output file must be passed when reading an OBJ.\n" +
                    "Use '-o <pacb>' where <pacb> is the original unedited pack file.");
                return;
            }

            var sr = new StreamReader(path);

            string file = sr.ReadToEnd();
            string[] lines = file.Split("\n");

            switch (GetObjType(lines))
            {
                case ObjType.MDL:
                    ToMDL(lines, newpath);
                    break;
                case ObjType.RWY:
                    ToRWY(lines, newpath);
                    break;
                case ObjType.Unknown:
                default:
                    throw new InvalidDataException("Can't find an identifier.\n" +
                                                   " Please leave the aaa_ object in the scene after editing.");
            }
        }

        static void ToMDL(string[] lines, string newpath)
        {
            int meshnum = -1;
            Dictionary<int, MDL3Mesh.Vertex[]> toEdit = new();
            List<MDL3Mesh.Vertex> verts = new();
            List<int> toDelete = new();
            foreach (string line in lines)
            {
                if (line.StartsWith("o "))
                {
                    // first record changes if we were in an edit mesh
                    if (meshnum != -1 && verts.Count > 0)
                    {
                        //Console.WriteLine($"Got edits to {meshnum}.");
                        toEdit[meshnum] = verts.ToArray();
                    }

                    // object
                    string objname = line.Split(" ")[1];

                    string[] objpart = objname.Split("_");
                    if (objpart[0] == "edit")
                    {
                        meshnum = int.Parse(objpart[1]);
                        verts = new();
                        //Console.WriteLine($"Object {meshnum}.");
                    }
                    else
                    {
                        if (objpart[0] == "del")
                        {
                            int delnum = int.Parse(objpart[1]);
                            toDelete.Add(delnum);
                            //Console.WriteLine($"Removing obj_{delnum}.");
                        }
                        meshnum = -1;
                    }
                }
                else if (line.StartsWith("v "))
                {
                    // vertex
                    if (meshnum != -1)
                    {
                        // only in edit objects
                        List<string> floats = line.Split(" ").TakeLast(3).ToList();

                        MDL3Mesh.Vertex vert = new();
                        vert.X = float.Parse(floats[0]);
                        vert.Y = float.Parse(floats[1]);
                        vert.Z = float.Parse(floats[2]);

                        //Console.WriteLine($"Vertex {vert.X} {vert.Y} {vert.Z}.");
                        verts.Add(vert);
                    }
                }
                else if (line.StartsWith("f "))
                {
                    //TODO: given not all faces are extracted...
                    //       vertex numbers can't be matched up
                }
            }

            // for last mesh edit
            if (meshnum != -1 && verts.Count > 0)
            {
                toEdit[meshnum] = verts.ToArray();
                //Console.WriteLine($"Got edits to final mesh {meshnum}.");
            }

            // xd
            string newnewpath = newpath + "_edited";
            File.Delete(newnewpath);
            File.Copy(newpath, newnewpath);

            PACB.EditFile(newnewpath, toEdit, toDelete);
        }

        public struct BigChungus {
            public RWY.Vertex BoundsStart;
            public RWY.Vertex BoundsEnd;
            public List<RWY.VertexRot> StartingGrid;
            public List<RWY.VertexRot> PitBoxes;
            public List<RWY.VertexRot> PitUnknowns;
            public List<RWY.Checkpoint> Checkpoints;
            public List<uint> CheckpointList;
            public List<RWY.Vertex> CutTrack;
            public List<RWY.RoadFace> RoadFaces;
            public List<RWY.BoundaryMesh> BoundaryMeshes;
            public List<uint> BoundaryList;
        }

        static void ToRWY(string[] lines, string newpath)
        {
            // edit bytes: 0 = leave, 1 = edit, 2 = delete
            // id 0 = bounds,
            // id 1 = grid,
            // id 2 = pit boxes,
            // id 3 = pit unknowns      // inherently linked to pitboxes, oops
            // id 4 = checkpoints,
            // id 5 = checkpoint list,  // not currently supported
            // id 6 = cut tracks,
            // id 7 = road faces,
            // id 8 = boundaries,
            // id 9 = boundary list     // not currently supported
            byte[] edits = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            sbyte currentEdit = -1; // -1 = don't care about this object

            int vertnum = 1; // current vertex in object
            RWY.RoadSurface currentSurface = RWY.RoadSurface.Unknown; // current surface value
            int objnum = 0;

            // all of the stored changes in one big chungus of a(n) ~object~ struct!
            BigChungus bigChungus;
            bigChungus.BoundsStart = new();
            bigChungus.BoundsEnd = new();
            bigChungus.BoundaryList = new();
            bigChungus.BoundaryMeshes = new();
            bigChungus.CheckpointList = new();
            bigChungus.Checkpoints = new();
            bigChungus.CutTrack = new();
            bigChungus.PitBoxes = new();
            bigChungus.PitUnknowns = new();
            bigChungus.RoadFaces = new();
            bigChungus.StartingGrid = new();

            // storage of vertices, prevents having to fuck about
            List<RWY.Vertex> vertexHelper = new();
            foreach (string line in lines)
            {
                if (line.StartsWith("o "))
                {
                    string[] split = line[2..].Split("_");
                    string edit = split[0];
                    string type = split[1];
                    if (type == "bbox")
                        currentEdit = 0;
                    else if (type == "startgrid")
                        currentEdit = 1;
                    else if (type == "pitbox")
                        currentEdit = 2;
                    else if (type == "pitunknown")
                        currentEdit = 3;
                    else if (type == "checkpoints")
                        currentEdit = 4;
                    else if (type == "cuttracks")
                        currentEdit = 6;
                    else if (type == "road")
                    {
                        currentEdit = 7;
                        currentSurface = RWY.StringToRoadSurface(split[2]);
                    }
                    else if (type == "boundary")
                    {
                        currentEdit = 8;
                        RWY.BoundaryMesh boundaryMesh; // i hate structs ree
                        boundaryMesh.verts = new();
                        boundaryMesh.unknowns = new();
                        bigChungus.BoundaryMeshes.Add(boundaryMesh);
                        objnum = bigChungus.BoundaryMeshes.Count-1;
                    }
                    else
                        currentEdit = -1;

                    if (edit == "edit")
                        edits[currentEdit] = 1;
                    else if (edit == "del")
                        edits[currentEdit] = 2;

                    vertnum = 1;
                }
                else if (line.StartsWith("v "))
                {
                    if (currentEdit == -1)
                        continue;

                    // note: doesn't really matter if we do things with "delete" objects,
                    //         they'll be ignored later

                    string[] floats = line.Split(" ")[1..];

                    RWY.Vertex vert;
                    vert.X = float.Parse(floats[0]);
                    vert.Y = float.Parse(floats[1]);
                    vert.Z = float.Parse(floats[2]);

                    RWY.VertexRot vertr;
                    vertr.X = vert.X; vertr.Y = vert.Y; vertr.Z = vert.Z;
                    vertr.R = 0;

                    if (currentEdit == 0 && vertnum == 2)
                        bigChungus.BoundsStart = vert;
                    else if (currentEdit == 0 && vertnum == 8)
                        bigChungus.BoundsEnd = vert;

                    // 1st vertex in every 4 for all arrowhead helper objects
                    //TODO: rotation!
                    else if (currentEdit == 1 && vertnum % 4 == 1)
                        bigChungus.StartingGrid.Add(vertr);
                    else if (currentEdit == 2 && vertnum % 4 == 1)
                        bigChungus.PitBoxes.Add(vertr);
                    else if (currentEdit == 3 && vertnum % 4 == 1)
                        bigChungus.PitUnknowns.Add(vertr);
                    else if (currentEdit == 4)
                    {
                        if (vertnum % 4 == 1)
                        {
                            vertexHelper.Clear();
                            vertexHelper.Add(vert);
                        }
                        else if (vertnum % 4 == 2)
                        {
                            vertexHelper.Add(vert);
                        }
                        else if (vertnum % 4 == 3)
                        {
                            RWY.CheckpointHalf left;
                            RWY.CheckpointHalf right;
                            left.vertStart = vertexHelper[0];
                            left.vertEnd = vertexHelper[1];
                            right.vertStart = vertexHelper[1];
                            right.vertEnd = vert;
                            left.pos = -1; right.pos = -1;

                            RWY.Checkpoint checkpoint;
                            checkpoint.left = left; checkpoint.right = right;
                            bigChungus.Checkpoints.Add(checkpoint);
                        }
                        // 4th point is not accurate to rwy, edited for visibility
                    }
                    else if (currentEdit == 6)
                        bigChungus.CutTrack.Add(vert);
                    else if (currentEdit == 7)
                    {
                        if (vertnum % 3 == 1)
                        {
                            vertexHelper.Clear();
                            vertexHelper.Add(vert);
                        }
                        else if (vertnum % 3 == 2)
                        {
                            vertexHelper.Add(vert);
                        }
                        else
                        {
                            RWY.RoadFace rf;
                            rf.A = vertexHelper[0];
                            rf.B = vertexHelper[1];
                            rf.C = vert;
                            rf.surface = (byte)currentSurface;
                            rf.flag2 = 0; rf.flag3 = 0; rf.flag4 = 0;
                            rf.unknown = 0;
                            // flags, unknown will be copied from current faces i guess fuck it
                            bigChungus.RoadFaces.Add(rf);
                        }
                    }
                    else if (currentEdit == 8)
                    {
                        bigChungus.BoundaryMeshes[objnum].verts.Add(vert);
                        bigChungus.BoundaryMeshes[objnum].unknowns.Add(0);
                    }

                    vertnum++;
                }
                else if(line.StartsWith("f "))
                {
                    if (currentEdit == -1)
                        continue;

                    // too hard for me 😳😔
                }
            }

            if (edits[2] != edits[3])
                Console.WriteLine("Warning: Pitboxes and Pitunknowns are inherently linked, " +
                                  "unexpected results may happen when editing/deleting one but not another.");

            // xd
            string newnewpath = newpath + "_edited";
            if (File.Exists(newnewpath))
                File.Delete(newnewpath);
            File.Copy(newpath, newnewpath);

            RWY.EditFile(newnewpath, edits, bigChungus);
        }
    }
}
