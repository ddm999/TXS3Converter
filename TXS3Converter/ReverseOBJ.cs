using System;
using System.IO;
using System.Collections.Generic;

using GTTools.Formats;
using GTTools.Formats.Entities;
using System.Drawing.Drawing2D;
using System.Linq;

namespace GTTools
{
    class ReverseOBJ
    {
        public static void FromFile(string path, string newpath)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("File does not exist");

            if (newpath == "")
            {
                Console.WriteLine("An output file must be passed when reading an OBJ.\n" +
                    "Use '-o <pacb>' where <pacb> is the original unedited pack file.");
                return;
            }

            var sr = new StreamReader(path);

            string file = sr.ReadToEnd();
            string[] lines = file.Split("\n");
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
            string newnewpath = newpath+"_edited";
            File.Delete(newnewpath);
            File.Copy(newpath, newnewpath);

            PACB.EditFile(newnewpath, toEdit, toDelete);
        }
    }
}
