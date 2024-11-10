namespace ProcessCommunication;

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
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
    /// Launches a process and opens a channel in send mode.
    /// Because launching a process can take time, callers should retry repeatedly until success.
    /// After <see cref="Timeouts.ProcessLaunchTimeout"/> has elapsed this method will always return the same result until <see cref="Reset"/> is called.
    /// </summary>
    /// <param name="pathToProcess">The process to launch.</param>
    /// <param name="guid">The channel guid.</param>
    /// <param name="arguments">Optional arguments.</param>
    /// <returns>The channel if successul; otherwise, <see langword="null"/>.</returns>
    public static IChannel? LaunchAndOpenChannel(string pathToProcess, Guid guid, string? arguments = null)
    {
        if (!CreationStopwatch.IsRunning)
        {
            CreationStopwatch.Start();

            try
            {
                ProcessStartInfo ProcessStartInfo = new();
                ProcessStartInfo.FileName = pathToProcess;
                ProcessStartInfo.Arguments = arguments;
                ProcessStartInfo.UseShellExecute = false;
                ProcessStartInfo.WorkingDirectory = Path.GetDirectoryName(pathToProcess);

                using Process? CreatedProcess = Process.Start(ProcessStartInfo);
            }
            catch
            {
            }
        }

        if (CreatedChannel is null)
            CreatedChannel = SetChannel(new Channel(guid, ChannelMode.Send));

        if (CreationStopwatch.Elapsed < Timeouts.ProcessLaunchTimeout)
            CreatedChannel.Open();

        if (!CreatedChannel.IsOpen)
            return null;

        IChannel Result = CreatedChannel;
        CreatedChannel = null;

        return Result;
    }

    /// <summary>
    /// Asynchronously launches a process and opens a channel in send mode.
    /// </summary>
    /// <param name="pathToProcess">The process to launch.</param>
    /// <param name="guid">The channel guid.</param>
    /// <param name="arguments">Optional arguments.</param>
    /// <returns>The channel if successul; otherwise, <see langword="null"/>.</returns>
    public static async Task<IChannel?> LaunchAndOpenChannelAsync(string pathToProcess, Guid guid, string? arguments = null)
    {
        Channel? Result = null;

        CreationStopwatch.Start();

        try
        {
            ProcessStartInfo ProcessStartInfo = new();
            ProcessStartInfo.FileName = pathToProcess;
            ProcessStartInfo.Arguments = arguments;
            ProcessStartInfo.UseShellExecute = false;
            ProcessStartInfo.WorkingDirectory = Path.GetDirectoryName(pathToProcess);

            using Process? CreatedProcess = Process.Start(ProcessStartInfo);
        }
        catch
        {
            return null;
        }

        Channel? Channel;
        Channel = new(guid, ChannelMode.Send);

        while (CreationStopwatch.Elapsed < Timeouts.ProcessLaunchTimeout)
        {
            Channel.Open();

            if (Channel.IsOpen)
            {
                Result = Channel;
                Channel = null;
                break;
            }
            else
                await Task.Delay(100).ConfigureAwait(false);
        }

        Channel?.Dispose();

        return Result;
    }

    /// <summary>
    /// Launches a process and opens a channel in send mode.
    /// Because launching a process can take time, callers should retry repeatedly until success.
    /// After <see cref="Timeouts.ProcessLaunchTimeout"/> has elapsed this method will always return the same result until <see cref="Reset"/> is called.
    /// </summary>
    /// <param name="pathToProcess">The process to launch.</param>
    /// <param name="guid">The channel guid.</param>
    /// <param name="channelCount">The channel count. If 0 or less, 1 is assumed.</param>
    /// <param name="arguments">Optional arguments.</param>
    /// <returns>The channel if successul; otherwise, <see langword="null"/>.</returns>
    public static IMultiChannel? LaunchAndOpenChannel(string pathToProcess, Guid guid, int channelCount, string? arguments = null)
    {
        if (!CreationStopwatch.IsRunning)
        {
            CreationStopwatch.Start();

            try
            {
                ProcessStartInfo ProcessStartInfo = new();
                ProcessStartInfo.FileName = pathToProcess;
                ProcessStartInfo.Arguments = arguments;
                ProcessStartInfo.UseShellExecute = false;
                ProcessStartInfo.WorkingDirectory = Path.GetDirectoryName(pathToProcess);

                using Process? CreatedProcess = Process.Start(ProcessStartInfo);
            }
            catch
            {
            }
        }

        if (CreatedMultiChannel is null)
            CreatedMultiChannel = SetMultiChannel(new MultiChannel(guid, ChannelMode.Send, channelCount));

        if (CreationStopwatch.Elapsed < Timeouts.ProcessLaunchTimeout)
            CreatedMultiChannel.Open();

        if (!CreatedMultiChannel.IsOpen)
            return null;

        IMultiChannel Result = CreatedMultiChannel;
        CreatedMultiChannel = null;

        return Result;
    }

    /// <summary>
    /// Asynchronously launches a process and opens a channel in send mode.
    /// </summary>
    /// <param name="pathToProcess">The process to launch.</param>
    /// <param name="guid">The channel guid.</param>
    /// <param name="channelCount">The channel count. If 0 or less, 1 is assumed.</param>
    /// <param name="arguments">Optional arguments.</param>
    /// <returns>The channel if successul; otherwise, <see langword="null"/>.</returns>
    public static async Task<IMultiChannel?> LaunchAndOpenChannelAsync(string pathToProcess, Guid guid, int channelCount, string? arguments = null)
    {
        MultiChannel? Result = null;

        CreationStopwatch.Start();

        try
        {
            ProcessStartInfo ProcessStartInfo = new();
            ProcessStartInfo.FileName = pathToProcess;
            ProcessStartInfo.Arguments = arguments;
            ProcessStartInfo.UseShellExecute = false;
            ProcessStartInfo.WorkingDirectory = Path.GetDirectoryName(pathToProcess);

            using Process? CreatedProcess = Process.Start(ProcessStartInfo);
        }
        catch
        {
            return null;
        }

        MultiChannel? Channel;
        Channel = new(guid, ChannelMode.Send, channelCount);

        while (CreationStopwatch.Elapsed < Timeouts.ProcessLaunchTimeout)
        {
            Channel.Open();

            if (Channel.IsOpen)
            {
                Result = Channel;
                Channel = null;
                break;
            }
            else
                await Task.Delay(100).ConfigureAwait(false);
        }

        Channel?.Dispose();

        return Result;
    }

    /// <summary>
    /// Resets the helper state.
    /// </summary>
    public static void Reset()
    {
        CreationStopwatch.Reset();
        _ = SetChannel(null);
        _ = SetMultiChannel(null);
    }

    private static Channel SetChannel(Channel? channel)
    {
        CreatedChannel?.Dispose();
        CreatedChannel = channel;

        return channel!;
    }

    private static MultiChannel SetMultiChannel(MultiChannel? channel)
    {
        CreatedMultiChannel?.Dispose();
        CreatedMultiChannel = channel;

        return channel!;
    }

    private static readonly Stopwatch CreationStopwatch = new();
    private static Channel? CreatedChannel;
    private static MultiChannel? CreatedMultiChannel;
}