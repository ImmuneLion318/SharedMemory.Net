using System.Runtime.InteropServices;

namespace SharedMemory;

[StructLayout(LayoutKind.Sequential)]
public struct SharedObject
{
    public long Size;
}
