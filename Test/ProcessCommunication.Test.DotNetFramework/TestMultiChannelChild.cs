namespace ProcessCommunication;

using System;

internal class TestMultiChannelChild(Guid channelGuid, ChannelMode mode, int channelCount) : MultiChannel(channelGuid, mode, channelCount)
{
    protected override void Dispose(bool disposing)
    {
        // For coverage only. Validates the dispose pattern.
        base.Dispose(false);
        base.Dispose(disposing);
        base.Dispose(false);
    }
}