﻿using System;
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
            for (uint i = 0; i < pacb.Models.Count; i++)
            {
                bool anyMeshSuccessful = false;
                string dirname = Path.GetDirectoryName(path);
                if (dirname != "")
                    Directory.CreateDirectory(dirname);
                string newpath = Path.ChangeExtension(path, $"mdl_{i}.obj");
                using (StreamWriter sw = new(newpath))
                {
                    MDL3 mdl = pacb.Models[i];

                    int vertexOffset = 1;

                    sw.Write("# outputted by TXS3Converter\ns off\n");
                    for (int n = 0; n < mdl.Meshes.Count; n++)
                    {
                        MDL3Mesh mesh = mdl.Meshes[n];
                        bool badVertex = false;
                        bool emptyFace = false;
                        if (mesh.Verts.Length != 0 && mesh.Faces.Length != 0)
                        {
                            bool ok = true;
                            sw.Write($"o obj_{n}\n");
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
            }
        }
    }
}
