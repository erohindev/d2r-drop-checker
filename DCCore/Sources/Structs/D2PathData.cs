using System;
using System.Runtime.InteropServices;

namespace D2Tools.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct D2PathData
    {
        [FieldOffset(0x02)] public ushort DynamicX;
        [FieldOffset(0x06)] public ushort DynamicY;
        [FieldOffset(0x10)] public ushort StaticX;
        [FieldOffset(0x14)] public ushort StaticY;
        [FieldOffset(0x20)] public IntPtr pRoom;
    }
}
