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
            for (uint i = 0; i < pacb.Models.Count; i++)
            {
                bool anyMeshSuccessful = false;
                string newpath = Path.ChangeExtension(path, $"mdl_{i}.obj");
                using (StreamWriter sw = new(newpath))
                {
                    MDL3 mdl = pacb.Models[i];

                    int vertexOffset = 1;

                    sw.Write("# outputted by TXS3Converter\ns off\n");
                    for (int n = 0; n < mdl.Meshes.Count; n++)
                    {
                        MDL3Mesh mesh = mdl.Meshes[n];
                        if (mesh.Verts.Length != 0 && mesh.Faces.Length != 0)
                        {
                            sw.Write($"o obj_{n}\n");
                            for (int v = 0; v < mesh.Verts.Length; v++)
                            {
                                sw.Write($"v {mesh.Verts[v].X:f} {mesh.Verts[v].Y:f} {mesh.Verts[v].Z:f}\n");
                            }

                            bool skippedFaces = false;
                            for (int f = 0; f < mesh.Faces.Length; f++)
                            {
                                if (mesh.Faces[f].A == 0xFFFF || mesh.Faces[f].B == 0xFFFF || mesh.Faces[f].C == 0xFFFF)
                                {
                                    skippedFaces = true;
                                    continue;
                                }

                                sw.Write($"f {mesh.Faces[f].A + vertexOffset} {mesh.Faces[f].B + vertexOffset} {mesh.Faces[f].C + vertexOffset}\n");
                            }
                            vertexOffset += mesh.Verts.Length;

                            if (skippedFaces)
                            {
                                Console.WriteLine($"Model {i}, obj_{n} will have missing faces (tristrips not supported).");
                            }
                            anyMeshSuccessful = true;
                        }
                    }
                }
                if (!anyMeshSuccessful)
                {
                    Console.WriteLine($"Model {i} does not use offset data and has not been extracted (unsupported).");
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
