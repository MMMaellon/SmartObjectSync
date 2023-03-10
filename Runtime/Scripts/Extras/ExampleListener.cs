
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    public class ExampleListener : SmartObjectSyncListener
    {
        public TMPro.TextMeshProUGUI textBox;
        public AudioSource audioSource;
        public AudioClip teleportSound;
        public AudioClip grabSound;
        public AudioClip stickSound;
        public AudioClip attachSound;
        public AudioClip yeetSound;
        public AudioClip inventorySound;
        public AudioClip collisionSound;

        public override void OnChangeState(SmartObjectSync sync, int oldState, int newState)
        {
            transform.position = sync.transform.position;
            if (newState >= SmartObjectSync.STATE_CUSTOM)
            {
                if (sync.customState == sync.GetComponent<InventoryState>())
                {
                    textBox.text = "CUSTOM STATE: Stored in inventory";
                    if (audioSource)
                    {
                        audioSource.PlayOneShot(inventorySound);
                    }
                }
                else if (sync.customState == sync.GetComponent<StickyAttachmentState>())
                {
                    StickyAttachmentState sticky = sync.GetComponent<StickyAttachmentState>();
                    if (Utilities.IsValid(sticky))
                    {
                        textBox.text = "CUSTOM STATE: Stick to " + sticky.parentTransformName;
                        if (audioSource)
                        {
                            audioSource.PlayOneShot(stickSound);
                        }
                    }
                }
                else if (sync.customState == sync.GetComponent<StackableState>())
                {
                    StackableState stack = sync.GetComponent<StackableState>();
                    if (Utilities.IsValid(stack))
                    {
                        textBox.text = "CUSTOM STATE: Stacked on " + stack.rootParentName;
                        if (audioSource)
                        {
                            audioSource.PlayOneShot(stickSound);
                        }
                    }
                }
                else
                {
                    textBox.text = "CUSTOM STATE: unknown";
                }
            }
            else if (newState < 0)
            {
                textBox.text = "Attached to " + sync.StateToString(newState);
                if (audioSource)
                {
                    audioSource.PlayOneShot(attachSound);
                }
            }
            else
            {
                textBox.text = sync.StateToString(newState);
                if (newState == SmartObjectSync.STATE_TELEPORTING)
                {
                    //just got respawned or teleported
                    if (audioSource)
                    {
                        audioSource.PlayOneShot(teleportSound);
                    }
                }
                else if (newState == SmartObjectSync.STATE_LEFT_HAND_HELD || newState == SmartObjectSync.STATE_RIGHT_HAND_HELD || oldState == SmartObjectSync.STATE_NO_HAND_HELD)
                {
                    //just got grabbed
                    if (audioSource)
                    {
                        audioSource.PlayOneShot(grabSound);
                    }
                }
                else
                {
                    if (oldState == SmartObjectSync.STATE_LEFT_HAND_HELD || oldState == SmartObjectSync.STATE_RIGHT_HAND_HELD || oldState == SmartObjectSync.STATE_NO_HAND_HELD || oldState < 0 || oldState > SmartObjectSync.STATE_CUSTOM)
                    {
                        //We were just yeeted
                        if (audioSource)
                        {
                            audioSource.PlayOneShot(yeetSound);
                        }
                    }
                    else if (newState == SmartObjectSync.STATE_INTERPOLATING && (oldState == SmartObjectSync.STATE_INTERPOLATING || oldState == SmartObjectSync.STATE_FALLING))
                    {
                        Debug.LogWarning("oldState: " + sync.StateToString(oldState));
                        //we just collided with something
                        if (audioSource)
                        {
                            audioSource.PlayOneShot(collisionSound);
                        }
                    }
                }
            }
        }

        public override void OnChangeOwner(SmartObjectSync sync, VRCPlayerApi oldOwner, VRCPlayerApi newOwner)
        {
            //do nothing
        }
    }
}
