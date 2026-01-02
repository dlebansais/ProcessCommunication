namespace ProcessCommunication;

using System;
using System.Globalization;
using System.IO.MemoryMappedFiles;
using System.Threading;
using Contracts;

/// <summary>
/// Represents a communication channel.
/// </summary>
/// <param name="channelGuid">The channel guid.</param>
/// <param name="mode">The caller channel mode.</param>
/// <param name="channelCount">The channel count. If 0 or less, 1 is assumed.</param>
public class MultiChannel(Guid channelGuid, ChannelMode mode, int channelCount) : IMultiChannel, IDisposable
{
    /// <summary>
    /// Gets or sets the channel capacity.
    /// </summary>
    public static int Capacity { get; set; } = 0x100000;

    /// <inheritdoc />
    public Guid ChannelGuid { get; } = channelGuid;

    /// <inheritdoc />
    public ChannelMode Mode { get; } = mode;

    /// <inheritdoc />
    public int ChannelCount { get; } = channelCount;

    /// <inheritdoc />
    public int EffectiveChannelCount { get; } = channelCount > 0 ? channelCount : 1;

    /// <inheritdoc />
    public void Open()
    {
        if (IsOpen)
            throw new InvalidOperationException();

        Contract.Assert(Array.TrueForAll(DataFiles, item => item is null));
        Contract.Assert(Array.TrueForAll(DataAccessors, item => item is null));
        Contract.Assert(SendingIndex == -1);

        if (Mode == ChannelMode.Receive)
            OpenReceivingDataChannels();
        else
            OpenSendingDataChannel();
    }

    private void OpenReceivingDataChannels()
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
            }
        }
    }

    private void CloseReceivingDataChannels()
    {
        for (int i = 0; i < EffectiveChannelCount; i++)
            SetDataFileAndAccessor(i, null, null, null);
    }

    private void OpenSendingDataChannel()
    {
        unsafe
        {
            // Try to open all channels until we get a free one.
            for (int i = 0; i < EffectiveChannelCount; i++)
                if (TryOpenSendingDataChannel(i))
                    return;

            LastError = $"Out of slots (Tried {EffectiveChannelCount})";
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

    private void CloseSendingDataChannel()
        => SetDataFileAndAccessor(SendingIndex, null, null, null);

    /// <inheritdoc />
    public bool IsOpen => Mode == ChannelMode.Receive ? Array.TrueForAll(DataAccessors, accessor => accessor is not null) : SendingIndex >= 0;

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index.</exception>
    public bool IsConnected(int index)
    {
        return index < 0 || index >= EffectiveChannelCount
            ? throw new ArgumentOutOfRangeException(nameof(index))
            : DataAccessors[index] is not null;
    }

    /// <inheritdoc />
    public string LastError { get; private set; } = string.Empty;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    public bool TryRead(out byte[] data, out int index)
    {
        if (Mode != ChannelMode.Receive || !IsOpen)
            throw new InvalidOperationException();

        int StartingIndex = ReceivingIndex;

        do
        {
            MemoryMappedViewAccessor ConnectedAccessor = Contract.AssertNotNull(DataAccessors[ReceivingIndex]);

            if (CircularBufferHelper.Read(ConnectedAccessor, EffectiveCapacity, out data))
            {
                index = ReceivingIndex;
                return true;
            }

            ReceivingIndex++;
            if (ReceivingIndex >= EffectiveChannelCount)
                ReceivingIndex = 0;
        }
        while (StartingIndex != ReceivingIndex);

        Contract.Unused(out data);
        Contract.Unused(out index);
        return false;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    public int GetFreeLength()
    {
        if (Mode == ChannelMode.Receive || SendingIndex < 0)
            throw new InvalidOperationException();

        MemoryMappedViewAccessor Accessor = Contract.AssertNotNull(SelectedAccessor);

        return CircularBufferHelper.GetFreeLength(Accessor, EffectiveCapacity);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index.</exception>
    /// <exception cref="InvalidOperationException">The specified channel is not connected.</exception>
    public int GetFreeLength(int index)
    {
        if (index < 0 || index >= EffectiveChannelCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        MemoryMappedViewAccessor Accessor = DataAccessors[index] ?? throw new InvalidOperationException();

        return CircularBufferHelper.GetFreeLength(Accessor, EffectiveCapacity);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    public int GetUsedLength()
    {
        if (Mode == ChannelMode.Receive || SendingIndex < 0)
            throw new InvalidOperationException();

        MemoryMappedViewAccessor Accessor = Contract.AssertNotNull(SelectedAccessor);

        return CircularBufferHelper.GetUsedLength(Accessor, EffectiveCapacity);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index.</exception>
    /// <exception cref="InvalidOperationException">The specified channel is not connected.</exception>
    public int GetUsedLength(int index)
    {
        if (index < 0 || index >= EffectiveChannelCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        MemoryMappedViewAccessor Accessor = DataAccessors[index] ?? throw new InvalidOperationException();

        return CircularBufferHelper.GetUsedLength(Accessor, EffectiveCapacity);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="channelName"/> is <see langword="null"/>.</exception>
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

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="channelName"/> is <see langword="null"/>.</exception>
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

    /// <inheritdoc />
    public void Close()
    {
        if (!IsOpen)
            return;

        if (Mode == ChannelMode.Receive)
            CloseReceivingDataChannels();
        else
            CloseSendingDataChannel();
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

    private string DataMapName(int index) => ChannelGuid.ToString("B") + "-Channel" + index.ToString(CultureInfo.InvariantCulture);
    private string DataMutexName(int index) => ChannelGuid.ToString("B") + "-Mutex" + index.ToString(CultureInfo.InvariantCulture);

    private readonly int EffectiveCapacity = Capacity > 0 ? Capacity : 0x100;
    private readonly MemoryMappedFile?[] DataFiles = new MemoryMappedFile?[channelCount > 0 ? channelCount : 1];
    private readonly EventWaitHandle?[] DataSharingHandle = new EventWaitHandle?[channelCount > 0 ? channelCount : 1];
    private readonly MemoryMappedViewAccessor?[] DataAccessors = new MemoryMappedViewAccessor?[channelCount > 0 ? channelCount : 1];
    private int SendingIndex = -1;
    private MemoryMappedViewAccessor? SelectedAccessor => DataAccessors[SendingIndex];
    private int ReceivingIndex;
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

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}