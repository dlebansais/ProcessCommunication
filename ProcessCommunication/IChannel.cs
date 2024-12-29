namespace ProcessCommunication;

using System;

/// <summary>
/// Represents a type implementing a communication channel.
/// </summary>
public interface IChannel : IDisposable
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
    /// Opens the channel.
    /// </summary>
    /// <exception cref="InvalidOperationException">The channel is already open.</exception>
    void Open();

    /// <summary>
    /// Gets a value indicating whether the channel is open.
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Gets the last error.
    /// </summary>
    string LastError { get; }

    /// <summary>
    /// Reads data from the channel.
    /// This method requires <see cref="Mode"/> to be <see cref="ChannelMode.Receive"/>.
    /// </summary>
    /// <param name="data">The data read upon return if successful.</param>
    /// <returns><see langword="true"/> data has been read; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    bool TryRead(out byte[] data);

    /// <summary>
    /// Gets the number of free bytes in the channel.
    /// </summary>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    int GetFreeLength();

    /// <summary>
    /// Gets the number of used bytes in the channel.
    /// </summary>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    public int GetUsedLength();

    /// <summary>
    /// Writes data to the channel.
    /// This method requires <see cref="Mode"/> to be <see cref="ChannelMode.Send"/>.
    /// </summary>
    /// <param name="data">The data to write.</param>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    void Write(byte[] data);

    /// <summary>
    /// Gets channel stats for debug purpose.
    /// </summary>
    /// <param name="channelName">The channel name.</param>
    /// <exception cref="ArgumentNullException"><paramref name="channelName"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The channel is not open.</exception>
    string GetStats(string channelName);

    /// <summary>
    /// Closes the channel.
    /// If the channel is already closed this is a no-op.
    /// </summary>
    void Close();
}
