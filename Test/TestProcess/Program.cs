﻿namespace TestProcess;

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

        Stopwatch Stopwatch = Stopwatch.StartNew();
        TimeSpan Timeout = TimeSpan.FromSeconds(10);

        while (Stopwatch.Elapsed < Timeout)
        {
            Thread.Sleep(100);

            _ = Channel.Read();
        }
    }
}