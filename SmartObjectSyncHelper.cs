
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


#if !COMPILER_UDONSHARP && UNITY_EDITOR
using VRC.SDKBase.Editor.BuildPipeline;
using UnityEditor;
using UdonSharpEditor;
using System.Collections.Immutable;


namespace MMMaellon
{
    [CustomEditor(typeof(SmartObjectSyncHelper))]

    public class SmartObjectSyncHelperEditor : Editor
    {
        
        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }
    }
}

#endif

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SmartObjectSyncHelper : UdonSharpBehaviour
    {
        public SmartObjectSync sync;
        public void Update()
        {
            if (!sync || !Utilities.IsValid(sync.owner))
            {
                Debug.LogWarning(name + " is missing sync or sync owner");
                enabled = false;
                return;
            }

            if (!sync.owner.isLocal)
            {
                if (sync.lastSync < 0)
                {
                    sync._printErr("waiting for first sync" + sync.lastSync);
                    //if we haven't received data yet, do nothing. Otherwise this will move the object to the origin
                    enabled = false;
                    return;
                }
                sync.CalcLerpProgress();
                sync.CalcParentTransform();
                sync.LerpToSyncedTransform();


                if (sync.lerpProgress >= 1 && !sync.IsAttachedToPlayer() && (sync.rigid == null || sync.rigid.isKinematic || sync.state != SmartObjectSync.STATE_SLEEPING || sync.rigid.IsSleeping()))
                {
                    //we no longer need to update every frame since we're already caught up
                    //unless we're attached to a player, then we need to set the sync local to a body part of the player every frame
                    sync._print("fell asleep");
                    enabled = false;
                }
                return;
            } else
            {

                switch (sync.state)
                {
                    case (SmartObjectSync.STATE_SLEEPING):
                    case (SmartObjectSync.STATE_TELEPORTING):
                    case (SmartObjectSync.STATE_LERPING):
                    case (SmartObjectSync.STATE_FALLING):
                        {
                            //if we're here, that means we're being controlled entirely by physics and outside forces
                            if (!sync.LerpDelayOver())
                            {
                                break;
                            }

                            if (ShouldBeSleeping())
                            {
                                if (sync.state != SmartObjectSync.STATE_SLEEPING)
                                {
                                    sync.state = SmartObjectSync.STATE_SLEEPING;
                                }
                                //sleeping objects don't move so we disable the update loop on the helper
                                sync._print("should be sleeping");
                                enabled = false;
                                break;
                            }

                            //we check to see if the current velocity has changed much from what we last synced over the network
                            //we first take out the gravity component and then we doe a != compare. Unity docs say that kind of compare takes into account floating point imprecision
                            if (NonGravitationalAcceleration())
                            {
                                sync.Synchronize();
                                sync.lastSync = Time.timeSinceLevelLoad;
                            }
                            else
                            {
                                sync.lastSync = Time.timeSinceLevelLoad;
                            }

                            if (sync.transform.position.y < sync.respawn_height)
                            {
                                sync.Respawn();
                            }
                            break;
                        }
                    case (SmartObjectSync.STATE_LEFT_HAND_HELD):
                    case (SmartObjectSync.STATE_RIGHT_HAND_HELD):
                        {
                            //if we're manipulating the sync with our hands
                            //then we continually check to see if the offset has changed because we used the ijkl keys or the pickup hit another collider or something
                            if (!sync.LerpDelayOver())
                            {
                                break;
                            }
                            if (sync.ObjectMoved())
                            {
                                sync.Synchronize();
                                sync.lastSync = Time.timeSinceLevelLoad;
                            }
                            else
                            {
                                //everything is still in sync so we reset the timer on the sync interval
                                sync.lastSync = Time.timeSinceLevelLoad;
                            }
                            break;
                        }
                    default:
                        {
                            //if the sync is attached to the local player, then we have to move it local to our position just like the local clients do.
                            //the difference being that we never lerp so syncProgress needs to be 1.0f
                            sync.lerpProgress = 1.0f;
                            sync.CalcParentTransform();
                            sync.LerpToSyncedTransform();
                            break;
                        }
                }
            }
        }


        public bool ShouldBeSleeping()
        {
            return sync.state == SmartObjectSync.STATE_SLEEPING || sync.rigid == null || sync.rigid.isKinematic || sync.rigid.IsSleeping();
        }
        
        Vector3 gravityLessVelocity;
        Vector3 gravityProjection;
        Vector3 gravityProjectionSynced;
        public bool NonGravitationalAcceleration()
        {
            if (!sync.rigid)
            {
                return false;
            }
            //returns true of object's velocity changed along an axis other than gravity's
            //this will let us know if the object stayed in projectile motion or if another force acted upon it
            gravityProjection = Vector3.Project(sync.rigid.velocity, Physics.gravity);
            gravityProjectionSynced = Vector3.Project(sync.vel, Physics.gravity);

            if (Vector3.Dot(gravityProjection, Physics.gravity) < Vector3.Dot(gravityProjectionSynced, Physics.gravity))
            {
                //This means that the object was moving against the gravity
                //there is definitely a non-gravitational velocity change
                return true;
            }

            //we know that the object acelerated along the gravity vector,
            //but if it also acelerated on another axis then another force acted upon it
            //here we remove the influence of gravity and compare the velocity with the last synced velocity
            gravityLessVelocity = sync.vel - gravityProjectionSynced;
            return Vector3.Distance(gravityLessVelocity, sync.rigid.velocity - gravityProjection) > 0.001f;
        }

    }
}