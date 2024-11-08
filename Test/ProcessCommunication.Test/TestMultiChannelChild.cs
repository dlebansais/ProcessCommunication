namespace ProcessCommunication;

using System;

public class TestMultiChannelChild(Guid guid, ChannelMode mode, int channelCount) : MultiChannel(guid, mode, channelCount)
{
    protected override void Dispose(bool disposing)
    {
        // For coverage only. Validates the dispose pattern.
        base.Dispose(false);
        base.Dispose(disposing);
        base.Dispose(false);
    }
}