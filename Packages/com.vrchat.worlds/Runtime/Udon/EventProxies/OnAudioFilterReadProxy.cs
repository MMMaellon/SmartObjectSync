using UnityEngine;

namespace VRC.Udon
{
    [AddComponentMenu("")]
    internal class OnAudioFilterReadProxy : AbstractUdonBehaviourEventProxy
    {
        public void OnAudioFilterRead(float[] data, int channels)
        {
            EventReceiver.ProxyOnAudioFilterRead(data, channels);
        }
    }
}