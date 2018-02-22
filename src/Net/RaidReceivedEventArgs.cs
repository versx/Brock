namespace BrockBot.Net
{
    using System;

    public class RaidReceivedEventArgs : EventArgs
    {
        public RaidData Raid { get; }

        public RaidReceivedEventArgs(RaidData raid)
        {
            Raid = raid;
        }
    }
}