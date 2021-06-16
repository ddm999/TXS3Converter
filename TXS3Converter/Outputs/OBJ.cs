﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using GTTools.Formats;
using GTTools.Formats.Entities;
using TXS3Converter.Formats;

namespace GTTools.Outputs
{
    class OBJ
    {
        static string BrickHelper(float Y)
        {
            // creates a dummy object: offset vertices by 136 for this
            float scale = 0.016f;
            string str = "";

            // box
            for (int n=0;n<2;n++)
                str += $"v {20*scale} {Y+(n*10*scale)} {10*scale}\nv {20*scale} {Y+(n*10*scale)} {-10*scale}\n" +
                       $"v {-20*scale} {Y+(n*10*scale)} {10*scale}\nv {-20*scale} {Y+(n*10*scale)} {-10*scale}\n";
            str += "f 1 2 4 3\nf 5 6 8 7\nf 1 2 6 5\nf 3 4 8 7\nf 1 3 7 5\nf 2 4 8 6\n";

            // stud vertices
            for (int z=-1;z<2;z+=2)
                for (float x=-15;x<21;x+=10)
                    for (int y=12;y>9;y-=2)
                        str += $"v {(x+2)*scale} {Y+(y*scale)} {z*3*scale}\nv {(x)*scale} {Y+(y*scale)} {z*2*scale}\n" +
                               $"v {(x-2)*scale} {Y+(y*scale)} {z*3*scale}\nv {(x-3)*scale} {Y+(y*scale)} {z*5*scale}\n" +
                               $"v {(x-2)*scale} {Y+(y*scale)} {z*7*scale}\nv {(x)*scale} {Y+(y*scale)} {z*8*scale}\n" +
                               $"v {(x+2)*scale} {Y+(y*scale)} {z*7*scale}\nv {(x+3)*scale} {Y+(y*scale)} {z*5*scale}\n";

            // stud faces
            for (int n=1;n<9;n++)
            {
                str += $"f {(n*16)-7} {(n*16)-6} {(n*16)-5} {(n*16)-4} {(n*16)-3} {(n*16)-2} {(n*16)-1} {(n*16)}\n" +
                       $"f {(n*16)} {(n*16)-7} {(n*16)+1} {(n*16)+8}\n";
                for (int f=0;f<7;f++)
                    str += $"f {(n*16)+f-7} {(n*16)+f-6} {(n*16)+f+2} {(n*16)+f+1}\n";
            }

            return str;
        }

        static float[][] ArrowheadHelper(float X, float Y, float Z, float R)
        {
            // creates an arrow
            float[] orig = { X, Y, Z };
            float[] A = { -1f, 0f, -1f };
            float[] B = { 0f, 0f, 1f };
            float[] C = {  1f, 0f, -1f };

            float[] newA = { X + (A[0] * MathF.Cos(-R)) - (A[2] * MathF.Sin(-R)), Y,
                             Z + (A[0] * MathF.Sin(-R)) + (A[2] * MathF.Cos(-R))};
            float[] newB = { X + (B[0] * MathF.Cos(-R)) - (B[2] * MathF.Sin(-R)), Y,
                             Z + (B[0] * MathF.Sin(-R)) + (B[2] * MathF.Cos(-R))};
            float[] newC = { X + (C[0] * MathF.Cos(-R)) - (C[2] * MathF.Sin(-R)), Y,
                             Z + (C[0] * MathF.Sin(-R)) + (C[2] * MathF.Cos(-R))};

            float[][] ret = { orig, newA, newB, newC };
            return ret;
        }

