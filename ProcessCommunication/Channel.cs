namespace ProcessCommunication;

using System;
using System.IO.MemoryMappedFiles;
using Contracts;

/// <summary>
/// Represents a communication channel.
/// </summary>
/// <param name="guid">The channel guid.</param>
/// <param name="mode">The caller channel mode.</param>
public class Channel(Guid guid, ChannelMode mode) : IDisposable
{
    /// <summary>
    /// Gets a shared guid from client to server.
    /// </summary>
    public static Guid ClientToServerGuid { get; } = new Guid("{03C9C797-C924-415E-A6F9-9112AE75E56F}");

    /// <summary>
    /// Gets or sets the channel capacity.
    /// </summary>
    public static int Capacity { get; set; } = 0x100000;

    /// <summary>
    /// Gets the channel guid.
    /// </summary>
    public Guid Guid { get; } = guid;

    /// <summary>
    /// Gets the caller channel mode.
    /// </summary>
    public ChannelMode Mode { get; } = mode;

    /// <summary>
    /// Opens the channel.
    /// </summary>
    public void Open()
    {
        if (IsOpen)
            throw new InvalidOperationException();

        Contract.Assert(Accessor is null);
        Contract.Assert(File is null);

        try
        {
            int CapacityWithHeadTail = Capacity + (sizeof(int) * 2);
            string ChannelName = Guid.ToString("B");

            MemoryMappedFile NewFile = Mode switch
            {
                ChannelMode.Receive => MemoryMappedFile.CreateNew(ChannelName, CapacityWithHeadTail, MemoryMappedFileAccess.ReadWrite),
                ChannelMode.Send or _ => MemoryMappedFile.OpenExisting(ChannelName, MemoryMappedFileRights.ReadWrite),
            };

            MemoryMappedViewAccessor NewAccessor = NewFile.CreateViewAccessor();

            SetFileAndAccessor(NewFile, NewAccessor);
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the channel is open.
    /// </summary>
    public bool IsOpen { get => Accessor is not null; }

    /// <summary>
    /// Gets the last error.
    /// </summary>
    public string LastError { get; private set; } = string.Empty;

    /// <summary>
    /// Reads data from the channel.
    /// </summary>
    /// <param name="data">The data read upon return if successful.</param>
    /// <returns><see langword="true"/> data has been read; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    public bool TryRead(out byte[] data)
    {
        if (Accessor is null || Mode != ChannelMode.Receive)
            throw new InvalidOperationException();

        return CircularBufferHelper.Read(Accessor, Capacity, out data);
    }

    /// <summary>
    /// Gets the number of free bytes in the channel.
    /// </summary>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    public int GetFreeLength()
    {
        if (Accessor is null)
            throw new InvalidOperationException();

        return CircularBufferHelper.GetFreeLength(Accessor, Capacity);
    }

    /// <summary>
    /// Gets the number of used bytes in the channel.
    /// </summary>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    public int GetUsedLength()
    {
        if (Accessor is null)
            throw new InvalidOperationException();

        return CircularBufferHelper.GetUsedLength(Accessor, Capacity);
    }

    /// <summary>
    /// Writes data to the channel.
    /// </summary>
    /// <param name="data">The data to write.</param>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    public void Write(byte[] data)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(data);
#else
        if (data is null)
            throw new ArgumentNullException(nameof(data));
#endif

        if (Accessor is null || Mode == ChannelMode.Receive)
            throw new InvalidOperationException();

        CircularBufferHelper.Write(Accessor, Capacity, data);
    }

    /// <summary>
    /// Gets channel stats for debug purpose.
    /// </summary>
    /// <param name="channelName">The channel name.</param>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    public string GetStats(string channelName)
    {
        if (Accessor is null)
            throw new InvalidOperationException();

        int EndOfBuffer = Capacity;
        Accessor.Read(EndOfBuffer, out int Head);
        Accessor.Read(EndOfBuffer + sizeof(int), out int Tail);

        int FreeLength = CircularBufferHelper.GetFreeLength(Head, Tail, Capacity);
        int UsedLength = CircularBufferHelper.GetUsedLength(Head, Tail, Capacity);
        return $"{channelName} - Head:{Head} Tail:{Tail} Capacity:{Capacity} Free:{FreeLength} Used:{UsedLength}";
    }

    /// <summary>
    /// Closes the channel.
    /// </summary>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    public void Close()
    {
        if (!IsOpen)
            return;

        SetFileAndAccessor(null, null);
    }

    private void SetFileAndAccessor(MemoryMappedFile? file, MemoryMappedViewAccessor? accessor)
    {
        Accessor?.Dispose();
        File?.Dispose();

        File = file;
        Accessor = accessor;
    }

    private MemoryMappedFile? File;
    private MemoryMappedViewAccessor? Accessor;
    private bool DisposedValue;

    /// <summary>
    /// Optiuonally disposes of the instance.
    /// </summary>
    /// <param name="disposing">True if disposing must be done.</param>
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

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
