using System;
using System.Runtime.InteropServices;
using D2Tools.Types;

namespace D2Tools.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct D2UnitAnyData
    {
        [FieldOffset(0x00)] public D2UnitType UnitType;
        [FieldOffset(0x04)] public uint TXTID;
        [FieldOffset(0x08)] public uint UniqueIdentifier;
        [FieldOffset(0x0C)] public uint Mode;
        [FieldOffset(0x10)] public IntPtr pUnitData;
        [FieldOffset(0x20)] public IntPtr pAct;
        [FieldOffset(0x38)] public IntPtr pPath; // XY here
        [FieldOffset(0x88)] public IntPtr pStatsListEx;
        [FieldOffset(0x90)] public IntPtr pInventory;
        [FieldOffset(0xC4)] public ushort X;
        [FieldOffset(0xC6)] public ushort Y;
        [FieldOffset(0x150)] public IntPtr pListNext;
        [FieldOffset(0x158)] public IntPtr pRoomNext;

        public override bool Equals(object obj) => obj is D2UnitAnyData other && Equals(other);

        public bool Equals(D2UnitAnyData unit) => UniqueIdentifier == unit.UniqueIdentifier;

        public override int GetHashCode() => UniqueIdentifier.GetHashCode();

        public static bool operator ==(D2UnitAnyData unit1, D2UnitAnyData unit2) => unit1.Equals(unit2);

        public static bool operator !=(D2UnitAnyData unit1, D2UnitAnyData unit2) => !(unit1 == unit2);
    }
}
