
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DestructibleObject : UdonSharpBehaviour
    {
        public SmartObjectSync wholeObject;
        public Transform piecesParent;
        public SmartObjectSync[] pieces;
        public AudioSource breakSound;
        public float resetTimer = 30f;
        [UdonSynced]
        public bool _broken = false;
        public bool broken{
            get => _broken;
            set
            {
                if (value)
                {
                    wholeObject.gameObject.SetActive(false);
                    piecesParent.transform.position = wholeObject.transform.position;
                    piecesParent.transform.rotation = wholeObject.transform.rotation;
                    foreach (SmartObjectSync sync in pieces)
                    {
                        if (Utilities.IsValid(sync))
                        {
                            sync.gameObject.SetActive(true);
                            sync.rigid.isKinematic = false;
                            if (wholeObject.IsLocalOwner())
                            {
                                sync.TakeOwnership(false);
                                sync.Respawn();
                            } else
                            {
                                sync.transform.localPosition = sync.spawnPos;
                                sync.transform.localRotation = sync.spawnRot;
                                sync.pos = sync.transform.position;
                                sync.rot = sync.transform.rotation;
                            }
                            sync.rigid.velocity = wholeObject.rigid.velocity;
                            sync.rigid.angularVelocity = wholeObject.rigid.angularVelocity;
                        }
                    }
                    if (Utilities.IsValid(breakSound))
                    {
                        breakSound.Play();
                    }
                    lastBroken = Time.timeSinceLevelLoad;
                    if (resetTimer > 0)
                    {
                        SendCustomEventDelayedSeconds(nameof(lastBroken), resetTimer);
                    }
                } else
                {
                    wholeObject.gameObject.SetActive(true);
                    if (wholeObject.IsLocalOwner())
                    {
                        wholeObject.Respawn();
                    } else if (_broken != value)
                    {
                        wholeObject.StartInterpolation();
                    }
                    foreach (SmartObjectSync sync in pieces)
                    {
                        if (Utilities.IsValid(sync))
                        {
                            sync.gameObject.SetActive(false);
                        }
                    }
                    lastBroken = -1001f;
                }

                _broken = value;

                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }
        void Start()
        {
            broken = broken;
        }

        public void Break()
        {
            if (!wholeObject.IsLocalOwner() || broken)
            {
                return;
            }
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            broken = true;
        }

        public void Fix()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            Networking.SetOwner(Networking.LocalPlayer, wholeObject.gameObject);
            broken = false;
        }

        float lastBroken = -1001f;
        public void FixCheck()
        {
            if (Networking.LocalPlayer.IsOwner(gameObject) && lastBroken > 0 && lastBroken + resetTimer - 0.1f < Time.timeSinceLevelLoad || !broken)
            {
                Fix();
            }
        }
    }
}