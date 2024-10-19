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
        using Channel Channel = new(TestGuid, Mode.Receive);
        Channel.Open();

        if (!Channel.IsOpen)
            return;

        int MaxDuration = 1;
        if (args.Length > 0 && int.TryParse(args[0], out int ArgMaxDuration))
            MaxDuration = ArgMaxDuration;

        Stopwatch Stopwatch = Stopwatch.StartNew();
        TimeSpan Timeout = TimeSpan.FromSeconds(MaxDuration);

        while (Stopwatch.Elapsed < Timeout)
        {
            Thread.Sleep(100);

            _ = Channel.Read();
        }
    }
}
