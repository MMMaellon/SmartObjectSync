
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
        public override void OnDeserialization()
        {
            base.OnDeserialization();
            audioSource.Play();
        }
    }
}