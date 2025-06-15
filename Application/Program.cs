using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharedMemory;

namespace SharedMemory;

public class Program
{
    public static SharedOptions Options = new SharedOptions
    {
        Name = "SharedIpc",
        Size = 1024 * 1024 * 1,
        Access = MemoryMappedFileAccess.ReadWrite,
        AutoClear = false,
    };

    public static SharedServer Server = new SharedServer(Options);
    public static SharedClient Client = new SharedClient(Options);

    public static void Main(string[] Parameters)
    {
        Console.WriteLine($"Server: {Server.Name}\n" +
            $"  Size : {Server.Size}\n" +
            $"  Total Size : {Server.SharedMemorySize}\n" +
            $"  Is Object Owner : {Server.IsOwnerOfSharedObject}");

        Server.OnMessageReceived += (Data, Sender) =>
        {
            Console.WriteLine("\nBytes Received: " + Data.Length);
            Console.WriteLine($"\nServer Received: {Encoding.UTF8.GetString(Data)}");
        };

        Client.Write(Encoding.UTF8.GetBytes("Hello from Client!"));

        Thread.Sleep(-1);
    }
}
