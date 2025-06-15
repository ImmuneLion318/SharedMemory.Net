using System.IO.MemoryMappedFiles;

namespace SharedMemory;

public class SharedOptions
{
    public string Name;
    public long Size;
    public MemoryMappedFileAccess Access;
    public bool AutoClear;
}