﻿using CrossMod.Rendering;
using CrossMod.Rendering.Resources;
using CrossMod.Tools;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

// Classes ported from StudioSB
// https://github.com/Ploaj/StudioSB/blob/master/LICENSE
namespace CrossMod.Nodes
{
    public class NutexNode : FileNode, IRenderableNode
    {
        public string TexName { get; }

        public Lazy<IRenderable> Renderable { get; }

        public NutexNode(string path) : base(path, "texture", true)
        {
            var surface = Open(AbsolutePath);
            TexName = surface?.Name ?? Path.GetFileNameWithoutExtension(AbsolutePath);


            if (surface == null)
                Renderable = new Lazy<IRenderable>(new RTexture(DefaultTextures.Instance.Value.DefaultBlack, false));
            else
                Renderable = new Lazy<IRenderable>(new RTexture(surface.GetRenderTexture(), surface.IsSRGB));
        }

        public override string ToString()
        {
            return Text.Contains(".") ? Text.Substring(0, Text.IndexOf(".")) : Text;
        }

        private static SBSurface? Open(string FilePath)
        {
            using (BinaryReader reader = new BinaryReader(new FileStream(FilePath, FileMode.Open)))
            {
                // TODO: Why are there empty streams?
                if (reader.BaseStream.Length == 0)
                    return null;

                SBSurface surface = new SBSurface();

                reader.BaseStream.Position = reader.BaseStream.Length - 0xB0;

                int[] mipmapSizes = new int[16];
                for (int i = 0; i < mipmapSizes.Length; i++)
                    mipmapSizes[i] = reader.ReadInt32();

                reader.ReadChars(4); // TNX magic

                string texName = ReadTexName(reader);
                surface.Name = texName;

                surface.Width = reader.ReadInt32();
                surface.Height = reader.ReadInt32();
                surface.Depth = reader.ReadInt32();

                var Format = (NUTEX_FORMAT)reader.ReadByte();

                reader.ReadByte();

                ushort Padding = reader.ReadUInt16();
                reader.ReadUInt32();

                int MipCount = reader.ReadInt32();
                int Alignment = reader.ReadInt32();
                surface.ArrayCount = reader.ReadInt32();
                int ImageSize = reader.ReadInt32();
                char[] Magic = reader.ReadChars(4);
                int MajorVersion = reader.ReadInt16();
                int MinorVersion = reader.ReadInt16();

                if (pixelFormatByNuTexFormat.ContainsKey(Format))
                    surface.PixelFormat = pixelFormatByNuTexFormat[Format];

                if (internalFormatByNuTexFormat.ContainsKey(Format))
                    surface.InternalFormat = internalFormatByNuTexFormat[Format];

                surface.PixelType = GetPixelType(Format);

                reader.BaseStream.Position = 0;
                byte[] ImageData = reader.ReadBytes(ImageSize);

                for (int array = 0; array < surface.ArrayCount; array++)
                {
                    MipArray arr = new MipArray();
                    for (int i = 0; i < MipCount; i++)
                    {
                        byte[] deswiz = SwitchSwizzler.GetImageData(surface, ImageData, array, i, MipCount);
                        arr.Mipmaps.Add(deswiz);
                    }
                    surface.Arrays.Add(arr);
                }

                return surface;
            }
        }

        public IRenderable GetRenderableNode()
        {
            return Renderable.Value;
        }

        private static string ReadTexName(BinaryReader reader)
        {
            var result = "";
            for (int i = 0; i < 0x40; i++)
            {
                byte b = reader.ReadByte();
                if (b != 0)
                    result += (char)b;
            }

            return result;
        }

