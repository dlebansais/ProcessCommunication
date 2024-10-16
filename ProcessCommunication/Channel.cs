namespace ProcessCommunication;

using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using Contracts;

/// <summary>
/// Represents a communication channel.
/// </summary>
/// <param name="guid">The channel guid.</param>
/// <param name="mode">The caller channel mode.</param>
public class Channel(Guid guid, Mode mode) : IDisposable
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
    public Mode Mode { get; } = mode;

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
                Mode.Receive => MemoryMappedFile.CreateNew(ChannelName, CapacityWithHeadTail, MemoryMappedFileAccess.ReadWrite),
                Mode.Send or _ => MemoryMappedFile.OpenExisting(ChannelName, MemoryMappedFileRights.ReadWrite),
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
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    public byte[]? Read()
    {
        if (Accessor is null)
            throw new InvalidOperationException();

        int EndOfBuffer = Capacity;
        Accessor.Read(EndOfBuffer, out int Head);
        Accessor.Read(EndOfBuffer + sizeof(int), out int Tail);

        byte[]? Result;

        if (Head > Tail)
        {
            int Length = Head - Tail;
            Result = new byte[Length];
            int Read = Accessor.ReadArray(Tail, Result, 0, Length);
            Debug.Assert(Read == Length);
        }
        else if (Head < Tail)
        {
            int Length = EndOfBuffer - Tail + Head;
            Result = new byte[Length];
            int Read = Accessor.ReadArray(Tail, Result, 0, EndOfBuffer - Tail);
            Read += Accessor.ReadArray(0, Result, EndOfBuffer - Tail, Head);
            Debug.Assert(Read == Length);
        }
        else
            return null;

        // Copy head to tail.
        Accessor.Write(EndOfBuffer + sizeof(int), Head);

        return Result;
    }

    /// <summary>
    /// Gets the number of free bytes in the channel.
    /// </summary>
    public int GetFreeLength()
    {
        if (Accessor is null)
            throw new InvalidOperationException();

        int EndOfBuffer = Capacity;
        Accessor.Read(EndOfBuffer, out int Head);
        Accessor.Read(EndOfBuffer + sizeof(int), out int Tail);

        return GetFreeLength(Head, Tail, Capacity);
    }

    private static int GetFreeLength(int head, int tail, int capacity)
    {
        if (tail > head)
            return tail - head - 1;
        else
            return capacity - head + tail - 1;
    }

    /// <summary>
    /// Gets the number of used bytes in the channel.
    /// </summary>
    public int GetUsedLength()
    {
        if (Accessor is null)
            throw new InvalidOperationException();

        int EndOfBuffer = Capacity;
        Accessor.Read(EndOfBuffer, out int Head);
        Accessor.Read(EndOfBuffer + sizeof(int), out int Tail);

        return GetUsedLength(Head, Tail, Capacity);
    }

    private static int GetUsedLength(int head, int tail, int capacity)
    {
        if (head >= tail)
            return head - tail;
        else
            return capacity - tail + head;
    }

    /// <summary>
    /// Writes data to the channel.
    /// </summary>
    /// <param name="data">The data to write.</param>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    public void Write(byte[] data)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(data);
#else
        if (data is null)
            throw new ArgumentNullException(nameof(data));
#endif

        if (Accessor is null)
            throw new InvalidOperationException();

        int EndOfBuffer = Capacity;
        Accessor.Read(EndOfBuffer, out int Head);
        Accessor.Read(EndOfBuffer + sizeof(int), out int Tail);

        int Length = data.Length;

        if (Tail > Head)
        {
            if (Length > Tail - Head - 1)
                throw new InvalidOperationException();

            Accessor.WriteArray(Head, data, 0, data.Length);
        }
        else
        {
            if (Length > EndOfBuffer - Head + Tail - 1)
                throw new InvalidOperationException();

            int FirstCopyLength = Math.Min(EndOfBuffer - Head, Length);
            int SecondCopyLength = Length - FirstCopyLength;

            Accessor.WriteArray(Head, data, 0, FirstCopyLength);
            Accessor.WriteArray(0, data, FirstCopyLength, SecondCopyLength);
        }

        Head += Length;
        if (Head >= EndOfBuffer)
            Head -= EndOfBuffer;

        // Copy the new head.
        Accessor.Write(EndOfBuffer, Head);
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

        int FreeLength = GetFreeLength(Head, Tail, Capacity);
        int UsedLength = GetUsedLength(Head, Tail, Capacity);
        return $"{channelName} - Head:{Head} Tail:{Tail} Capacity:{Capacity} Free:{FreeLength} Used:{UsedLength}";
    }

    /// <summary>
    /// Closes the chanel.
    /// </summary>
    public void Close()
    {
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
    private bool disposedValue;

    /// <summary>
    /// Optiuonally disposes of the instance.
    /// </summary>
    /// <param name="disposing">True if disposing must be done.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Close();
            }

            disposedValue = true;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}