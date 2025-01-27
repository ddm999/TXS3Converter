﻿using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using Syroot.BinaryData.Memory;

using GTTools.Formats.Entities;
namespace GTTools.Formats
{
    class MDL3
    {
        public const string MAGIC = "MDL3";

        public Dictionary<uint, MDL3MeshInfo> MeshInfo { get; private set; } = new Dictionary<uint, MDL3MeshInfo>();

        public Dictionary<uint, MDL3Mesh> Meshes { get; private set; } = new Dictionary<uint, MDL3Mesh>();

        public List<TXS3> Textures { get; set; } 

        //Extract MDL3 Standard and (HIGHLOD vertices only)
        public static MDL3 FromFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("File does not exist");

            byte[] file = File.ReadAllBytes(path);

            // Every format is read as BE.
            var sr = new SpanReader(file, endian: Syroot.BinaryData.Core.Endian.Big);

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

            sr.Position = (int)meshInfoTableAddress;
            for (int i = 0; i < meshCount; i++)
            {
                MDL3Mesh mesh = MDL3Mesh.FromStream(ref sr);
                mdl.Meshes.Add(mesh.MeshIndex, mesh);
            }

            sr.Position = (int)unkOffset;
            for (int i = 0; i < meshInfoCount; i++)
            {
                MDL3MeshInfo meshInfo = MDL3MeshInfo.FromStream(ref sr);
                mdl.MeshInfo.Add(meshInfo.MeshIndex, meshInfo);
            }

            // Flexible Vertexes
            MDL3FVF[] vertexInfo = new MDL3FVF[fvfTableCount];
            sr.Position = (int)fvfTableAddress;
            for (int i = 0; i < fvfTableCount; i++)
                vertexInfo[i] = MDL3FVF.FromStream(ref sr);

            return null;
        }
    }
}
