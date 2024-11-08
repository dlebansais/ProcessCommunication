namespace TestProcess;

using System;
using System.Diagnostics;
using System.Threading;
using ProcessCommunication;

internal class Program
{
    public static readonly Guid TestGuid = new("20E9C969-C990-4DEB-984F-979C824DCC18");

    private static void Main(string[] args)
    {
        int ArgIndex = 0;
        bool IsMulti = bool.Parse(args[ArgIndex++]);

        int ChannelCount = 1;
        if (args.Length > ArgIndex && int.TryParse(args[ArgIndex++], out int ArgChannelCount))
            ChannelCount = ArgChannelCount;

        int MaxDuration = 1;
        if (args.Length > ArgIndex && int.TryParse(args[ArgIndex++], out int ArgMaxDuration))
            MaxDuration = ArgMaxDuration;

        if (IsMulti)
            RunMultiReceiver(ChannelCount, MaxDuration);
        else
            RunSingleReceiver(MaxDuration);
    }

    private static void RunSingleReceiver(int maxDuration)
    {
        using Channel Channel = new(TestGuid, ChannelMode.Receive);
        Channel.Open();

        if (!Channel.IsOpen)
            return;

        Stopwatch Stopwatch = Stopwatch.StartNew();
        TimeSpan Timeout = TimeSpan.FromSeconds(maxDuration);

        while (Stopwatch.Elapsed < Timeout)
        {
            Thread.Sleep(100);

            _ = Channel.TryRead(out _);
        }
    }

    private static void RunMultiReceiver(int channelCount, int maxDuration)
    {
        using MultiChannel Channel = new(TestGuid, ChannelMode.Receive, channelCount);
        Channel.Open();

        if (!Channel.IsOpen)
            return;

        Stopwatch Stopwatch = Stopwatch.StartNew();
        TimeSpan Timeout = TimeSpan.FromSeconds(maxDuration);

        while (Stopwatch.Elapsed < Timeout)
        {
            Thread.Sleep(100);

            _ = Channel.TryRead(out _, out _);
        }
    }
}
