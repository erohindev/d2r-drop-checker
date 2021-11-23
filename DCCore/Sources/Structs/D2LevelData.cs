using System.Runtime.InteropServices;
using D2Tools.Types;

namespace D2Tools.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct D2LevelData
    {
        [FieldOffset(0x1F8)] public Area LevelId;
    }
}
