using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using Syroot.BinaryData.Core;
using Syroot.BinaryData.Memory;

using GTTools.Formats.Entities;
namespace GTTools.Formats
{
    class MDL3
    {
        public const string MAGIC = "MDL3";

        public Dictionary<uint, MDL3MeshInfo> MeshInfo = new();
        public List<TXS3> Textures = new();
        public List<MDL3Mesh> Meshes = new();

        public static MDL3 ModelFromSpan(ReadOnlySpan<byte> span)
        {
            var sr = new SpanReader(span, endian: Endian.Big);

            MDL3 mdl = new MDL3();

            if (sr.ReadStringRaw(4) != MAGIC)
                return mdl;

            // in packs, this sometimes isn't size? like, whopping huge compared to real size
            //  so maybe this is memory to reserve? and texture data adds to make this size?
            //  that would imply that the MDL is responsible for loading the DDS data though,
            //   and I don't really think that's true?
            int size = sr.ReadInt32();

            sr.Position += 0x0C;
            uint meshCount = sr.ReadUInt16();
            uint meshInfoCount = sr.ReadUInt16();
            uint fvfTableCount = sr.ReadUInt16();
            uint boneCount = sr.ReadUInt16();

            sr.Position += 0x1C;
            uint meshTableAddress = sr.ReadUInt32();
            uint meshInfoTableAddress = sr.ReadUInt32();
            uint fvfTableAddress = sr.ReadUInt32();
            uint unkOffset2 = sr.ReadUInt32();
            uint txsOffset = sr.ReadUInt32();
            uint shdsOffset = sr.ReadUInt32();
            uint boneOffset = sr.ReadUInt32();

            Console.WriteLine($"Found model with {meshCount} meshes");

            MDL3FVF[] fvfInfo = new MDL3FVF[fvfTableCount];
            for (int i = 0; i < fvfTableCount; i++)
            {
                sr.Position = (int)(fvfTableAddress + i*0x78);
                fvfInfo[i] = MDL3FVF.FromStream(ref sr);
            }

            mdl.MeshInfo = new();
            for (uint i = 0; i < meshInfoCount; i++)
            {
                sr.Position = (int)(meshInfoTableAddress + i*0x08);
                mdl.MeshInfo.Add(i, MDL3MeshInfo.FromStream(ref sr));
            }

            mdl.Meshes = new();
            for (int i = 0; i < meshCount; i++)
            {
                sr.Position = (int)(meshTableAddress + i*0x30);
                mdl.Meshes.Add(MDL3Mesh.FromStream(ref sr, fvfInfo, i));
            }

            if (txsOffset >= span.Length)
            {
                Console.WriteLine("bruh");
            }

            return mdl;
        }

        public static MDL3 TexturesFromFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("File does not exist");

            var sr = new SpanReader(File.ReadAllBytes(path), endian: Endian.Big);

            if (sr.Length < 4 || sr.ReadStringRaw(4) != MAGIC)
                throw new InvalidDataException("Not a valid MDL3 image file.");

            var size = sr.ReadInt32();
            if (size != sr.Length)
                Console.WriteLine($"MDL3 Warning: Hardcoded file size does not match actual file size ({size} != {sr.Length}). Might break.");

            Directory.CreateDirectory(Path.GetFileName(path) + "_extracted");
            MDL3 mdl = new MDL3();
            // Mesh Infos
            sr.Position = 0x10; // 16

            uint meshCount = sr.ReadUInt16();
            uint meshInfoCount = sr.ReadUInt16(); // Same as above

            uint fvfTableCount = sr.ReadUInt16();
            uint boneCount = sr.ReadUInt16();

            sr.Position = 0x38; // 56
            uint meshInfoTableAddress = sr.ReadUInt32();
            uint unkOffset = sr.ReadUInt32();
            uint fvfTableAddress = sr.ReadUInt32();
            uint unkOffset2 = sr.ReadUInt32();
            uint txsOffset = sr.ReadUInt32();
            uint shdsOffset = sr.ReadUInt32();
            uint boneOffset = sr.ReadUInt32();

            sr.Position = (int)txsOffset;

            mdl.Textures = TXS3.FromStream(ref sr);
            int j = 0;
            foreach (var text in mdl.Textures)
            {
                text.SaveAsPng($"{j}.png");
                j++;
            }

            return null;
        }
    }
}
