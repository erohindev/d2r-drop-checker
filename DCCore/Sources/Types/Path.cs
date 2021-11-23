using System;
using D2Tools.Helpers;
using D2Tools.Interfaces;

namespace D2Tools.Types
{
    public class Path : IUpdatable<Path>
    {
        private readonly IntPtr _pPath = IntPtr.Zero;
        private Structs.D2PathData _path;

        public Path(IntPtr pPath)
        {
            _pPath = pPath;
            Update();
        }

        public Path Update()
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                _path = processContext.Read<Structs.D2PathData>(_pPath);
            }
            return this;
        }

        public ushort DynamicX => _path.DynamicX;
        public ushort DynamicY => _path.DynamicY;
        public ushort StaticX => _path.StaticX;
        public ushort StaticY => _path.StaticY;
        public Room Room => new Room(_path.pRoom);
    }
}