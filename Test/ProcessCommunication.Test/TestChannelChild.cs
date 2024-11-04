namespace ProcessCommunication;

using System;

public class TestChannelChild(Guid guid, ChannelMode mode) : Channel(guid, mode)
{
    protected override void Dispose(bool disposing)
    {
        // For coverage only. Validates the dispose pattern.
        base.Dispose(false);
        base.Dispose(disposing);
        base.Dispose(false);
    }
}