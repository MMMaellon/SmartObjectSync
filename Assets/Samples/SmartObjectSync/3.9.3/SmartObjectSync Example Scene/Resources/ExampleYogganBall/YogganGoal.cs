
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class YogganGoal : UdonSharpBehaviour
{
    public ParticleSystem particles;
    void Start()
    {
        
    }

    public void BroadcastParticles()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Particles));
    }

    public void Particles()
    {
        particles.Play();
    }
}
