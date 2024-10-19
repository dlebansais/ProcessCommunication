﻿#pragma warning disable CS1591
#pragma warning disable SA1600

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
    public static string? Info1 { get; private set; }
    public static string? Info2 { get; private set; }
    public static string? Info3 { get; private set; }
    public static string? Info4 { get; private set; }

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
                SetProcess(Process.Start(ProcessStartInfo));

                Info1 = "Launched";
            }
            catch
            {
                Info2 = "Exception";
            }
        }

        if (CreatedChannel is null)
            SetChannel(new(guid, Mode.Send));

        CreatedChannel = Contract.AssertNotNull(CreatedChannel);

        if (!CreatedChannel.IsOpen && CreationStopwatch.Elapsed >= Timeouts.ProcessLaunchTimeout)
        {
            Info3 = "Too late";
            return null;
        }

        CreatedChannel.Open();

        if (!CreatedChannel.IsOpen)
        {
            Info4 = "Not open";
            return null;
        }

        return CreatedChannel;
    }

    /// <summary>
    /// Resets the helper state.
    /// </summary>
    public static void Reset()
    {
        CreationStopwatch.Reset();
        SetProcess(null);
        SetChannel(null);
        Info1 = null;
        Info2 = null;
        Info3 = null;
        Info4 = null;
    }

    private static void SetProcess(Process? process)
    {
        CreatedProcess?.Dispose();
        CreatedProcess = process;
    }

    private static void SetChannel(Channel? channel)
    {
        CreatedChannel?.Dispose();
        CreatedChannel = channel;
    }

    private static readonly Stopwatch CreationStopwatch = new();
    private static Process? CreatedProcess;
    private static Channel? CreatedChannel;
}