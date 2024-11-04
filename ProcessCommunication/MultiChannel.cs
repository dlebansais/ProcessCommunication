namespace ProcessCommunication;

using System;
using System.Globalization;
using System.IO.MemoryMappedFiles;
using System.Threading;
using Contracts;

/// <summary>
/// Represents a communication channel.
/// </summary>
/// <param name="guid">The channel guid.</param>
/// <param name="mode">The caller channel mode.</param>
/// <param name="channelCount">The channel count. If 0 or less, 1 is assumed.</param>
public class MultiChannel(Guid guid, ChannelMode mode, int channelCount) : IDisposable
{
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
    /// Gets the channel count. If 0 or less, 1 is assumed.
    /// </summary>
    public int ChannelCount { get; } = channelCount;

    private int EffectiveChannelCount => ChannelCount > 0 ? ChannelCount : 1;

    /// <summary>
    /// Opens the channel.
    /// </summary>
    public void Open()
    {
        if (IsOpen)
            throw new InvalidOperationException();

        Contract.Assert(LockFile is null);
        Contract.Assert(LockAccessor is null);
        Contract.Assert(Array.TrueForAll(DataFiles, item => item is null));
        Contract.Assert(Array.TrueForAll(DataAccessors, item => item is null));
        Contract.Assert(SelectedFile is null);
        Contract.Assert(SelectedAccessor is null);

        if (Mode == ChannelMode.Receive)
        {
            if (!OpenReceivingDataChannels() || !OpenReceivingLockChannel())
                CloseReceivingDataChannels();
        }
        else
        {
            if (!OpenSendingLockChannel() || !OpenSendingDataChannel())
                CloseSendingLockChannel();
        }
    }

    private bool OpenReceivingDataChannels()
    {
        for (int i = 0; i < EffectiveChannelCount; i++)
        {
            try
            {
                int CapacityWithHeadTail = Capacity + (sizeof(int) * 2);
                string ChannelName = DataMapName(i);

                MemoryMappedFile NewFile = MemoryMappedFile.CreateNew(ChannelName, CapacityWithHeadTail, MemoryMappedFileAccess.ReadWrite);
                MemoryMappedViewAccessor NewAccessor = NewFile.CreateViewAccessor();

                SetDataFileAndAccessor(i, NewFile, NewAccessor);
            }
            catch (Exception exception)
            {
                LastError = exception.Message;
                return false;
            }
        }

        return true;
    }

    private bool OpenReceivingLockChannel()
    {
        unsafe
        {
            try
            {
                string ChannelName = LockMapName;
                MemoryMappedFile NewFile = MemoryMappedFile.CreateNew(ChannelName, EffectiveChannelCount * sizeof(IntPtr), MemoryMappedFileAccess.ReadWrite);
                MemoryMappedViewAccessor NewAccessor = NewFile.CreateViewAccessor();

                SetLockFileAndAccessor(NewFile, NewAccessor);
            }
            catch (Exception exception)
            {
                LastError = exception.Message;
                return false;
            }
        }

        return true;
    }

    private void CloseReceivingDataChannels()
    {
        for (int i = 0; i < EffectiveChannelCount; i++)
            SetDataFileAndAccessor(i, null, null);
    }

    private void CloseReceivingLockChannel()
    {
        SetLockFileAndAccessor(null, null);
    }

    private bool OpenSendingLockChannel()
    {
        try
        {
            string ChannelName = LockMapName;
            MemoryMappedFile NewFile = MemoryMappedFile.OpenExisting(ChannelName, MemoryMappedFileRights.ReadWrite);
            MemoryMappedViewAccessor NewAccessor = NewFile.CreateViewAccessor();

            SetLockFileAndAccessor(NewFile, NewAccessor);
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            return false;
        }

        return true;
    }