        private static readonly Dictionary<NUTEX_FORMAT, InternalFormat> internalFormatByNuTexFormat = new Dictionary<NUTEX_FORMAT, InternalFormat>()
        {
            { NUTEX_FORMAT.R8G8B8A8_SRGB, InternalFormat.Srgb8Alpha8 },
            { NUTEX_FORMAT.R8G8B8A8_UNORM, InternalFormat.Rgba8 },
            { NUTEX_FORMAT.R32G32B32A32_FLOAT, InternalFormat.Rgba32f },
            { NUTEX_FORMAT.B8G8R8A8_UNORM, InternalFormat.Rgba8 },
            { NUTEX_FORMAT.B8G8R8A8_SRGB, InternalFormat.Rgba8Snorm },
            { NUTEX_FORMAT.BC1_UNORM, InternalFormat.CompressedRgbaS3tcDxt1Ext },
            { NUTEX_FORMAT.BC1_SRGB, InternalFormat.CompressedSrgbAlphaS3tcDxt1Ext },
            { NUTEX_FORMAT.BC2_UNORM, InternalFormat.CompressedRgbaS3tcDxt3Ext },
            { NUTEX_FORMAT.BC2_SRGB, InternalFormat.CompressedSrgbAlphaS3tcDxt3Ext },
            { NUTEX_FORMAT.BC3_UNORM, InternalFormat.CompressedRgbaS3tcDxt5Ext },
            { NUTEX_FORMAT.BC3_SRGB, InternalFormat.CompressedSrgbAlphaS3tcDxt5Ext },
            { NUTEX_FORMAT.BC4_UNORM, InternalFormat.Rgba },
            { NUTEX_FORMAT.BC4_SNORM, InternalFormat.Rgba },
            { NUTEX_FORMAT.BC5_UNORM, InternalFormat.Rgba },
            { NUTEX_FORMAT.BC5_SNORM, InternalFormat.Rgba },
            { NUTEX_FORMAT.BC6_UFLOAT, InternalFormat.CompressedRgbBptcUnsignedFloat },
            { NUTEX_FORMAT.BC7_UNORM, InternalFormat.CompressedRgbaBptcUnorm },
            { NUTEX_FORMAT.BC7_SRGB, InternalFormat.CompressedSrgbAlphaBptcUnorm }
        };

        private static PixelType GetPixelType(NUTEX_FORMAT format)
        {
            switch (format)
            {
                case NUTEX_FORMAT.R32G32B32A32_FLOAT:
                    return PixelType.Float;
                default:
                    return PixelType.UnsignedByte;
            }
        }
        /// <summary>
        /// Channel information for uncompressed formats.
        /// </summary>
        private static readonly Dictionary<NUTEX_FORMAT, PixelFormat> pixelFormatByNuTexFormat = new Dictionary<NUTEX_FORMAT, PixelFormat>()
        {
            { NUTEX_FORMAT.R32G32B32A32_FLOAT, PixelFormat.Rgba },
            { NUTEX_FORMAT.R8G8B8A8_SRGB, PixelFormat.Rgba },
            { NUTEX_FORMAT.R8G8B8A8_UNORM, PixelFormat.Rgba },
            { NUTEX_FORMAT.B8G8R8A8_UNORM, PixelFormat.Bgra },
            { NUTEX_FORMAT.B8G8R8A8_SRGB, PixelFormat.Bgra },
        };
    }

    public enum NUTEX_FORMAT
    {
        R8G8B8A8_UNORM = 0,
        R8G8B8A8_SRGB = 0x05,
        R32G32B32A32_FLOAT = 0x34,
        B8G8R8A8_UNORM = 0x50,
        //53
        B8G8R8A8_SRGB = 0x55,
        BC1_UNORM = 0x80,
        BC1_SRGB = 0x85,
        BC2_UNORM = 0x90,
        BC2_SRGB = 0x95,
        BC3_UNORM = 0xa0,
        BC3_SRGB = 0xa5,
        BC4_UNORM = 0xb0,
        BC4_SNORM = 0xb5,
        BC5_UNORM = 0xc0,
        BC5_SNORM = 0xc5,
        BC6_UFLOAT = 0xd7,
        BC7_UNORM = 0xe0,
        BC7_SRGB = 0xe5
    }
}
