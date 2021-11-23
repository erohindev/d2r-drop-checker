using System;
using System.Runtime.InteropServices;

namespace D2Tools.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct D2UnitHashTableData
    {
        [FieldOffset(0x00)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public IntPtr[] UnitTable;
    }
}
