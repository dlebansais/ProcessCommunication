namespace ProcessCommunication;

using System;
using System.Globalization;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Threading;
using Contracts;

/// <summary>
/// Represents a communication channel.
/// </summary>
/// <param name="guid">The channel guid.</param>
/// <param name="mode">The caller channel mode.</param>
/// <param name="channelCount">The channel count. If 0 or less, 1 is assumed.</param>
public class MultiChannel(Guid guid, ChannelMode mode, int channelCount) : IMultiChannel, IDisposable
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
    /// Gets the channel count. If 0 or less, 1 is assumed (see <see cref="EffectiveChannelCount"/>).
    /// </summary>
    public int ChannelCount { get; } = channelCount;

    /// <summary>
    /// Gets the effective channel count.
    /// </summary>
    public int EffectiveChannelCount { get; } = channelCount > 0 ? channelCount : 1;

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
        Contract.Assert(SendingIndex == -1);

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
                int CapacityWithHeadTail = EffectiveCapacity + (sizeof(int) * 2);
                string ChannelName = DataMapName(i);

                MemoryMappedFile NewFile = MemoryMappedFile.CreateNew(ChannelName, CapacityWithHeadTail, MemoryMappedFileAccess.ReadWrite);
                MemoryMappedViewAccessor NewAccessor = NewFile.CreateViewAccessor();

                SetDataFileAndAccessor(i, NewFile, null, NewAccessor);
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
                MemoryMappedFile NewFile = MemoryMappedFile.CreateNew(ChannelName, EffectiveChannelCount * sizeof(int), MemoryMappedFileAccess.ReadWrite);
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
            SetDataFileAndAccessor(i, null, null, null);
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
            byte* SafeMemoryPointer = default;
            Accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref SafeMemoryPointer);
            void* BasePointer = SafeMemoryPointer;
            const int Comparand = 0;
            const int Value = 1;
            int FreeChannel = -1;

            for (int i = 0; i < EffectiveChannelCount; i++)
            {
                void* Pointer = Unsafe.Add<int>(BasePointer, i);
                ref int Location = ref Unsafe.AsRef<int>(Pointer);
                int OriginalValue = Interlocked.CompareExchange(ref Location, Value, Comparand);

                // Lock the channel for us. If we can't open it, at leats the next time we'll try another one.
                // If we don't find any unlocked channel, but still succeed at opening one, it'll be unlocked on close.
                if (OriginalValue == Comparand)
                {
                    FreeChannel = i;
                    break;
                }
            }

            // Try to open all channels until we get a free one.
            for (int i = 0; i < EffectiveChannelCount; i++)
            {
                // If there is a free channel, start from that one, otherwise start from 0.
                int Index = FreeChannel >= 0 ? (FreeChannel + i) % EffectiveChannelCount : i;

                if (TryOpenSendingDataChannel(Index))
                    return true;
            }

            LastError = $"Out of slots (Tried {EffectiveChannelCount})";
            return false;
        }
    }

    private bool TryOpenSendingDataChannel(int index)
    {
        string ChannelName = DataMapName(index);
        string MutexName = DataMutexName(index);

        MemoryMappedFile? NewFile = null;
        EventWaitHandle? NewSharingHandle = null;
        MemoryMappedViewAccessor? NewAccessor = null;

        try
        {
            NewSharingHandle = new(false, EventResetMode.ManualReset, MutexName, out bool CreatedNew);
            if (CreatedNew)
            {
                NewFile = MemoryMappedFile.OpenExisting(ChannelName, MemoryMappedFileRights.ReadWrite);
                NewAccessor = NewFile.CreateViewAccessor();
                SetDataFileAndAccessor(index, NewFile, NewSharingHandle, NewAccessor);
                return true;
            }
            else
            {
                NewSharingHandle.Dispose();
                NewSharingHandle = null;
            }
        }
        catch
        {
        }

        // Cleanup of any handle still open.
        SetDataFileAndAccessor(index, NewFile, NewSharingHandle, NewAccessor);
        SetDataFileAndAccessor(index, null, null, null);

        return false;
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
            void* BasePointer = SafeMemoryPointer;

            const int Value = 0;
            void* Pointer = Unsafe.Add<int>(BasePointer, SendingIndex);
            ref int Location = ref Unsafe.AsRef<int>(Pointer);
            _ = Interlocked.Exchange(ref Location, Value);
        }

        SetDataFileAndAccessor(SendingIndex, null, null, null);
    }

    /// <summary>
    /// Gets a value indicating whether the channel is open.
    /// </summary>
    public bool IsOpen => LockAccessor is not null;

    /// <summary>
    /// Gets a value indicating whether the specified channel is connected.
    /// </summary>
    /// <param name="index">The channel index.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index.</exception>
    public bool IsConnected(int index)
    {
        if (index < 0 || index >= EffectiveChannelCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        return DataAccessors[index] is not null;
    }

    /// <summary>
    /// Gets the last error.
    /// </summary>
    public string LastError { get; private set; } = string.Empty;

    /// <summary>
    /// Reads data from the channel.
    /// This method requires <see cref="Mode"/> to be <see cref="ChannelMode.Receive"/>.
    /// </summary>
    /// <param name="data">The data read upon return if successful.</param>
    /// <param name="index">Index of the channel with data upon return.</param>
    /// <returns><see langword="true"/> data has been read; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    public bool TryRead(out byte[] data, out int index)
    {
        if (Mode != ChannelMode.Receive || LockAccessor is null)
            throw new InvalidOperationException();

        int StartingIndex = ReceivingIndex++;

        do
        {
            if (ReceivingIndex >= EffectiveChannelCount)
                ReceivingIndex = 0;

            if (DataAccessors[ReceivingIndex] is MemoryMappedViewAccessor ConnectedAccessor)
            {
                if (CircularBufferHelper.Read(ConnectedAccessor, EffectiveCapacity, out data))
                {
                    index = ReceivingIndex;
                    return true;
                }
            }
        }
        while (StartingIndex != ReceivingIndex);

        Contract.Unused(out data);
        Contract.Unused(out index);
        return false;
    }

    /// <summary>
    /// Gets the number of free bytes in the channel.
    /// This method requires <see cref="Mode"/> to be <see cref="ChannelMode.Send"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    public int GetFreeLength()
    {
        if (Mode == ChannelMode.Receive || SendingIndex < 0)
            throw new InvalidOperationException();

        MemoryMappedViewAccessor Accessor = Contract.AssertNotNull(SelectedAccessor);

        return CircularBufferHelper.GetFreeLength(Accessor, EffectiveCapacity);
    }

    /// <summary>
    /// Gets the number of free bytes in the specified channel.
    /// </summary>
    /// <param name="index">The channel index.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index.</exception>
    /// <exception cref="InvalidOperationException">The specified channel is not connected.</exception>
    public int GetFreeLength(int index)
    {
        if (index < 0 || index >= EffectiveChannelCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        MemoryMappedViewAccessor Accessor = DataAccessors[index] ?? throw new InvalidOperationException();

        return CircularBufferHelper.GetFreeLength(Accessor, EffectiveCapacity);
    }

    /// <summary>
    /// Gets the number of used bytes in the channel.
    /// This method requires <see cref="Mode"/> to be <see cref="ChannelMode.Send"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    public int GetUsedLength()
    {
        if (Mode == ChannelMode.Receive || SendingIndex < 0)
            throw new InvalidOperationException();

        MemoryMappedViewAccessor Accessor = Contract.AssertNotNull(SelectedAccessor);

        return CircularBufferHelper.GetUsedLength(Accessor, EffectiveCapacity);
    }

    /// <summary>
    /// Gets the number of used bytes in the channel.
    /// </summary>
    /// <param name="index">The channel index.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index.</exception>
    /// <exception cref="InvalidOperationException">The specified channel is not connected.</exception>
    public int GetUsedLength(int index)
    {
        if (index < 0 || index >= EffectiveChannelCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        MemoryMappedViewAccessor Accessor = DataAccessors[index] ?? throw new InvalidOperationException();

        return CircularBufferHelper.GetUsedLength(Accessor, EffectiveCapacity);
    }

    /// <summary>
    /// Writes data to the channel.
    /// This method requires <see cref="Mode"/> to be <see cref="ChannelMode.Send"/>.
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

        if (Mode == ChannelMode.Receive || SendingIndex < 0)
            throw new InvalidOperationException();

        MemoryMappedViewAccessor Accessor = Contract.AssertNotNull(SelectedAccessor);

        CircularBufferHelper.Write(Accessor, EffectiveCapacity, data);
    }

    /// <summary>
    /// Gets channel stats for debug purpose.
    /// This method requires <see cref="Mode"/> to be <see cref="ChannelMode.Send"/>.
    /// </summary>
    /// <param name="channelName">The channel name.</param>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    public string GetStats(string channelName)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(channelName);
#else
        if (channelName is null)
            throw new ArgumentNullException(nameof(channelName));
#endif

        if (Mode == ChannelMode.Receive || SendingIndex < 0)
            throw new InvalidOperationException();

        MemoryMappedViewAccessor Accessor = Contract.AssertNotNull(SelectedAccessor);

        return GetStats(channelName, Accessor);
    }

    /// <summary>
    /// Gets channel stats for debug purpose for the specified channel.
    /// </summary>
    /// <param name="channelName">The channel name.</param>
    /// <param name="index">The channel index.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index.</exception>
    /// <exception cref="InvalidOperationException">The channel is not connected.</exception>
    public string GetStats(string channelName, int index)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(channelName);
#else
        if (channelName is null)
            throw new ArgumentNullException(nameof(channelName));
#endif

        if (index < 0 || index >= EffectiveChannelCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        MemoryMappedViewAccessor Accessor = DataAccessors[index] ?? throw new InvalidOperationException();

        return GetStats(channelName, Accessor);
    }

    private string GetStats(string channelName, MemoryMappedViewAccessor accessor)
    {
        int EndOfBuffer = EffectiveCapacity;
        accessor.Read(EndOfBuffer, out int Head);
        accessor.Read(EndOfBuffer + sizeof(int), out int Tail);

        int FreeLength = CircularBufferHelper.GetFreeLength(Head, Tail, EffectiveCapacity);
        int UsedLength = CircularBufferHelper.GetUsedLength(Head, Tail, EffectiveCapacity);
        return $"{channelName} - Head:{Head} Tail:{Tail} Capacity:{EffectiveCapacity} Free:{FreeLength} Used:{UsedLength}";
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

    private void SetDataFileAndAccessor(int index, MemoryMappedFile? file, EventWaitHandle? sharingHandle, MemoryMappedViewAccessor? accessor)
    {
        Contract.Require(index >= 0);
        Contract.Require(index < EffectiveChannelCount);

        DataAccessors[index]?.Dispose();
        DataSharingHandle[index]?.Dispose();
        DataFiles[index]?.Dispose();

        DataFiles[index] = file;
        DataSharingHandle[index] = sharingHandle;
        DataAccessors[index] = accessor;

        SendingIndex = Mode == ChannelMode.Receive || accessor is null ? -1 : index;
    }

    private string LockMapName => Guid.ToString("B");
    private string DataMapName(int index) => Guid.ToString("B") + "-Channel" + index.ToString(CultureInfo.InvariantCulture);
    private string DataMutexName(int index) => Guid.ToString("B") + "-Mutex" + index.ToString(CultureInfo.InvariantCulture);

    private readonly int EffectiveCapacity = Capacity > 0 ? Capacity : 0x100;
    private MemoryMappedFile? LockFile;
    private MemoryMappedViewAccessor? LockAccessor;
    private readonly MemoryMappedFile?[] DataFiles = new MemoryMappedFile?[channelCount > 0 ? channelCount : 1];
    private readonly EventWaitHandle?[] DataSharingHandle = new EventWaitHandle?[channelCount > 0 ? channelCount : 1];
    private readonly MemoryMappedViewAccessor?[] DataAccessors = new MemoryMappedViewAccessor?[channelCount > 0 ? channelCount : 1];
    private int SendingIndex = -1;
    private MemoryMappedViewAccessor? SelectedAccessor => DataAccessors[SendingIndex];
    private int ReceivingIndex;
    private bool DisposedValue;

    /// <summary>
    /// Optionally disposes of the instance.
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