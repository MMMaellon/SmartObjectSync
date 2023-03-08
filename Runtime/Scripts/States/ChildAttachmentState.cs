
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    public class ChildAttachmentState : SmartObjectSyncState
    {
        void Start()
        {

        }
        public override void OnEnterState(){
            if (!sync.rigid.isKinematic)
            {
                //don't work with physics
                ExitState();
                return;
            }
            sync.startPos = transform.localPosition;
            sync.startRot = transform.localRotation;
            if (sync.IsLocalOwner())
            {
                sync.pos = transform.localPosition;
                sync.rot = transform.localRotation;
            }
        }

        public override void OnExitState(){
        
        }

        public override void OnSmartObjectSerialize(){
        
        }

        public override void OnInterpolationStart(){
        
        }
        
        public override void Interpolate(float interpolation){
            transform.localPosition = sync.HermiteInterpolatePosition(sync.startPos, Vector3.zero, sync.pos, Vector3.zero, interpolation);
            transform.localRotation = sync.HermiteInterpolateRotation(sync.startRot, Vector3.zero, sync.rot, Vector3.zero, interpolation);
        }
        
        public override bool OnInterpolationEnd(){
            return false;
        }

        public override void OnDrop()
        {
            if (sync.IsLocalOwner() && sync.rigid.isKinematic)
            {
                EnterState();
            }
        }
    }
}