    private bool OpenSendingDataChannel()
    {
        MemoryMappedViewAccessor Accessor = Contract.AssertNotNull(LockAccessor);

        unsafe
        {
            int AvailableDataChannels = (int)(Accessor.Capacity / sizeof(IntPtr));
            byte* SafeMemoryPointer = default;
            Accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref SafeMemoryPointer);
            IntPtr BasePointer = (IntPtr)SafeMemoryPointer;
            IntPtr Comparand = IntPtr.Zero;
            IntPtr Value = IntPtr.Add(Comparand, 1);

            for (int i = 0; i < AvailableDataChannels; i++)
            {
                IntPtr Pointer = IntPtr.Add(BasePointer, sizeof(IntPtr) * i);
                IntPtr OriginalValue = Interlocked.CompareExchange(ref Pointer, Value, Comparand);

                if (OriginalValue == Comparand)
                {
                    try
                    {
                        string ChannelName = DataMapName(i);

                        MemoryMappedFile NewFile = MemoryMappedFile.OpenExisting(ChannelName, MemoryMappedFileRights.ReadWrite);
                        MemoryMappedViewAccessor NewAccessor = NewFile.CreateViewAccessor();

                        SetDataFileAndAccessor(i, NewFile, NewAccessor);
                        return true;
                    }
                    catch (Exception exception)
                    {
                        LastError = exception.Message;
                        return false;
                    }
                }
            }

            LastError = $"Out of slots (Tried {AvailableDataChannels})";
            return false;
        }
    }

    private void CloseSendingLockChannel()
    {
        SetLockFileAndAccessor(null, null);
    }

    private void CloseSendingDataChannel()
    {
        MemoryMappedViewAccessor Accessor = Contract.AssertNotNull(LockAccessor);

        unsafe
        {
            byte* SafeMemoryPointer = default;
            Accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref SafeMemoryPointer);
            IntPtr Pointer = (IntPtr)SafeMemoryPointer;
            IntPtr Value = IntPtr.Zero;
            _ = Interlocked.Exchange(ref Pointer, Value);
        }

        SetDataFileAndAccessor(SendingIndex, null, null);
    }

    /// <summary>
    /// Gets a value indicating whether the channel is open.
    /// </summary>
    public bool IsOpen => LockAccessor is not null;

    /// <summary>
    /// Gets a value indicating whether the specified channel is connected.
    /// </summary>
    /// <param name="index">The channel index.</param>
    public bool IsConnected(int index) => DataAccessors[index] is not null;

    /// <summary>
    /// Gets the last error.
    /// </summary>
    public string LastError { get; private set; } = string.Empty;

    /// <summary>
    /// Reads data from the channel.
    /// </summary>
    /// <param name="data">The data read upon return if successful.</param>
    /// <param name="index">Index of the channel with data upon return.</param>
    /// <returns><see langword="true"/> data has been read; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    public bool TryRead(out byte[] data, out int index)
    {
        if (LockAccessor is null || Mode != ChannelMode.Receive)
            throw new InvalidOperationException();

        int StartingIndex = ReceivingIndex++;

        do
        {
            if (ReceivingIndex >= EffectiveChannelCount)
                ReceivingIndex = 0;

            if (DataAccessors[ReceivingIndex] is MemoryMappedViewAccessor ConnectedAccessor)
            {
                if (CircularBufferHelper.Read(ConnectedAccessor, Capacity, out data))
                {
                    index = ReceivingIndex;
                    return true;
                }
            }
        }
        while (StartingIndex != ReceivingIndex);

        Contract.Unused(out data);
        index = 0;

        // Contract.Unused(out index);
        return false;
    }

    /// <summary>
    /// Gets the number of free bytes in the channel.
    /// This method requires <see cref="Mode"/> to be <see cref="ChannelMode.Send"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    public int GetFreeLength()
    {
        if (SelectedAccessor is null || Mode == ChannelMode.Receive)
            throw new InvalidOperationException();

        return CircularBufferHelper.GetFreeLength(SelectedAccessor, Capacity);
    }

    /// <summary>
    /// Gets the number of free bytes in the specified channel.
    /// This method requires <see cref="Mode"/> to be <see cref="ChannelMode.Receive"/>.
    /// </summary>
    /// <param name="index">The channel index.</param>
    /// <exception cref="InvalidOperationException">The specified channel is not connected.</exception>
    public int GetFreeLength(int index)
    {
        MemoryMappedViewAccessor? Accessor = DataAccessors[index];
        if (Accessor is null || Mode != ChannelMode.Receive)
            throw new InvalidOperationException();

        return CircularBufferHelper.GetFreeLength(Accessor, Capacity);
    }

    /// <summary>
    /// Gets the number of used bytes in the channel.
    /// This method requires <see cref="Mode"/> to be <see cref="ChannelMode.Send"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    public int GetUsedLength()
    {
        if (SelectedAccessor is null || Mode == ChannelMode.Receive)
            throw new InvalidOperationException();

        return CircularBufferHelper.GetUsedLength(SelectedAccessor, Capacity);
    }

    /// <summary>
    /// Gets the number of used bytes in the channel.
    /// This method requires <see cref="Mode"/> to be <see cref="ChannelMode.Receive"/>.
    /// </summary>
    /// <param name="index">The channel index.</param>
    /// <exception cref="InvalidOperationException">The specified channel is not connected.</exception>
    public int GetUsedLength(int index)
    {
        MemoryMappedViewAccessor? Accessor = DataAccessors[index];
        if (Accessor is null || Mode != ChannelMode.Receive)
            throw new InvalidOperationException();

        return CircularBufferHelper.GetUsedLength(Accessor, Capacity);
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

        if (SelectedAccessor is null || Mode == ChannelMode.Receive)
            throw new InvalidOperationException();

        CircularBufferHelper.Write(SelectedAccessor, Capacity, data);
    }

    /// <summary>
    /// Gets channel stats for debug purpose.
    /// </summary>
    /// <param name="channelName">The channel name.</param>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    public string GetStats(string channelName)
    {
        if (SelectedAccessor is null)
            throw new InvalidOperationException();

        int EndOfBuffer = Capacity;
        SelectedAccessor.Read(EndOfBuffer, out int Head);
        SelectedAccessor.Read(EndOfBuffer + sizeof(int), out int Tail);

        int FreeLength = CircularBufferHelper.GetFreeLength(Head, Tail, Capacity);
        int UsedLength = CircularBufferHelper.GetUsedLength(Head, Tail, Capacity);
        return $"{channelName} - Head:{Head} Tail:{Tail} Capacity:{Capacity} Free:{FreeLength} Used:{UsedLength}";
    }

    /// <summary>
    /// Gets channel stats for debug purpose for the specified channel.
    /// </summary>
    /// <param name="channelName">The channel name.</param>
    /// <param name="index">The channel index.</param>
    /// <exception cref="InvalidOperationException">The channel is not connected.</exception>
    public string GetStats(string channelName, int index)
    {
        MemoryMappedViewAccessor? Accessor = DataAccessors[index];
        if (Accessor is null || Mode != ChannelMode.Receive)
            throw new InvalidOperationException();

        int EndOfBuffer = Capacity;
        Accessor.Read(EndOfBuffer, out int Head);
        Accessor.Read(EndOfBuffer + sizeof(int), out int Tail);

        int FreeLength = CircularBufferHelper.GetFreeLength(Head, Tail, Capacity);
        int UsedLength = CircularBufferHelper.GetUsedLength(Head, Tail, Capacity);
        return $"{channelName} - Head:{Head} Tail:{Tail} Capacity:{Capacity} Free:{FreeLength} Used:{UsedLength}";
    }

    /// <summary>
    /// Closes the chanel.
    /// </summary>
    public void Close()
    {
        if (!IsOpen)
            return;

        if (Mode == ChannelMode.Receive)
        {
            CloseReceivingLockChannel();
            CloseReceivingDataChannels();
        }
        else
        {
            CloseSendingDataChannel();
            CloseSendingLockChannel();
        }
    }

    private void SetLockFileAndAccessor(MemoryMappedFile? file, MemoryMappedViewAccessor? accessor)
    {
        LockAccessor?.Dispose();
        LockFile?.Dispose();

        LockFile = file;
        LockAccessor = accessor;
    }

    private void SetDataFileAndAccessor(int index, MemoryMappedFile? file, MemoryMappedViewAccessor? accessor)
    {
        Contract.Require(index >= 0);
        Contract.Require(index < EffectiveChannelCount);

        DataAccessors[index]?.Dispose();
        DataFiles[index]?.Dispose();

        DataFiles[index] = file;
        DataAccessors[index] = accessor;

        SendingIndex = Mode == ChannelMode.Receive || accessor is null ? -1 : index;
    }

    private string LockMapName => Guid.ToString("B");
    private string DataMapName(int index) => Guid.ToString("B") + "-" + index.ToString(CultureInfo.InvariantCulture);

    private MemoryMappedFile? LockFile;
    private MemoryMappedViewAccessor? LockAccessor;
    private readonly MemoryMappedFile?[] DataFiles = new MemoryMappedFile?[channelCount > 0 ? channelCount : 1];
    private readonly MemoryMappedViewAccessor?[] DataAccessors = new MemoryMappedViewAccessor?[channelCount > 0 ? channelCount : 1];
    private int SendingIndex = -1;
    private MemoryMappedFile? SelectedFile => DataFiles[SendingIndex];
    private MemoryMappedViewAccessor? SelectedAccessor => DataAccessors[SendingIndex];
    private int ReceivingIndex;
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