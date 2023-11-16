using System;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

namespace me.corruptionhades.memory {
	class MemoryHelper64 {
		#region Imports
		[DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, int bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, ulong lpBaseAddress, byte[] lpBuffer, int nSize, IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, ulong lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern Int32 CloseHandle(IntPtr hProcess);

        [DllImport("kernel32.dll")]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddres, int dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, int dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);
		#endregion

		private Process process;

		public MemoryHelper64(string processName) {
			process = Process.GetProcessesByName(processName).FirstOrDefault();
			if(process == null) {
				throw new Exception("Failed to find " + process + "!");
			}
		}

		public ulong GetBaseAddress(ulong startingAddress) {
            return (ulong) process.MainModule.BaseAddress.ToInt64() + startingAddress;
        }

        public ulong GetBaseAddress() {
            return (ulong) process.MainModule.BaseAddress.ToInt64();
        }

        public byte[] ReadMemoryBytes(ulong address, int bytes) {
            byte[] data = new byte[bytes];
            ReadProcessMemory(process.Handle, address, data, data.Length, IntPtr.Zero);
            return data;
        }

        public T ReadMemory<T>(ulong address) {
            byte[] data = ReadMemoryBytes(address, Marshal.SizeOf(typeof(T)));

            T t;
            GCHandle pinnedStruct = GCHandle.Alloc(data, GCHandleType.Pinned);
            try {
                t = (T) Marshal.PtrToStructure(pinnedStruct.AddrOfPinnedObject(), typeof(T)); 
            }
            catch (Exception ex) {
                throw ex; 
            }
            finally {
                pinnedStruct.Free(); 
            }

            return t;
        }

        public bool WriteMemory<T>(ulong address, T value) {
        	IntPtr bw = IntPtr.Zero;
            int sz = ObjectType.GetSize<T>();
            byte[] data = ObjectType.GetBytes<T>(value);

            // Change the page protection to writeable
            uint oldProtect;
            bool success = VirtualProtectEx(process.Handle, (IntPtr) address, (UIntPtr) sz, 0x40, out oldProtect);
            if (!success)  {
                return false;
            }

            // Write the new value
            success = WriteProcessMemory(process.Handle, address, data, sz, out bw);

            // Restore the original page protection
            success = VirtualProtectEx(process.Handle, (IntPtr) address, (UIntPtr) sz, oldProtect, out oldProtect);

            return success && bw != IntPtr.Zero;
        }

        public ulong OffsetCalculator(ulong baseAddress, int[] offsets) {
            var address = baseAddress;
            foreach (uint offset in offsets) {
                address = ReadMemory<ulong>(address) + offset;
            }
            return address;
        }

        public bool SetMemoryProtection(ulong memoryAddress, uint size, uint protection) {
            uint OldProtection;
            return VirtualProtectEx(process.Handle, new IntPtr((long) memoryAddress), new UIntPtr(size), protection, out OldProtection);
        }

        public void Close() {
            CloseHandle(process.Handle);
        }

        public ulong FindAddressFromSignature(string signature) {

            byte[] moduleBytes = new byte[process.MainModule.ModuleMemorySize];

            byte[] signatureBytes = ParseSignature(signature);
            int signatureLength = signatureBytes.Length;

            for (int i = 0; i < moduleBytes.Length - signatureLength; i++)
            {
                bool match = true;
                for (int j = 0; j < signatureLength; j++)
                {
                    if (signatureBytes[j] != 0x00 && signatureBytes[j] != moduleBytes[i + j])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    ulong address = (ulong)process.MainModule.BaseAddress + (ulong)i;
                    return address;
                }
            }

             throw new InvalidOperationException("Failed to find signature.");
        }

        private byte[] ParseSignature(string signature)
        {
            signature = signature.Replace(" ", "");
            byte[] signatureBytes = new byte[signature.Length / 2];
            for (int i = 0; i < signature.Length; i += 2)
            {
                string byteString = signature.Substring(i, 2);
                if (byteString == "??" || byteString == "**")
                {
                    signatureBytes[i / 2] = 0x00;
                }
                else
                {
                    signatureBytes[i / 2] = Convert.ToByte(byteString, 16);
                }
            }
            return signatureBytes;
        }
	}

	public static class ObjectType {
        public static int GetSize<T>() {
            return Marshal.SizeOf(typeof(T));
        }

        public static byte[] GetBytes<T>(T Value) {
            string typename = typeof(T).ToString();
            switch (typename) {
                case "System.Single":
                    return BitConverter.GetBytes((float) Convert.ChangeType(Value, typeof(float)));
                case "System.Int32":
                    return BitConverter.GetBytes((int) Convert.ChangeType(Value, typeof(int)));
                case "System.Int64":
                    return BitConverter.GetBytes((long) Convert.ChangeType(Value, typeof(long)));
                case "System.Double":
                    return BitConverter.GetBytes((double) Convert.ChangeType(Value, typeof(double)));
                case "System.Byte":
                    return BitConverter.GetBytes((byte) Convert.ChangeType(Value, typeof(byte)));
                case "System.String":
                    return Encoding.Unicode.GetBytes((string) Convert.ChangeType(Value, typeof(string)));
                default:
                    return new byte[0];
            }
        }
    }
}