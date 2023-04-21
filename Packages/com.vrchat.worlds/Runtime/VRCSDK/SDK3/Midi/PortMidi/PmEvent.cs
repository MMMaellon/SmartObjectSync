using PmMessage = System.Int32;
using PmTimestamp = System.Int32;

namespace PortMidi
{
    public struct PmEvent
    {
        public PmMessage message;
        public PmTimestamp timestamp;
    }
}