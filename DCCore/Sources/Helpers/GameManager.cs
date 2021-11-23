using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D2Tools.Structs;
using D2Tools.Types;

namespace D2Tools.Helpers
{
    public static class GameManager
    {
        public static event Action<string, bool> OnLog; 
        public static event Action OnGameProcessExit; 

        public static bool Run = true;
        
        public static IntPtr MainWindowHandle => _mainWindowHandle;

        private static readonly string ProcessName = Encoding.UTF8.GetString(new byte[] { 68, 50, 82 });

        private static int _lastProcessId = 0;
        private static IntPtr _mainWindowHandle = IntPtr.Zero;
        private static ProcessContext _processContext;
        private static Process _gameProcess;

        private static D2UnitAnyType _playerUnit;
        
        private static IntPtr _unitHashTableOffset;
        private static IntPtr _uiSettingOffset;
        private static IntPtr _expansionCheckOffset;

        public static async Task WaitForGame()
        {
            while (Run && _processContext == null)
            {
                _processContext = GetProcessContext();
                
                await Task.Delay(1000);
            }
        }

        public static ProcessContext GetProcessContext()
        {
            if (_processContext != null && _processContext.OpenContextCount > 0)
            {
                OnLog?.Invoke("", true);
                
                _processContext.OpenContextCount++;
                return _processContext;
            }
            
            OnLog?.Invoke("Looking for ProcessContext...", false);

            if (_gameProcess == null)
            {
                Process[] processes = Process.GetProcessesByName(ProcessName);

                IntPtr windowInFocus = WindowsExternal.GetForegroundWindow();

                if (windowInFocus == IntPtr.Zero)
                    _gameProcess = processes.FirstOrDefault();
                else
                    _gameProcess = processes.FirstOrDefault(p => p.MainWindowHandle == windowInFocus);

                if (_gameProcess == null)
                    _gameProcess = processes.FirstOrDefault();

                if (_gameProcess == null)
                {
                    OnLog?.Invoke("Game process not found", false);
                    return null;
                }

                // If changing processes we need to re-find the player
                if (_gameProcess.Id != _lastProcessId) ResetPlayerUnit();
            }
            
            _gameProcess.EnableRaisingEvents = true;
            _gameProcess.Exited += (sender, args) => OnGameProcessExit?.Invoke();

            _lastProcessId = _gameProcess.Id;
            _mainWindowHandle = _gameProcess.MainWindowHandle;
            _processContext = new ProcessContext(_gameProcess);

            return _processContext;
        }

        public static D2UnitAnyType GetPlayerUnit()
        {
            using (var processContext = GetProcessContext())
            {
                foreach (var pUnitAny in UnitHashTable.UnitTable)
                {
                    var unitAny = new D2UnitAnyType(pUnitAny);

                    while (unitAny.IsValid())
                    {
                        if (unitAny.IsPlayerUnit())
                        {
                            _playerUnit = unitAny;
                            OnLog?.Invoke("", true);
                            return _playerUnit;
                        }
                            
                        unitAny = unitAny.ListNext;
                    }
                }
                
                OnLog?.Invoke("PlayerUnit not found", false);

                return new D2UnitAnyType(IntPtr.Zero);
            }
        }

        public static List<D2UnitAnyType> GetItemUnits(List<D2UnitAnyType> items = null)
        {
            using (var processContext = GetProcessContext())
            {
                if(items == null) items = new List<D2UnitAnyType>();
                else items.Clear();

                foreach (var pUnitAny in ItemHashTable.UnitTable)
                {
                    var unitAny = new D2UnitAnyType(pUnitAny);

                    while (unitAny.IsValid())
                    {
                        if (unitAny.IsDroppedItem())
                        {
                            items.Add(unitAny);
                        }

                        unitAny = unitAny.ListNext;
                    }
                }

                return items;
            }
        }

        // UNIT
        private static D2UnitHashTableData UnitHashTable
        {
            get
            {
                using (var processContext = GetProcessContext())
                {
                    if (_unitHashTableOffset == IntPtr.Zero)
                        _unitHashTableOffset = processContext.GetUnitHashtableOffset();

                    return processContext.Read<D2UnitHashTableData>(_unitHashTableOffset);
                }
            }
        }

        // ITEM
        private static D2UnitHashTableData ItemHashTable
        {
            get
            {
                using (var processContext = GetProcessContext())
                {
                    return processContext.Read<D2UnitHashTableData>(_unitHashTableOffset + 128 * 32);
                }
            }
        }

        public static IntPtr ExpansionCheckOffset
        {
            get
            {
                if (_expansionCheckOffset != IntPtr.Zero)
                    return _expansionCheckOffset;

                using (var processContext = GetProcessContext())
                    _expansionCheckOffset = processContext.GetExpansionOffset();

                return _expansionCheckOffset;
            }
        }
        
        /*public static UiSettings UiSettings
        {
            get
            {
                using (var processContext = GetProcessContext())
                {
                    if (_UiSettingOffset == IntPtr.Zero)
                        _UiSettingOffset = processContext.GetUiSettingsOffset();
                    
                    return new UiSettings(_UiSettingOffset);
                }
            }
        }*/

        private static void ResetPlayerUnit()
        {
            _playerUnit = default;
            _unitHashTableOffset = IntPtr.Zero;
            _expansionCheckOffset = IntPtr.Zero;
            _uiSettingOffset = IntPtr.Zero;
        }
    }
    
    /*public static class Offsets
    {
        public static int UnitHashTable = 0x20AF660;
        public static int UiSettings = 0x20BF322;
        public static int ExpansionCheck = 0x20BF335;
    }*/
}