        public static void FromMDL(MDL3 mdl, string path, uint i)
        {
            bool anyMeshSuccessful = false;
            string dirname = Path.GetDirectoryName(path);
            if (dirname != "")
                Directory.CreateDirectory(dirname);
            string newpath = Path.ChangeExtension(path, $"mdl_{i}.obj");
            using (StreamWriter sw = new(newpath))
            {
                sw.Write("# outputted by TXS3Converter\ns off\no aaa_mdl3\n" + BrickHelper(-100));
                int vertexOffset = 137;

                //if (i == 0)
                //    sw.Write($"mtllib {Path.GetFileName(path)}.mtl\n");
                for (int n = 0; n < mdl.Meshes.Count; n++)
                {
                    MDL3Mesh mesh = mdl.Meshes[n];
                    bool badVertex = false;
                    bool emptyFace = false;
                    if (mesh.Verts.Length != 0 && mesh.Faces.Length != 0)
                    {
                        bool ok = true;
                        if (i == 0)
                            sw.Write($"#usemtl mat{mesh.TextureIndex}\n");

                        sw.Write($"o obj_{n}_{mdl.MeshInfo[(uint)n].MeshName}\n");

                        for (int v = 0; v < mesh.Verts.Length; v++)
                        {
                            if (float.IsNaN(mesh.Verts[v].X) || float.IsNaN(mesh.Verts[v].Y) || float.IsNaN(mesh.Verts[v].Z) ||
                                mesh.Verts[v].X > 1e9 || mesh.Verts[v].Y > 1e9 || mesh.Verts[v].Z > 1e9 ||
                                mesh.Verts[v].X < -1e9 || mesh.Verts[v].Y < -1e9 || mesh.Verts[v].Z < -1e9)
                            {
                                sw.Write($"v 0.0 0.0 0.0\n");
                                badVertex = true;
                                ok = false;
                            }
                            else
                            {
                                sw.Write($"v {mesh.Verts[v].X:f} {mesh.Verts[v].Y:f} {mesh.Verts[v].Z:f}\n");
                            }
                        }

                        for (int f = 0; f < mesh.Faces.Length; f++)
                        {
                            if (mesh.Faces[f].A == 0 && mesh.Faces[f].B == 0 && mesh.Faces[f].C == 0)
                            {
                                emptyFace = true;
                                ok = false;
                            }
                            else
                            {
                                sw.Write($"f {mesh.Faces[f].A + vertexOffset} {mesh.Faces[f].B + vertexOffset} {mesh.Faces[f].C + vertexOffset}\n");
                            }
                        }
                        vertexOffset += mesh.Verts.Length;

                        if (ok)
                            anyMeshSuccessful = true;
                    }

                    if (badVertex)
                        Console.WriteLine("Model's vertex data could not be parsed entirely:\n some vertices may be incorrect.");
                    if (emptyFace)
                        Console.WriteLine("Model's face data could not be parsed entirely:\n some faces may be missing.");
                }
            }
            if (!anyMeshSuccessful)
            {
                Console.WriteLine($"Model {i} could not be extracted (unsupported data).");
                File.Delete(newpath);
            }
            else
            {
                Console.WriteLine($"Successfully created {Path.GetFileName(newpath)}");
            }

            anyMeshSuccessful = false;
            newpath = Path.ChangeExtension(path, $"mdl_{i}_bbox.obj");
            using (StreamWriter sw = new(newpath))
            {
                int vertexOffset = 1;
                sw.Write("# outputted by TXS3Converter\ns off\n");
                for (int n = 0; n < mdl.Meshes.Count; n++)
                {
                    MDL3Mesh mesh = mdl.Meshes[n];
                    bool badVertex = false;
                    if (mesh.BBox != null && mesh.BBox.Length == 8)
                    {
                        bool ok = true;
                        sw.Write($"o bbox_{n}_{mdl.MeshInfo[(uint)n].MeshName}\n");
                        Console.WriteLine($"Mesh {n} has name {mdl.MeshInfo[(uint)n].MeshName}");
                        for (int v = 0; v < mesh.BBox.Length; v++)
                        {
                            if (float.IsNaN(mesh.BBox[v].X) || float.IsNaN(mesh.BBox[v].Y) || float.IsNaN(mesh.BBox[v].Z) ||
                                mesh.BBox[v].X > 1e9 || mesh.BBox[v].Y > 1e9 || mesh.BBox[v].Z > 1e9 ||
                                mesh.BBox[v].X < -1e9 || mesh.BBox[v].Y < -1e9 || mesh.BBox[v].Z < -1e9)
                            {
                                sw.Write($"v 0.0 0.0 0.0\n");
                                badVertex = true;
                                ok = false;
                            }
                            else
                            {
                                sw.Write($"v {mesh.BBox[v].X:f} {mesh.BBox[v].Y:f} {mesh.BBox[v].Z:f}\n");
                            }
                        }
                        sw.Write($"f {vertexOffset + 0} {vertexOffset + 1} {vertexOffset + 2} {vertexOffset + 3} \n" +
                                 $"f {vertexOffset + 4} {vertexOffset + 5} {vertexOffset + 6} {vertexOffset + 7} \n" +
                                 $"f {vertexOffset + 0} {vertexOffset + 3} {vertexOffset + 7} {vertexOffset + 4} \n" +
                                 $"f {vertexOffset + 1} {vertexOffset + 2} {vertexOffset + 6} {vertexOffset + 5} \n" +
                                 $"f {vertexOffset + 0} {vertexOffset + 1} {vertexOffset + 5} {vertexOffset + 4} \n" +
                                 $"f {vertexOffset + 2} {vertexOffset + 3} {vertexOffset + 7} {vertexOffset + 6} \n");
                        vertexOffset += 8;

                        if (ok)
                            anyMeshSuccessful = true;
                    }

                    if (badVertex)
                        Console.WriteLine("Model's bbox data could not be parsed entirely:\n some bboxes may be incorrect.");
                }
            }
            if (!anyMeshSuccessful)
            {
                Console.WriteLine($"Model {i}'s bbox could not be extracted (unsupported data).");
                File.Delete(newpath);
            }
            else
            {
                Console.WriteLine($"Successfully created {Path.GetFileName(newpath)}");
            }

            anyMeshSuccessful = false;
            newpath = Path.ChangeExtension(path, $"mdl_{i}_floaty.obj");
            using (StreamWriter sw = new(newpath))
            {
                int vertexOffset = 1;
                sw.Write("# outputted by TXS3Converter\ns off\n");
                for (int n = 0; n < mdl.FloatyMaps.Count; n++)
                {
                    MDL3FloatyMap fm = mdl.FloatyMaps[n];
                    bool badVertex = false;
                    if (fm.Verts.Count == 8)
                    {
                        bool ok = true;
                        sw.Write($"o floaty_{n}\n");
                        for (int v = 0; v < fm.Verts.Count; v++)
                        {
                            if (float.IsNaN(fm.Verts[v].X) || float.IsNaN(fm.Verts[v].Y) || float.IsNaN(fm.Verts[v].Z) ||
                                fm.Verts[v].X > 1e9 || fm.Verts[v].Y > 1e9 || fm.Verts[v].Z > 1e9 ||
                                fm.Verts[v].X < -1e9 || fm.Verts[v].Y < -1e9 || fm.Verts[v].Z < -1e9)
                            {
                                sw.Write($"v 0.0 0.0 0.0\n");
                                badVertex = true;
                                ok = false;
                            }
                            else
                            {
                                sw.Write($"v {fm.Verts[v].X:f} {fm.Verts[v].Y:f} {fm.Verts[v].Z:f}\n");
                            }
                        }
                        sw.Write($"f {vertexOffset + 0} {vertexOffset + 1} {vertexOffset + 2} {vertexOffset + 3} \n" +
                                 $"f {vertexOffset + 4} {vertexOffset + 5} {vertexOffset + 6} {vertexOffset + 7} \n" +
                                 $"f {vertexOffset + 0} {vertexOffset + 3} {vertexOffset + 7} {vertexOffset + 4} \n" +
                                 $"f {vertexOffset + 1} {vertexOffset + 2} {vertexOffset + 6} {vertexOffset + 5} \n" +
                                 $"f {vertexOffset + 0} {vertexOffset + 1} {vertexOffset + 5} {vertexOffset + 4} \n" +
                                 $"f {vertexOffset + 2} {vertexOffset + 3} {vertexOffset + 7} {vertexOffset + 6} \n");
                        vertexOffset += 8;

                        if (ok)
                            anyMeshSuccessful = true;
                    }

                    if (badVertex)
                        Console.WriteLine("Model's floaty data could not be parsed entirely:\n some floaty may be incorrect.");
                }
            }
            if (!anyMeshSuccessful)
            {
                //Console.WriteLine($"Model {i}'s floaty could not be extracted (unsupported data).");
                File.Delete(newpath);
            }
            else
            {
                Console.WriteLine($"Successfully created {Path.GetFileName(newpath)}");
            }
        }

