using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace D2Tools.Helpers
{
    public class ProcessContext : IDisposable
    {
        private Process _process;
        private IntPtr _handle;
        private bool _disposedValue;
        public int OpenContextCount = 1;

        private IntPtr ProcessBaseAddress
        {
            get
            {
                if (_processBaseAddress == IntPtr.Zero && _process?.MainModule != null)
                    _processBaseAddress = _process.MainModule.BaseAddress;
                    
                return _processBaseAddress;
            }
        }

        private IntPtr _processBaseAddress;
        
        private readonly int _moduleSize;

        public ProcessContext(Process process)
        {
            if (process == null)
            {
                Console.WriteLine("process is null");
                return;
            }
            
            _process = process;
            _handle = WindowsExternal.OpenProcess((uint)WindowsExternal.ProcessAccessFlags.VirtualMemoryRead, false, process.Id);
            
            _moduleSize = _process.MainModule.ModuleMemorySize;
        }

        public IntPtr Handle => _handle;

        public IntPtr FromOffset(int offset)
        {
            return IntPtr.Add(ProcessBaseAddress, offset);
        }

        public T[] Read<T>(IntPtr address, int count) where T : struct
        {
            var sz = Marshal.SizeOf<T>();
            var buf = new byte[sz * count];
            
            var handle = GCHandle.Alloc(buf, GCHandleType.Pinned);
            
            WindowsExternal.ReadProcessMemory(_handle, address, buf, buf.Length, out _);
            
            var result = new T[count];
            
            try
            {
                for (var i = 0; i < count; i++)
                    result[i] = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject() + (i * sz), typeof(T));

                return result;
            }
            finally
            {
                handle.Free();
            }
        }

        public T Read<T>(IntPtr address) where T : struct
        {
            return Read<T>(address, 1)[0];
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                if (_handle != IntPtr.Zero)
                {
                    WindowsExternal.CloseHandle(_handle);
                }

                _process = null;
                _handle = IntPtr.Zero;
                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ProcessContext()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            if (--OpenContextCount > 0)
            {
                return;
            }
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        
        // USABLE?
        public IntPtr GetUnitHashtableOffset()
        {
            var buffer = GetProcessMemory();
            
            IntPtr patternAddress = FindPatternEx(ref buffer, ProcessBaseAddress, _moduleSize, "\x48\x8d\x00\x00\x00\x00\x00\x8b\xd1", "xx?????xx");
            
            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, 3);
            
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                Console.WriteLine("Can't read the process memory");
                return IntPtr.Zero;
            }

            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0);
            var delta = patternAddress.ToInt64() - ProcessBaseAddress.ToInt64();
            
            return IntPtr.Add(ProcessBaseAddress, (int)(delta + 7 + offsetAddressToInt));
        }

        public IntPtr GetUiSettingsOffset()
        {
            var buffer = GetProcessMemory();
            
            var patternAddress = FindPatternEx(ref buffer, ProcessBaseAddress, _moduleSize, "\x40\x84\xed\x0f\x94\x05", "xxxxxx");
            
            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, 6);
            
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                Console.WriteLine("Can't read the process memory");
                return IntPtr.Zero;
            }

            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0);
            var delta = patternAddress.ToInt64() - ProcessBaseAddress.ToInt64();
            
            return IntPtr.Add(ProcessBaseAddress, (int)(delta + 10 + offsetAddressToInt));
        }

        public IntPtr GetExpansionOffset()
        {
            var buffer = GetProcessMemory();
            
            IntPtr patternAddress = FindPatternEx(ref buffer, ProcessBaseAddress, _moduleSize,
                "\xC7\x05\x00\x00\x00\x00\x00\x00\x00\x00\x48\x85\xC0\x0F\x84\x00\x00\x00\x00\x83\x78\x5C\x00\x0F\x84\x00\x00\x00\x00\x33\xD2\x41",
                "xx????xxxxxxxxx????xxxxxx????xxx");
            
            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, -4);
            
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                Console.WriteLine("Can't read the process memory");
                return IntPtr.Zero;
            }

            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0);
            var delta = patternAddress.ToInt64() - ProcessBaseAddress.ToInt64();
            
            return IntPtr.Add(ProcessBaseAddress, (int)(delta + offsetAddressToInt));
        }

        private static int FindPattern(ref byte[] buffer, ref int size, ref string pattern, ref string mask)
        {
            var patternLength = pattern.Length;
            for (var i = 0; i < size - patternLength; i++)
            {
                var found = true;
                for (var j = 0; j < patternLength; j++)
                {
                    if (mask[j] != '?' && pattern[j] != buffer[i + j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found) return i;
            }

            return 0;
        }

        private byte[] GetProcessMemory()
        {
            var memoryBuffer = new byte[_moduleSize];
            
            if (WindowsExternal.ReadProcessMemory(_handle, ProcessBaseAddress, memoryBuffer, _moduleSize, out _) == false)
            {
                Console.WriteLine("Can't read the process memory");
                return null;
            }

            return memoryBuffer;
        }

        private IntPtr FindPatternEx(ref byte[] buffer, IntPtr baseAddr, int size, string pattern, string mask)
        {
            var offset = FindPattern(ref buffer, ref size, ref pattern, ref mask);
            
            return offset != 0 ? IntPtr.Add(baseAddr, offset) : IntPtr.Zero;
        }

        private IntPtr FindPatternEx(IntPtr baseAddr, int size, string pattern, string mask)
        {
            var buffer = new byte[size];
            
            if (!WindowsExternal.ReadProcessMemory(_handle, baseAddr, buffer, size, out _))
            {
                return IntPtr.Zero;
            }

            var offset = FindPattern(ref buffer, ref size, ref pattern, ref mask);
            return offset != 0 ? IntPtr.Add(baseAddr, offset) : IntPtr.Zero;
        }
    }
}
