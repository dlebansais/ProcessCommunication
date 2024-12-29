namespace ProcessCommunication;

using System;
using System.IO.MemoryMappedFiles;
using System.Threading;
using Contracts;

/// <summary>
/// Represents a communication channel.
/// </summary>
/// <param name="channelGuid">The channel guid.</param>
/// <param name="mode">The caller channel mode.</param>
public class Channel(Guid channelGuid, ChannelMode mode) : IChannel, IDisposable
{
    /// <summary>
    /// Gets a shared guid from client to server.
    /// </summary>
    public static Guid ClientToServerGuid { get; } = new Guid("{03C9C797-C924-415E-A6F9-9112AE75E56F}");

    /// <summary>
    /// Gets or sets the channel capacity.
    /// </summary>
    public static int Capacity { get; set; } = 0x100000;

    /// <inheritdoc cref="IChannel.ChannelGuid" />
    public Guid ChannelGuid { get; } = channelGuid;

    /// <inheritdoc cref="IChannel.Mode" />
    public ChannelMode Mode { get; } = mode;

    /// <inheritdoc cref="IChannel.Open" />
    public void Open()
    {
        if (IsOpen)
            throw new InvalidOperationException();

        Contract.Assert(Accessor is null);
        Contract.Assert(File is null);

        MemoryMappedFile? NewFile = null;
        EventWaitHandle? NewSharingHandle = null;
        MemoryMappedViewAccessor? NewAccessor = null;

        try
        {
            int CapacityWithHeadTail = EffectiveCapacity + (sizeof(int) * 2);
            string ChannelName = ChannelGuid.ToString("B") + "-Channel";
            string MutexName = ChannelGuid.ToString("B") + "-Mutex";

            if (Mode == ChannelMode.Receive)
            {
                NewFile = MemoryMappedFile.CreateNew(ChannelName, CapacityWithHeadTail, MemoryMappedFileAccess.ReadWrite);
                NewAccessor = NewFile.CreateViewAccessor();
                SetFileAndAccessor(NewFile, null, NewAccessor);
                return;
            }
            else
            {
                NewSharingHandle = new(false, EventResetMode.ManualReset, MutexName, out bool CreatedNew);
                if (CreatedNew)
                {
                    NewFile = MemoryMappedFile.OpenExisting(ChannelName, MemoryMappedFileRights.ReadWrite);
                    NewAccessor = NewFile.CreateViewAccessor();
                    SetFileAndAccessor(NewFile, NewSharingHandle, NewAccessor);
                    return;
                }
                else
                {
                    LastError = "Channel already opened";
                    NewSharingHandle.Dispose();
                    NewSharingHandle = null;
                }
            }
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
        }

        // Cleanup of any handle still open.
        SetFileAndAccessor(NewFile, NewSharingHandle, NewAccessor);
        SetFileAndAccessor(null, null, null);
    }

    /// <inheritdoc cref="IChannel.IsOpen" />
    public bool IsOpen => Accessor is not null;

    /// <inheritdoc cref="IChannel.LastError" />
    public string LastError { get; private set; } = string.Empty;

    /// <inheritdoc cref="IChannel.TryRead" />
    public bool TryRead(out byte[] data)
    {
        return Mode != ChannelMode.Receive || Accessor is null
            ? throw new InvalidOperationException()
            : CircularBufferHelper.Read(Accessor, EffectiveCapacity, out data);
    }

    /// <inheritdoc cref="IChannel.GetFreeLength" />
    public int GetFreeLength()
        => Accessor is null ? throw new InvalidOperationException() : CircularBufferHelper.GetFreeLength(Accessor, EffectiveCapacity);

    /// <inheritdoc cref="IChannel.GetUsedLength" />
    public int GetUsedLength()
        => Accessor is null ? throw new InvalidOperationException() : CircularBufferHelper.GetUsedLength(Accessor, EffectiveCapacity);

    /// <inheritdoc cref="IChannel.Write" />
    public void Write(byte[] data)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(data);
#else
        if (data is null)
            throw new ArgumentNullException(nameof(data));
#endif

        if (Mode == ChannelMode.Receive || Accessor is null)
            throw new InvalidOperationException();

        CircularBufferHelper.Write(Accessor, EffectiveCapacity, data);
    }

    /// <inheritdoc cref="IChannel.GetStats" />
    public string GetStats(string channelName)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(channelName);
#else
        if (channelName is null)
            throw new ArgumentNullException(nameof(channelName));
#endif

        if (Accessor is null)
            throw new InvalidOperationException();

        int EndOfBuffer = EffectiveCapacity;
        Accessor.Read(EndOfBuffer, out int Head);
        Accessor.Read(EndOfBuffer + sizeof(int), out int Tail);

        int FreeLength = CircularBufferHelper.GetFreeLength(Head, Tail, EffectiveCapacity);
        int UsedLength = CircularBufferHelper.GetUsedLength(Head, Tail, EffectiveCapacity);
        return $"{channelName} - Head:{Head} Tail:{Tail} Capacity:{EffectiveCapacity} Free:{FreeLength} Used:{UsedLength}";
    }

    /// <inheritdoc cref="IChannel.Close" />
    public void Close()
    {
        if (!IsOpen)
            return;

        SetFileAndAccessor(null, null, null);
    }

    private void SetFileAndAccessor(MemoryMappedFile? file, EventWaitHandle? sharingHandle, MemoryMappedViewAccessor? accessor)
    {
        Accessor?.Dispose();
        SharingHandle?.Dispose();
        File?.Dispose();

        File = file;
        SharingHandle = sharingHandle;
        Accessor = accessor;
    }

    private readonly int EffectiveCapacity = Capacity > 0 ? Capacity : 0x100;
    private MemoryMappedFile? File;
    private EventWaitHandle? SharingHandle;
    private MemoryMappedViewAccessor? Accessor;
    private bool DisposedValue;

    /// <summary>
    /// Disposes of managed and unmanaged resources.
    /// </summary>
    /// <param name="disposing"><see langword="True"/> if the method should dispose of resources; Otherwise, <see langword="false"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!DisposedValue)
        {
            if (disposing)
            {
                Close();
            }

            DisposedValue = true;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
