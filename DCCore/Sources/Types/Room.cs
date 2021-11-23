/**
 *   Copyright (C) 2021 okaygo
 *
 *   https://github.com/misterokaygo/MapAssist/
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <https://www.gnu.org/licenses/>.
 **/

using D2Tools.Helpers;
using D2Tools.Interfaces;
using System;
using System.Linq;

namespace D2Tools.Types
{
    public class Room : IUpdatable<Room>
    {
        private readonly IntPtr _pRoom = IntPtr.Zero;
        private Structs.D2RoomData _room;

        public Room(IntPtr pRoom)
        {
            _pRoom = pRoom;
            Update();
        }

        public Room Update()
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                _room = processContext.Read<Structs.D2RoomData>(_pRoom);
            }
            return this;
        }
        
        public RoomEx RoomEx => new RoomEx(_room.pRoomEx);
        public uint NumRoomsNear => _room.numRoomsNear;
        public Act Act => new Act(_room.pAct);
        public D2UnitAnyType UnitFirst => new D2UnitAnyType(_room.pUnitFirst);
        public Room RoomNext => new Room(_room.pRoomNext);
    }
}