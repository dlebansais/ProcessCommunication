namespace ProcessCommunication;

using System;

/// <summary>
/// Provides constants for variuous timeouts.
/// </summary>
public static class Timeouts
{
    /// <summary>
    /// The timeout waiting for the process to be started.
    /// </summary>
    public static readonly TimeSpan ProcessLaunchTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The timeout waiting for channels to no longer be busy.
    /// </summary>
    public static readonly TimeSpan BusyTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The timeout waiting for acknowledge.
    /// </summary>
    public static readonly TimeSpan AcknowledgeTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The timeout waiting for new data.
    /// </summary>
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(60);
}
