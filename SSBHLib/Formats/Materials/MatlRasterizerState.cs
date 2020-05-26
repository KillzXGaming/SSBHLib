﻿namespace SSBHLib.Formats.Materials
{
    public enum MatlFillMode
    {
        Line,
        Solid
    }

    public enum MatlCullMode
    {
        Back,
        Front,
        FrontAndBack
    }

    public partial class MatlAttribute
    {
        public class MatlRasterizerState : SsbhFile
        {
            public int FillMode { get; set; }

            public int CullMode { get; set; }

            public float DepthBias { get; set; }

            public float Unk4 { get; set; }

            public float Unk5 { get; set; }

            public int Unk6 { get; set; }

            public int Unk7 { get; set; }

            public float Unk8 { get; set; }

            public override string ToString()
            {
                return GetPropertyValues(GetType(), this);
            }
        }
    }
}