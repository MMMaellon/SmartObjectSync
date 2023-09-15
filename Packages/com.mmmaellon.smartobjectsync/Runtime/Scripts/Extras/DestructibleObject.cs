
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DestructibleObject : UdonSharpBehaviour
    {
        [FieldChangeCallback(nameof(destructible))]
        public bool _destructible = true;
        public bool destructible
        {
            get => _destructible;
            set
            {
                _destructible = value;
                broken = broken;
            }
        }
        public SmartObjectSync wholeObject;
        public Transform piecesParent;
        public GameObject breakObject;
        public SmartObjectSync[] pieces;
        public AudioSource breakSound;
        public float resetTimer = 30f;
        [UdonSynced, FieldChangeCallback(nameof(broken))]
        public bool _broken = false;
        public bool broken
        {
            get => _broken;
            set
            {
                wholeObject.gameObject.SetActive(!(value && _destructible));
                if (Utilities.IsValid(breakObject))
                {
                    breakObject.transform.position = wholeObject.transform.position;
                    breakObject.transform.rotation = wholeObject.transform.rotation;
                    breakObject.SetActive(value);
                }
                if (value && _destructible)
                {
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
                            }
                            else
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
                        breakSound.transform.position = wholeObject.transform.position;
                        breakSound.transform.rotation = wholeObject.transform.rotation;
                        breakSound.Play();
                    }
                    lastBroken = Time.timeSinceLevelLoad;
                    if (resetTimer > 0)
                    {
                        SendCustomEventDelayedSeconds(nameof(FixCheck), resetTimer);
                    }
                }
                else
                {
                    if (wholeObject.IsLocalOwner())
                    {
                        wholeObject.Respawn();
                    }
                    else if (_broken != (value && _destructible))
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

                _broken = (value && _destructible);

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
            if (!wholeObject.IsLocalOwner() || broken || !_destructible)
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
            if (Networking.LocalPlayer.IsOwner(gameObject) && lastBroken > 0 && lastBroken + resetTimer - 0.1f < Time.timeSinceLevelLoad && broken)
            {
                Fix();
            }
        }
    }
}