        public static void FromPACB(PACB pacb, string path)
        {
            for (uint i = 0; i < pacb.Models.Count; i++)
            {
                MDL3 mdl = pacb.Models[i];

                FromMDL(mdl, path, i);
            }

            //TODO: find the magical mystery material table
            /*using (StreamWriter sw = new(path + ".mtl"))
            {
                sw.Write($"# outputted by TXS3Converter\n");

                string folder = Path.GetFileName(path) + "_tex";
                int lastTxsNum = pacb.Textures.Count - 1;
                for (int n = 0; n < pacb.Textures[lastTxsNum].Count; n++)
                {
                    sw.Write($"newmtl mat{n}\n Kd 1.0 1.0 1.0\n Ks 0.0 0.0 0.0\n d 1.0\n" +
                             $" map_Kd {folder}/{n}.png\n");
                }
                if (pacb.Textures.Count < maxTextureIndex+1)
                {
                    for (int n = pacb.Textures[lastTxsNum].Count; n < maxTextureIndex+1; n++)
                    {
                        sw.Write($"newmtl mat{n}\nKd 1.0 0.0 0.0\n Ks 0.0 0.0 0.0\n d 1.0\n");
                    }
                }
            }*/
        }

        public static void FromRWY(RWY rwy, string path)
        {
            string dirname = Path.GetDirectoryName(path);
            if (dirname != "")
                Directory.CreateDirectory(dirname);
            string newpath = Path.ChangeExtension(path, $"rwy.obj");
            using (StreamWriter sw = new(newpath))
            {
                int vertexOffset = 137;

                sw.Write("# outputted by TXS3Converter\ns off\nmtllib rwy.mtl\nusemtl unknown\no aaa_rwy\n" + BrickHelper(-100) +
                         $"o rwy_bbox\n" +
                         $"v {rwy.BoundsStart.X} {rwy.BoundsStart.Y} {rwy.BoundsEnd.Z}\n" +
                         $"v {rwy.BoundsStart.X} {rwy.BoundsStart.Y} {rwy.BoundsStart.Z}\n" +
                         $"v {rwy.BoundsStart.X} {rwy.BoundsEnd.Y} {rwy.BoundsStart.Z}\n" +
                         $"v {rwy.BoundsStart.X} {rwy.BoundsEnd.Y} {rwy.BoundsEnd.Z}\n" +
                         $"v {rwy.BoundsEnd.X} {rwy.BoundsStart.Y} {rwy.BoundsEnd.Z}\n" +
                         $"v {rwy.BoundsEnd.X} {rwy.BoundsStart.Y} {rwy.BoundsStart.Z}\n" +
                         $"v {rwy.BoundsEnd.X} {rwy.BoundsEnd.Y} {rwy.BoundsStart.Z}\n" +
                         $"v {rwy.BoundsEnd.X} {rwy.BoundsEnd.Y} {rwy.BoundsEnd.Z}\n");

                sw.Write($"f {vertexOffset + 0} {vertexOffset + 1} {vertexOffset + 2} {vertexOffset + 3} \n" +
                         $"f {vertexOffset + 4} {vertexOffset + 5} {vertexOffset + 6} {vertexOffset + 7} \n" +
                         $"f {vertexOffset + 0} {vertexOffset + 3} {vertexOffset + 7} {vertexOffset + 4} \n" +
                         $"f {vertexOffset + 1} {vertexOffset + 2} {vertexOffset + 6} {vertexOffset + 5} \n" +
                         $"f {vertexOffset + 0} {vertexOffset + 1} {vertexOffset + 5} {vertexOffset + 4} \n" +
                         $"f {vertexOffset + 2} {vertexOffset + 3} {vertexOffset + 7} {vertexOffset + 6} \n");
                vertexOffset += 8;

                sw.Write("usemtl startgrid\no rwy_startgrid\n");
                for (int i = 0; i < rwy.StartingGrid.Count; i++)
                {
                    RWY.VertexRot vertr = rwy.StartingGrid[i];
                    float[][] arrow = ArrowheadHelper(vertr.X, vertr.Y, vertr.Z, vertr.R);
                    for (int n = 0; n < 4; n++)
                    {
                        sw.Write($"v {arrow[n][0]} {arrow[n][1]} {arrow[n][2]}\n");
                    }
                    sw.Write($"f {vertexOffset} {vertexOffset + 1} {vertexOffset + 2} {vertexOffset + 3}\n");
                    vertexOffset += 4;
                }

                sw.Write("usemtl pitbox\no rwy_pitbox\n");
                for (int i = 0; i < rwy.PitBoxes.Count; i++)
                {
                    RWY.VertexRot vertr = rwy.PitBoxes[i];
                    float[][] arrow = ArrowheadHelper(vertr.X, vertr.Y, vertr.Z, vertr.R);
                    for (int n = 0; n < 4; n++)
                    {
                        sw.Write($"v {arrow[n][0]} {arrow[n][1]} {arrow[n][2]}\n");
                    }
                    sw.Write($"f {vertexOffset} {vertexOffset + 1} {vertexOffset + 2} {vertexOffset + 3}\n");
                    vertexOffset += 4;
                }

                sw.Write("usemtl pitunknown\no rwy_pitunknown\n");
                for (int i = 0; i < rwy.PitUnknowns.Count; i++)
                {
                    RWY.VertexRot vertr = rwy.PitUnknowns[i];
                    float[][] arrow = ArrowheadHelper(vertr.X, vertr.Y, vertr.Z, vertr.R);
                    for (int n = 0; n < 4; n++)
                    {
                        sw.Write($"v {arrow[n][0]} {arrow[n][1]} {arrow[n][2]}\n");
                    }
                    sw.Write($"f {vertexOffset} {vertexOffset + 1} {vertexOffset + 2} {vertexOffset + 3}\n");
                    vertexOffset += 4;
                }

                int ccnt = rwy.Checkpoints.Count;
                if (ccnt > 0)
                {
                    sw.Write("usemtl checkpoints\no rwy_checkpoints\n");

                    for (int i = 0; i < ccnt; i++)
                    {
                        RWY.CheckpointHalf left = rwy.Checkpoints[i].left;
                        sw.Write($"v {left.vertStart.X} {left.vertStart.Y} {left.vertStart.Z}\n");
                        sw.Write($"v {left.vertEnd.X} {left.vertEnd.Y} {left.vertEnd.Z}\n");
                        RWY.CheckpointHalf right = rwy.Checkpoints[i].right;
                        sw.Write($"v {right.vertStart.X + 0.1} {right.vertStart.Y + 1} {right.vertStart.Z + 0.1}\n");
                        sw.Write($"v {right.vertEnd.X} {right.vertEnd.Y} {right.vertEnd.Z}\n");
                        sw.Write($"f {vertexOffset} {vertexOffset + 1} {vertexOffset + 3} {vertexOffset + 2}\n");
                        vertexOffset += 4;
                    }

                    sw.Write($"l ");
                    for (int i = 0; i < ccnt; i++)
                    {
                        sw.Write($"{vertexOffset + (i*4) + 1 - (ccnt*4)} "); // left.vertEnd
                    }
                    sw.Write($"\n");
                }

                int ucnt = rwy.CutTrack.Count;
                if (ucnt > 0)
                {
                    // don't need material, this is line only
                    sw.Write("o rwy_cuttracks\n");
                    for (int i = 0; i < ucnt; i++)
                    {
                        sw.Write($"v {rwy.CutTrack[i].X} {rwy.CutTrack[i].Y} {rwy.CutTrack[i].Z}\n");
                    }
                    sw.Write("l ");
                    for (int i = 0; i < ucnt; i++)
                    {
                        sw.Write($"{vertexOffset} ");
                        vertexOffset++;
                    }
                    sw.Write($"\n");
                }

                List<List<RWY.RoadFace>> roadFaces = new();
                for (RWY.RoadSurface n = 0; n < RWY.RoadSurface.MAX_ROADSURFACES; n++)
                    roadFaces.Add(new());

                for (int i = 0; i < rwy.RoadFaces.Count; i++)
                    roadFaces[rwy.RoadFaces[i].surface].Add(rwy.RoadFaces[i]);

                for (int n = 0; n < (int)RWY.RoadSurface.MAX_ROADSURFACES; n++)
                {
                    if (roadFaces[n].Count != 0)
                    {
                        string str = RWY.RoadSurfaceToString((RWY.RoadSurface)n);
                        sw.Write($"usemtl road_{str}\no rwy_road_{str}\n");
                    }
                    for (int i = 0; i < roadFaces[n].Count; i++)
                    {
                        sw.Write($"v {roadFaces[n][i].A.X} {roadFaces[n][i].A.Y} {roadFaces[n][i].A.Z}\n");
                        sw.Write($"v {roadFaces[n][i].B.X} {roadFaces[n][i].B.Y} {roadFaces[n][i].B.Z}\n");
                        sw.Write($"v {roadFaces[n][i].C.X} {roadFaces[n][i].C.Y} {roadFaces[n][i].C.Z}\n");
                        sw.Write($"f {vertexOffset} {vertexOffset + 1} {vertexOffset + 2}\n");
                        vertexOffset += 3;
                    }
                }

                for (int n = 0; n < rwy.BoundaryMeshes.Count; n++)
                {
                    List<RWY.Vertex> verts = rwy.BoundaryMeshes[n].verts;
                    int vcnt = verts.Count;
                    if (vcnt > 0)
                    {
                        sw.Write($"usemtl unknown\no rwy_boundary_{n}\n");
                        for (int i = 0; i < vcnt; i++)
                        {
                            sw.Write($"v {verts[i].X} {verts[i].Y} {verts[i].Z}\n");
                        }
                        sw.Write("l ");
                        for (int i = 0; i < vcnt; i++)
                        {
                            sw.Write($"{vertexOffset} ");
                            vertexOffset++;
                        }
                        sw.Write($"{vertexOffset-vcnt}\n");
                    }
                }
            }
        }
    }
}
