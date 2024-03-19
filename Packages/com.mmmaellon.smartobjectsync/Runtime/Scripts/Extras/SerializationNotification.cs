
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    public class SerializationNotification : SmartObjectSync
    {
        public AudioSource audioSource;

        public override void OnPreSerialization()
        {
            base.OnPreSerialization();
            audioSource.Play();
        }
        public override void OnDeserialization(VRC.Udon.Common.DeserializationResult result)
        {
            base.OnDeserialization(result);
            audioSource.Play();
        }
    }
}
