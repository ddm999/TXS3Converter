using System;
using System.Linq;
using System.IO;
using System.Text;

using GTTools.Formats;
using GTTools.Outputs;

namespace GTTools
{
    class Program
    {
        public static bool isBatchConvert = false;
        private static int processedFiles = 0;
        public static string currentFileName;

        static void Main(string[] args)
        {
            Console.WriteLine("Gran Turismo TXS3 Converter");
            if (args.Length < 1)
            {
                Console.WriteLine("    Usage: <input_files>\n");
                Console.WriteLine("    When handling PACB files, you can also provide '-o <output_directory>'.");
                Console.WriteLine("    Providing this incorrectly won't error but instead cause unintended behaviour,\n     so be sure you're running the right command.");
            }
            else
            {
                bool skiparg = false;
                foreach (var arg in args)
                {
                    if (skiparg == true)
                    {
                        skiparg = false;
                        continue;
                    }

                    // named arg shouldn't be treated as files
                    if (arg == "-o")
                    {
                        // and also skip the arg itself
                        skiparg = true;
                        continue;
                    }

                    if (!File.Exists(arg) && !Directory.Exists(arg))
                    {
                        Console.WriteLine($"File does not exist: {arg}");
                        continue;
                    }

                    FileAttributes attr = File.GetAttributes(arg);
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        string[] files = Directory.GetFiles(arg, "*.*", SearchOption.TopDirectoryOnly);
                        if (files.Length > 1) isBatchConvert = true;
                        foreach (string f in files)
                        {
                            currentFileName = Path.GetFileName(f);
                            //try
                            //{
                                ProcessFile(f, args);
                                processedFiles++;
                            //}
                            //catch (Exception e)
                            //{
                            //    Console.WriteLine($@"[!] Could not convert {currentFileName} : {e.Message}");
                            //}
                        }
                    }
                    else
                    {
                        if (args.Length > 1) 
                            isBatchConvert = true;

                        currentFileName = Path.GetFileName(arg);
                        //try
                        //{
                            ProcessFile(arg, args);
                            processedFiles++;
                        //}
                        //catch (Exception e)
                        //{
                        //    Console.WriteLine($@"[!] Could not convert {currentFileName} : {e.Message}");
                        //}
                    }
                }
                Console.WriteLine($"Done, {processedFiles} files were converted.");
            }
        }

        static bool _texConvExists = false;
        static void ProcessFile(string path, string[] args)
        {
            currentFileName = Path.GetFileName(path);

            string newpath = "";

            if (args.Contains("-o"))
                newpath = args[Array.IndexOf(args, "-o") + 1];

            if (newpath != "")
                newpath += "/" + Path.GetFileName(path);
            else
                newpath = Path.GetFileName(path);

            string magic;
            if (Path.GetExtension(path) == ".obj")
                magic = "OBJ";
            else
                magic = GetFileMagic(path);

            switch (magic)
            {
                case "TXS3":
                case "3SXT":
                    ProcessTXS3Texture(path);
                    break;
                case "MDL3":
                    MDL3.TexturesFromFile(path);
                    break;
                case PACB.MAGIC:
                case PACB.MAGIC_LE:
                    PACB pacb = PACB.FromFile(path);
                    OBJ.FromPACB(pacb, newpath);
                    Directory.CreateDirectory(Path.GetFileNameWithoutExtension(path)+"_tex");
                    for (int x = 0; x < pacb.Textures.Count; x++)
                    {
                        for (int y = 0; y < pacb.Textures[x].Count; y++)
                        {
                            if (pacb.Textures[x][y].OriginalFilePath != null)
                            {
                                pacb.Textures[x][y].SaveAsPng(Path.GetFileNameWithoutExtension(path) + "_tex/" + pacb.Textures[x][y].OriginalFilePath + ".png");
                                Console.WriteLine($"Saved named texture {pacb.Textures[x][y].OriginalFilePath}");
                            }
                            else
                            {
                                pacb.Textures[x][y].SaveAsPng(Path.GetFileNameWithoutExtension(path) + "_tex/" + y + ".png");
                                //Console.WriteLine($"Saved texture {y}");
                            }
                        }
                        Console.WriteLine($"TXS num {x+1}: Saved {pacb.Textures[x].Count} total textures.");
                    }
                    break;
                case RWY.MAGIC:
                    RWY rwy = RWY.FromFile(path);
                    OBJ.FromRWY(rwy, newpath);
                    break;
                case "OBJ":
                    ReverseOBJ.FromFile(path, newpath);
                    break;
                default:
                    if (!_texConvExists && !File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "texconv.exe")))
                    {
                        Console.WriteLine("TexConv (image to DDS tool) is missing. Download it from https://github.com/microsoft/DirectXTex/releases and place it next to the tool.");
                        Environment.Exit(0);
                    }
                    _texConvExists = true;

                    TXS3.ImageFormat format = 0;
                    if (args.Contains("--DXT1"))
                        format = TXS3.ImageFormat.DXT1;
                    else if (args.Contains("--DXT3"))
                        format = TXS3.ImageFormat.DXT3;
                    else if (args.Contains("--DXT5"))
                        format = TXS3.ImageFormat.DXT5;
                    else if (args.Contains("--DXT10"))
                        format = TXS3.ImageFormat.DXT10;
                    else
                    {
                        Console.WriteLine("If you tried to convert an image to TXS, provide the corresponding image format argument at the end. (--DXT1/DXT3/DXT5/DXT10)");
                        Environment.Exit(0);
                    }

                    if (TXS3.ToTXS3File(path, format))
                        Console.WriteLine($"Converted {path} to TXS3");
                    else
                        Console.WriteLine($"Could not process {path}.");
                    return;
            }
        }

        static void ProcessTXS3Texture(string path)
        {
            var tex = TXS3.ParseFromFile(path);
            Console.WriteLine($"DDS Image format: {tex.Format}");

            string finalFileName = Path.ChangeExtension(path, ".png");

            tex.SaveAsPng(finalFileName);
            Console.WriteLine($"Converted {currentFileName} to png.");
        }

        static string GetFileMagic(string path)
        {
            using var fs = new FileStream(path, FileMode.Open);

            Span<byte> mBuf = stackalloc byte[4];
            fs.Read(mBuf);
            return Encoding.ASCII.GetString(mBuf);
        }
    }
}
