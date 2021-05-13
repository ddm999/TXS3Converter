using System;
using System.IO;
using System.IO.Enumeration;
using GTTools.Formats;
using GTTools.Formats.Entities;

namespace GTTools.Outputs
{
    class OBJ
    {
        public static void FromPACB(PACB pacb, string path)
        {
            uint maxTextureIndex = 0;

            for (uint i = 0; i < pacb.Models.Count; i++)
            {
                MDL3 mdl = pacb.Models[i];

                bool anyMeshSuccessful = false;
                string dirname = Path.GetDirectoryName(path);
                if (dirname != "")
                    Directory.CreateDirectory(dirname);
                string newpath = Path.ChangeExtension(path, $"mdl_{i}.obj");
                using (StreamWriter sw = new(newpath))
                {
                    int vertexOffset = 1;

                    sw.Write("# outputted by TXS3Converter\ns off\n");
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
                            //if (i == 0)
                            //    sw.Write($"usemtl mat{mesh.TextureIndex}\n");

                            sw.Write($"o obj_{n}\n");

                            if (maxTextureIndex < mesh.TextureIndex)
                                maxTextureIndex = mesh.TextureIndex;

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
                            sw.Write($"o bbox_{n}\n");
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
    }
}
