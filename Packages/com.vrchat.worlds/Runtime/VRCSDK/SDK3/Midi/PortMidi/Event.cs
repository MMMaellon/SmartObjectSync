namespace PortMidi
{
    public class Event
    {
        public Event(PmEvent pmEvent)
        {
            this.Status = PortMidiMarshal.Pm_MessageStatus(pmEvent.message);
            this.Data1 = PortMidiMarshal.Pm_MessageData1(pmEvent.message);
            this.Data2 = PortMidiMarshal.Pm_MessageData2(pmEvent.message);
        }

        public long Timestamp { get; set; }
        public long Status { get; set; }
        public long Data1 { get; set; }
        public long Data2 { get; set; }
    }
}