using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;

namespace SharedMemory;

public abstract class SharedBase : IDisposable
{
    public SharedOptions Options { get; private set; }
    public string Name { get; private set; }
    public long Size { get; private set; }
    public bool IsOwnerOfSharedObject { get; private set; }
    public SharedObject SharedObject { get; private set; }

    public virtual long SharedMemorySize => this.HeaderOffset + Marshal.SizeOf(typeof(SharedObject)) + this.Size;

    protected virtual long HeaderOffset => 0;
    protected virtual long BufferOffset => this.HeaderOffset + Marshal.SizeOf(typeof(SharedObject));

    protected MemoryMappedFile File;
    protected MemoryMappedViewAccessor View;
    protected Thread Listener;

    public delegate void OnMessageReceivedHandler(byte[] Data, SharedBase Sender);
    public event OnMessageReceivedHandler OnMessageReceived;
    protected EventWaitHandle MessageEvent;

    protected SharedBase(SharedOptions Options, bool OwnsSharedObject)
    {
        this.Options = Options;
        this.Name = Options.Name;

        this.IsOwnerOfSharedObject = OwnsSharedObject;

        if (this.IsOwnerOfSharedObject is true)
            this.Size = Options.Size;

        if (this.IsOwnerOfSharedObject)
        {
            this.File = MemoryMappedFile.CreateNew(this.Name, this.SharedMemorySize);
            this.View = this.File.CreateViewAccessor(0, this.SharedMemorySize, Options.Access);

            SharedObject Object = new SharedObject
            {
                Size = this.SharedMemorySize
            };

            this.SharedObject = Object;
            this.View.Write(this.HeaderOffset, ref Object);
        }
        else
        {
            this.File = MemoryMappedFile.OpenExisting(this.Name);

            using (MemoryMappedViewAccessor View = this.File.CreateViewAccessor(0, this.BufferOffset, Options.Access))
            {
                View.Read(this.HeaderOffset, out SharedObject Object);
                this.SharedObject = Object;
            }

            this.Size = this.SharedObject.Size - Marshal.SizeOf(typeof(SharedObject));
            this.View = this.File.CreateViewAccessor(0, this.SharedMemorySize, Options.Access);
        }

        this.MessageEvent = new EventWaitHandle(false, EventResetMode.AutoReset, $"{Options.Name}-Event");
        this.Listener = new Thread(() =>
        {
            while (true)
            {
                this.MessageEvent.WaitOne();

                int Length = this.View.ReadInt32(this.BufferOffset);

                if (Length > 0)
                {
                    byte[] Buffer = new byte[Length];

                    if (Options.AutoClear)
                        View.Write(this.BufferOffset, 0);

                    this.View.ReadArray(this.BufferOffset + 4, Buffer, 0, Length);
                    OnMessageReceived?.Invoke(Buffer, this);
                }
            }
        })
        {
            IsBackground = true
        };
        this.Listener.Start();
    }

    ~SharedBase() => this.Dispose();

    public void Write(byte[] Data)
    {
        if (Data.Length > Options.Size)
            throw new Exception("Buffer Size Exceeds Shared Memory Size Limitations.");

        this.View.Write(this.BufferOffset, Data.Length);
        this.View.WriteArray(this.BufferOffset + 4, Data, 0, Data.Length);
        this.MessageEvent.Set();
    }

    public void Dispose()
    {
        this.View?.Dispose();
        this.File?.Dispose();

        this.SharedObject = default;
        this.View = null;
        this.File = null;

        GC.SuppressFinalize(this);
    }
}