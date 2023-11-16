using me.corruptionhades.memory;

class MemoryUtil {
    
    private static MemoryHelper64 helper;

    public static void init(string process) {
        helper = new MemoryHelper64(process);
    }

    public static void writeBytes(ulong address, string bytes) {
        string[] split = bytes.Split(' ');
        foreach(string s in split) {
            if(s.Length != 2) {
                throw new Exception("Invalid byte: " + s);
            }
            // convert the string to a byte (remember that the string is in hex)
            byte b = byte.Parse(s, System.Globalization.NumberStyles.HexNumber);
            helper.WriteMemory<byte>(address, b);
            address++;
        }
    }

    public static void WriteMemory<T>(ulong address, T value) {
        helper.WriteMemory<T>(address, value);
    }

    public static ulong GetBaseAddress() {
        return (ulong) helper.GetBaseAddress();
    }

    public static ulong GetBaseAddress(ulong startAddress) {
        return (ulong) helper.GetBaseAddress(startAddress);
    }

    public static T ReadMemory<T>(ulong address) {
        return helper.ReadMemory<T>(address);
    }

    public static ulong GetStaticAddr(string add) {
        return (ulong) helper.GetBaseAddress(Convert.ToUInt64(add, 16));
    }

    public static ulong CalcOffsets2(ulong address, int[] offsets) {
        ulong process = helper.GetBaseAddress(address);
        return helper.OffsetCalculator(process, offsets);
    }

     public static ulong CalcOffsets(ulong address, int[] offsets) {
        return helper.OffsetCalculator(address, offsets);
    }

    public static ulong CalcOffsets(ulong address, string offsets) {
        return CalcOffsets(address, offsets, false);
    }

    public static ulong CalcOffsets(ulong address, string offsets, bool reverse) {
        string[] split = offsets.Split(' ');
        if(reverse) {
            Array.Reverse(split);
        }

        int[] offsetsInt = new int[split.Length];
        foreach(string s in split) {
            offsetsInt[Array.IndexOf(split, s)] = Convert.ToInt32(s, 16);
        }

        ulong process = helper.GetBaseAddress(address);
        return helper.OffsetCalculator(process, offsetsInt);
    }

    public static int[] toIntArray(string s) {
        string[] split = s.Split(' ');
        int[] offsetsInt = new int[split.Length];
        foreach(string s2 in split) {
            offsetsInt[Array.IndexOf(split, s2)] = Convert.ToInt32(s2, 16);
        }
        return offsetsInt;
    }

    public static void Close() {
        helper.Close();
    }
}