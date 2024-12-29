namespace ProcessCommunication;

using System;

/// <summary>
/// Represents a type implementing a communication channel.
/// </summary>
public interface IMultiChannel : IDisposable
{
    /// <summary>
    /// Gets the channel guid.
    /// </summary>
    Guid ChannelGuid { get; }

    /// <summary>
    /// Gets the caller channel mode.
    /// </summary>
    ChannelMode Mode { get; }

    /// <summary>
    /// Gets the channel count. If 0 or less, 1 is assumed (see <see cref="EffectiveChannelCount"/>).
    /// </summary>
    int ChannelCount { get; }

    /// <summary>
    /// Gets the effective channel count.
    /// </summary>
    int EffectiveChannelCount { get; }

    /// <summary>
    /// Opens the channel.
    /// </summary>
    /// <exception cref="InvalidOperationException">The channel is already open.</exception>
    void Open();

    /// <summary>
    /// Gets a value indicating whether the channel is open.
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Gets a value indicating whether the specified channel is connected.
    /// </summary>
    /// <param name="index">The channel index.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index.</exception>
    bool IsConnected(int index);

    /// <summary>
    /// Gets the last error.
    /// </summary>
    string LastError { get; }

    /// <summary>
    /// Reads data from the channel.
    /// This method requires <see cref="Mode"/> to be <see cref="ChannelMode.Receive"/>.
    /// </summary>
    /// <param name="data">The data read upon return if successful.</param>
    /// <param name="index">Index of the channel with data upon return.</param>
    /// <returns><see langword="true"/> data has been read; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    bool TryRead(out byte[] data, out int index);

    /// <summary>
    /// Gets the number of free bytes in the channel.
    /// This method requires <see cref="Mode"/> to be <see cref="ChannelMode.Send"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    int GetFreeLength();

    /// <summary>
    /// Gets the number of free bytes in the specified channel.
    /// </summary>
    /// <param name="index">The channel index.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index.</exception>
    /// <exception cref="InvalidOperationException">The specified channel is not connected.</exception>
    int GetFreeLength(int index);

    /// <summary>
    /// Gets the number of used bytes in the channel.
    /// This method requires <see cref="Mode"/> to be <see cref="ChannelMode.Send"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    int GetUsedLength();

    /// <summary>
    /// Gets the number of used bytes in the channel.
    /// </summary>
    /// <param name="index">The channel index.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index.</exception>
    /// <exception cref="InvalidOperationException">The specified channel is not connected.</exception>
    int GetUsedLength(int index);

    /// <summary>
    /// Writes data to the channel.
    /// This method requires <see cref="Mode"/> to be <see cref="ChannelMode.Send"/>.
    /// </summary>
    /// <param name="data">The data to write.</param>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    void Write(byte[] data);

    /// <summary>
    /// Gets channel stats for debug purpose.
    /// This method requires <see cref="Mode"/> to be <see cref="ChannelMode.Send"/>.
    /// </summary>
    /// <param name="channelName">The channel name.</param>
    /// <exception cref="ArgumentNullException"><paramref name="channelName"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    string GetStats(string channelName);

    /// <summary>
    /// Gets channel stats for debug purpose for the specified channel.
    /// </summary>
    /// <param name="channelName">The channel name.</param>
    /// <param name="index">The channel index.</param>
    /// <exception cref="ArgumentNullException"><paramref name="channelName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index.</exception>
    /// <exception cref="InvalidOperationException">The channel is not connected.</exception>
    string GetStats(string channelName, int index);

    /// <summary>
    /// Closes the chanel.
    /// If the channel is already closed this is a no-op.
    /// </summary>
    void Close();
}
