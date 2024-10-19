namespace ProcessCommunication;

using System;

/// <summary>
/// Provides constants for variuous timeouts.
/// </summary>
public static class Timeouts
{
    /// <summary>
    /// The default timeout waiting for the process to be started.
    /// </summary>
    internal static readonly TimeSpan DefaultProcessLaunchTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the timeout waiting for the process to be started.
    /// </summary>
    public static TimeSpan ProcessLaunchTimeout { get; set; } = DefaultProcessLaunchTimeout;

    /// <summary>
    /// The default timeout waiting for channels to no longer be busy.
    /// </summary>
    internal static readonly TimeSpan DefaultBusyTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the timeout waiting for channels to no longer be busy.
    /// </summary>
    public static TimeSpan BusyTimeout { get; set; } = DefaultBusyTimeout;

    /// <summary>
    /// The default timeout waiting for acknowledge.
    /// </summary>
    internal static readonly TimeSpan DefaultAcknowledgeTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the timeout waiting for acknowledge.
    /// </summary>
    public static TimeSpan AcknowledgeTimeout { get; set; } = DefaultAcknowledgeTimeout;

    /// <summary>
    /// The default timeout waiting for new data.
    /// </summary>
    internal static readonly TimeSpan DefaultIdleTimeout = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets the timeout waiting for new data.
    /// </summary>
    public static TimeSpan IdleTimeout { get; set; } = DefaultIdleTimeout;

    /// <summary>
    /// Resets timeouts to their default value.
    /// </summary>
    public static void Reset()
    {
        ProcessLaunchTimeout = DefaultProcessLaunchTimeout;
        BusyTimeout = DefaultBusyTimeout;
        AcknowledgeTimeout = DefaultAcknowledgeTimeout;
        IdleTimeout = DefaultIdleTimeout;
    }
}
