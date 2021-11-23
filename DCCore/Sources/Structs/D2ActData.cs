using System;
using System.Runtime.InteropServices;

namespace D2Tools.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct D2ActData
    {
        [FieldOffset(0x14)] public uint MapSeed;
        [FieldOffset(0x20)] public uint ActId;
        [FieldOffset(0x70)] public IntPtr pActMisc;
    }
}
