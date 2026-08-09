namespace ProcessCommunication;

using System;

internal class TestChannelChild(Guid channelGuid, ChannelMode mode) : Channel(channelGuid, mode)
{
    protected override void Dispose(bool disposing)
    {
        // For coverage only. Validates the dispose pattern.
        base.Dispose(false);
        base.Dispose(disposing);
        base.Dispose(false);
    }
}