using System;
using System.Runtime.InteropServices;

namespace D2Tools.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct D2RoomExData
    {
        [FieldOffset(0x90)] public IntPtr pLevel;
    }
}
