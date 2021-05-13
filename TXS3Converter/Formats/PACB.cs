using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using Syroot.BinaryData.Core;
using Syroot.BinaryData.Memory;

using GTTools.Formats.Entities;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Printing;
using System.Linq;

namespace GTTools.Formats
{
    class PACB
    {
        public enum PACBTypes
        { // ignore all except MDL3 and TXS3 for now
            padding = -1,
            MDL3 = 0,
            MDL3_track,
            TXS3,
            TXS3_data, // data used by track MDL3
        }

        public Dictionary<uint, PACBInfo> PackInfo { get; private set; } = new Dictionary<uint, PACBInfo>();
        public List<(PACBTypes, int)> DataOffsets { get; private set; } = new List<(PACBTypes, int)>();

        public const string MAGIC = "PACB";
        public const string MAGIC_LE = "PACL";

        public Dictionary<uint, MDL3> Models { get; private set; } = new Dictionary<uint, MDL3>();
        public List<List<TXS3>> Textures = new();

        static PACB LoadPack(ref ReadOnlySpan<byte> span)
        {
            var sr = new SpanReader(span, endian: Endian.Big);

            if (sr.Length < 0x40)
                throw new InvalidDataException("File is too small to be a PACB pack.");

            string magic = sr.ReadStringRaw(4);
            if (magic != MAGIC && magic != MAGIC_LE)
                throw new InvalidDataException("Not a valid PACB pack file.");

            sr.Endian = magic == MAGIC_LE ? Endian.Little : Endian.Big;

            sr.Position = 0x3C;
            uint tocEntryCount = sr.ReadUInt32();

            PACB pacb = new();
            for (uint i = 0; i < tocEntryCount; i++)
                pacb.PackInfo.Add(i, PACBInfo.FromStream(ref sr));

            if (pacb.PackInfo[0].Type != 0)
                throw new InvalidDataException("PACB: First entry is not type 0/'course pack'.");

            sr.Position = pacb.PackInfo[0].DataStart;

            int packOffset = pacb.PackInfo[0].DataStart;

            for (uint i = 0; i < 64; i++)
            {
                PACBTypes type = PACBTypes.padding;
                switch (i)
                {
                    case 1:
                        type = PACBTypes.MDL3_track;
                        break;
                    case uint when i < 7: // i hate this
                    case 24:
                        type = PACBTypes.MDL3;
                        break;
                    case 36:
                        type = PACBTypes.TXS3;
                        break;
                    case 43:
                        type = PACBTypes.TXS3_data;
                        break;
                }

                int offsetValue = sr.ReadInt32();
                if (offsetValue != 0 && type != PACBTypes.padding)
                {
                    pacb.DataOffsets.Add((type, offsetValue + packOffset));
                    Console.WriteLine($"Packed data type {type} at 0x{offsetValue + packOffset:X}");
                }
                else if (offsetValue != 0)
                {
                    pacb.DataOffsets.Add((type, offsetValue + packOffset));
                    Console.WriteLine($"Packed data type Other at 0x{offsetValue + packOffset:X}");
                }
            }

            return pacb;
        }

        public static PACB FromFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("File does not exist");

            ReadOnlySpan<byte> span = File.ReadAllBytes(path);

            PACB pacb = LoadPack(ref span);
            var sr = new SpanReader(span, endian: Endian.Big);

            int trackModelOffset = 0x0;

            uint n = 1;
            for (int i = 0; i < pacb.DataOffsets.Count; i++)
            {
                if (pacb.DataOffsets[i].Item1 == PACBTypes.MDL3)
                {
                    if (i < pacb.DataOffsets.Count - 1)
                    {
                        pacb.Models.Add(n, MDL3.ModelFromSpan(
                                            span[pacb.DataOffsets[i].Item2..pacb.DataOffsets[i+1].Item2]));
                    }
                    else
                    {
                        pacb.Models.Add(n, MDL3.ModelFromSpan(span[pacb.DataOffsets[i].Item2..]));
                    }
                    n++;
                }
                else if (pacb.DataOffsets[i].Item1 == PACBTypes.TXS3)
                {
                    sr.Position = pacb.DataOffsets[i].Item2;
                    if (i < pacb.DataOffsets.Count - 1)
                    {
                        pacb.Textures.Add(TXS3.FromStream(ref sr, pacb.DataOffsets[i+1].Item2));
                    }
                    else
                    {
                        pacb.Textures.Add(TXS3.FromStream(ref sr, sr.Length));
                    }
                }
                else if (pacb.DataOffsets[i].Item1 == PACBTypes.MDL3_track)
                {
                    trackModelOffset = pacb.DataOffsets[i].Item2;
                    pacb.Models.Add(0, MDL3.ModelFromSpan(
                                        span[trackModelOffset..pacb.DataOffsets[i + 1].Item2]));
                }
                else if (pacb.DataOffsets[i].Item1 == PACBTypes.TXS3_data)
                {
                    sr.Position = trackModelOffset;
                    pacb.Textures.Add(TXS3.FromStream_ModelIndex(ref sr, pacb.DataOffsets[i].Item2));
                }
            }

            return pacb;
        }

        public static void EditFile(string path, Dictionary<int, MDL3Mesh.Vertex[]> meshEditVertex, List<int> meshDelete)
        {
            byte[] bytes = File.ReadAllBytes(path);

            ReadOnlySpan<byte> span = bytes;
            PACB pacb = LoadPack(ref span);
            //Console.WriteLine($"- Loaded PACB.");

            for (int i = 0; i < pacb.DataOffsets.Count; i++)
            {
                if (pacb.DataOffsets[i].Item1 == PACBTypes.MDL3)
                {
                    //Console.WriteLine($"- Found MDL3 @ 0x{pacb.DataOffsets[i].Item2:X}");
                    bytes = MDL3.EditModel(bytes, pacb.DataOffsets[i].Item2, meshEditVertex, meshDelete);
                    break;
                }
            }

            File.WriteAllBytes(path, bytes);
        }
    }
}
