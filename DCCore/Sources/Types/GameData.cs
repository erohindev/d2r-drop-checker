using System;
using System.Collections.Generic;
using D2Tools.Helpers;

namespace D2Tools.Types
{
    public class GameData
    {
        public static event Action<Area> OnAreaChanged; 

        public IntPtr MainWindowHandle;

        public D2UnitAnyType CurrentPlayer;
        public Difficulty CurrentDifficulty;
        public Area CurrentArea;

        public List<D2UnitAnyType> ItemUnits;

        public GameData Update()
        {
            var playerUnit = GameManager.GetPlayerUnit();
            
            ItemUnits.Clear();
            
            if (!playerUnit.IsValid()) return this;

            CurrentPlayer = playerUnit;

            MainWindowHandle = GameManager.MainWindowHandle;
            
            if (MainWindowHandle == IntPtr.Zero) return null;
            
            var levelId = playerUnit.Path.Room.RoomEx.Level.LevelId;
            
            if(CurrentArea != levelId)
                OnAreaChanged?.Invoke(levelId);
            
            CurrentArea = levelId;
            CurrentDifficulty = playerUnit.Act.ActMisc.GameDifficulty;
            
            ItemUnits = GameManager.GetItemUnits(ItemUnits);

            return this;
        }

        public static GameData GetNew()
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                var playerUnit = GameManager.GetPlayerUnit();

                if (!playerUnit.IsValid()) return null;

                var mapSeed = playerUnit.Act.MapSeed;

                if (mapSeed <= 0 || mapSeed > 0xFFFFFFFF)
                {
                    //Console.WriteLine("Map seed is out of bounds.");
                    return null;
                }

                var gameDifficulty = playerUnit.Act.ActMisc.GameDifficulty;

                if (!gameDifficulty.IsValid())
                {
                    //Console.WriteLine("Game difficulty out of bounds.");
                    return null;
                }

                var levelId = playerUnit.Path.Room.RoomEx.Level.LevelId;

                if (!levelId.IsValid())
                {
                    //Console.WriteLine("Level id out of bounds.");
                    return null;
                }
                
                OnAreaChanged?.Invoke(levelId);

                return new GameData
                {
                    MainWindowHandle = GameManager.MainWindowHandle,

                    CurrentPlayer = playerUnit,
                    CurrentArea = levelId,
                    CurrentDifficulty = gameDifficulty,

                    ItemUnits = GameManager.GetItemUnits()
                };
            }
        }
    }
}
