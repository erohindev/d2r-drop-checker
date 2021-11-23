using System;
using D2Tools.Helpers;
using D2Tools.Interfaces;
using D2Tools.Structs;

namespace D2Tools.Types
{
    public struct Point
    {
        public static Point Zero = new Point(0, 0);
        
        public int X;
        public int Y;

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
    
    public enum D2UnitType : uint
    {
        Player = 0,
        Monster,
        MapObject,
        Missle,
        Item,
        Tile,
    }

    public struct Pointers
    {
        public IntPtr pointer;
        public IntPtr pUnitData;
        public IntPtr pAct;
        public IntPtr pPath;
        public IntPtr pInventory;
        public IntPtr pMonStats;
        public IntPtr pStatsListEx;
        public IntPtr pFirstStat;

        public override string ToString()
        {
            string output = "Pointers:\r\n";

            output += pointer == IntPtr.Zero ? "" :         "pointer:      " + pointer.ToString("X") + "\r\n";
            output += pUnitData == IntPtr.Zero ? "" :       "pUnitData:    " + pUnitData.ToString("X") + "\r\n";
            output += pAct == IntPtr.Zero ? "" :            "pAct:         " + pAct.ToString("X") + "\r\n";
            output += pPath == IntPtr.Zero ? "" :           "pPath:        " + pPath.ToString("X") + "\r\n";
            output += pInventory == IntPtr.Zero ? "" :      "pInventory:   " + pInventory.ToString("X") + "\r\n";
            output += pMonStats == IntPtr.Zero ? "" :       "pMonStats:    " + pMonStats.ToString("X") + "\r\n";
            output += pStatsListEx == IntPtr.Zero ? "" :    "pStatsListEx: " + pStatsListEx.ToString("X") + "\r\n";
            output += pFirstStat == IntPtr.Zero ? "" :      "pFirstStat:   " + pFirstStat.ToString("X") + "\r\n";

            return output;
        }
    }

    public class D2UnitAnyType : IUpdatable<D2UnitAnyType>
    {
        public IntPtr Pointer;
        
        public uint TXTID => UnitAnyData.TXTID;
        public uint UID => UnitAnyData.UniqueIdentifier;

        public D2UnitType UnitType => UnitAnyData.UnitType;

        public Act Act;
        public Path Path;
        public ushort X => IsMovable() ? Path.DynamicX : Path.StaticX;
        public ushort Y => IsMovable() ? Path.DynamicY : Path.StaticY;
        public Point Position => new Point(X, Y);

        public D2UnitAnyData UnitAnyData;

        public ItemD2 ItemD2;
        
        public D2UnitAnyType ListNext => new D2UnitAnyType(UnitAnyData.pListNext);

        public D2UnitAnyType(IntPtr pointer)
        {
            Pointer = pointer;
            
            Update();
        }

        public D2UnitAnyType Update()
        {
            if (IsValid())
            {
                using (var processContext = GameManager.GetProcessContext())
                {
                    UnitAnyData = processContext.Read<D2UnitAnyData>(Pointer);
                    
                    Path = new Path(UnitAnyData.pPath);

                    switch (UnitAnyData.UnitType)
                    {
                        // Unit is PLAYER
                        case D2UnitType.Player:
                            Act = new Act(UnitAnyData.pAct);
                            break;
                        case D2UnitType.Item:
                            if ((D2ItemLocation)UnitAnyData.Mode == D2ItemLocation.GROUND)
                            {
                                var statsList = processContext.Read<D2StatListData>(UnitAnyData.pStatsListEx);
                                var statsValues = processContext.Read<D2StatValueData>(statsList.Stats.FirstStatPtr, Convert.ToInt32(statsList.Stats.Size));

                                ItemD2 = new ItemD2
                                {
                                    Unit = this,

                                    Pointers = new Pointers
                                    {
                                        pointer = Pointer,
                                        pUnitData = UnitAnyData.pUnitData,
                                        pAct = UnitAnyData.pAct,
                                        pPath = UnitAnyData.pPath,
                                        pInventory = UnitAnyData.pInventory,
                                        pStatsListEx = UnitAnyData.pStatsListEx,
                                        pFirstStat = statsList.Stats.FirstStatPtr
                                    },
                                    ItemData = processContext.Read<D2ItemData>(Pointer),
                                    ItemUnitData = processContext.Read<D2ItemUnitData>(UnitAnyData.pUnitData),

                                    StatsListData = statsList,
                                    Stats = statsValues
                                };
                            }
                            break;
                    }
                }
            }
            
            return this;
        }

        private bool IsMovable()
        {
            return !(UnitType == D2UnitType.MapObject || UnitType == D2UnitType.Item);
        }

        public bool IsValid()
        {
            return Pointer != IntPtr.Zero;
        }

        public bool IsPlayerUnit()
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                if (UnitType == D2UnitType.Player && UnitAnyData.pInventory != IntPtr.Zero)
                {
                    var expansionCharacter = processContext.Read<byte>(GameManager.ExpansionCheckOffset) == 1;
                    //var expansionCharacter = processContext.Read<byte>(processContext.FromOffset(Offsets.ExpansionCheck)) == 1;
                    var userBaseOffset = 0x30;
                    var checkUser1 = 1;
                    
                    if (expansionCharacter)
                    {
                        userBaseOffset = 0x70;
                        checkUser1 = 0;
                    }
                    
                    var userBaseCheck = processContext.Read<int>(IntPtr.Add(UnitAnyData.pInventory, userBaseOffset));
                    
                    if (userBaseCheck != checkUser1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool IsDroppedItem()
        {
            return UnitType == D2UnitType.Item && (D2ItemLocation)UnitAnyData.Mode == D2ItemLocation.GROUND;
        }

        public override bool Equals(object obj) => obj is D2UnitAnyType other && Equals(other);

        public bool Equals(D2UnitAnyType unit) => UID == unit.UID;

        public override int GetHashCode() => UID.GetHashCode();

        public static bool operator ==(D2UnitAnyType unit1, D2UnitAnyType unit2) => unit1.Equals(unit2);

        public static bool operator !=(D2UnitAnyType unit1, D2UnitAnyType unit2) => !(unit1 == unit2);
    }
}