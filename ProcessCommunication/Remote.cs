namespace ProcessCommunication;

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Contracts;

/// <summary>
/// Provides tools to start a remote process.
/// </summary>
public static class Remote
{
    /// <summary>
    /// Gets the full path to a file in the same directory as the calling assembly.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    public static string GetSiblingFullPath(string fileName)
    {
        string Folder = Contract.AssertNotNull(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location));
        string FullPath = Path.Combine(Folder, fileName);

        return FullPath;
    }

    /// <summary>
    /// Launches a process and open a channel in send mode.
    /// Because launching a process can take time, callers should retry repeatedly until success.
    /// After <see cref="Timeouts.ProcessLaunchTimeout"/> has elapsed this method will permanently fail until <see cref="Reset"/> is called.
    /// </summary>
    /// <param name="pathToProcess">The process to launch.</param>
    /// <param name="guid">The channel guid.</param>
    /// <returns>The channel if successul; otherwise, <see langword="null"/>.</returns>
    public static Channel? LaunchAndOpenChannel(string pathToProcess, Guid guid)
    {
        if (!CreationStopwatch.IsRunning)
        {
            CreationStopwatch.Start();

            try
            {
                ProcessStartInfo ProcessStartInfo = new();
                ProcessStartInfo.FileName = pathToProcess;
                ProcessStartInfo.UseShellExecute = false;
                ProcessStartInfo.WorkingDirectory = Path.GetDirectoryName(pathToProcess);

                using Process? CreatedProcess = Process.Start(ProcessStartInfo);
            }
            catch
            {
            }
        }

        if (CreatedChannel is null)
            SetChannel(new(guid, Mode.Send));

        CreatedChannel = Contract.AssertNotNull(CreatedChannel);

        if (!CreatedChannel.IsOpen && CreationStopwatch.Elapsed >= Timeouts.ProcessLaunchTimeout)
            return null;

        CreatedChannel.Open();

        if (!CreatedChannel.IsOpen)
            return null;

        return CreatedChannel;
    }

    /// <summary>
    /// Resets the helper state.
    /// </summary>
    public static void Reset()
    {
        CreationStopwatch.Reset();
        SetChannel(null);
    }

    private static void SetChannel(Channel? channel)
    {
        CreatedChannel?.Dispose();
        CreatedChannel = channel;
    }

    private static readonly Stopwatch CreationStopwatch = new();
    private static Channel? CreatedChannel;
}