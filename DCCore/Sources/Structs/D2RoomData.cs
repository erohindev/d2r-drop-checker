using System;
using System.Runtime.InteropServices;

namespace D2Tools.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct D2RoomData
    {
        [FieldOffset(0x00)] public IntPtr pRoomsNear;
        [FieldOffset(0x18)] public IntPtr pRoomEx;
        [FieldOffset(0x40)] public uint numRoomsNear;
        [FieldOffset(0x48)] public IntPtr pAct;
        [FieldOffset(0xA8)] public IntPtr pUnitFirst;
        [FieldOffset(0xB0)] public IntPtr pRoomNext;
    }
}
