
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

        public float cooldown = 0f;
        float lastStateChange = -1001f;
        public override void OnChangeState(SmartObjectSync sync, int oldState, int newState)
        {
            if (Time.timeSinceLevelLoad < 5f)//delay to prevent the spam you get at load in
            {
                return;
            }
            transform.position = sync.transform.position;
            if (cooldown > 0f && lastStateChange + cooldown > Time.timeSinceLevelLoad)
            {
                return;
            }
            lastStateChange = Time.timeSinceLevelLoad;
            if (newState >= SmartObjectSync.STATE_CUSTOM)
            {
                if (!Utilities.IsValid(sync.customState))
                {
                    return;
                }
                if (sync.customState == sync.GetComponent<InventoryState>())
                {
                    if (Utilities.IsValid(textBox))
                    {
                        textBox.text = "CUSTOM STATE: Stored in inventory";
                    }
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
                        if (Utilities.IsValid(textBox))
                        {
                            textBox.text = "CUSTOM STATE: Stick to " + sticky.parentTransformName;
                        }
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
                        if (Utilities.IsValid(textBox))
                        {
                            textBox.text = "CUSTOM STATE: Stacked on " + stack.rootParentName;
                        }
                        if (audioSource)
                        {
                            audioSource.PlayOneShot(stickSound);
                        }
                    }
                }
                else
                {
                    if (Utilities.IsValid(textBox))
                    {
                        textBox.text = "CUSTOM STATE: unknown";
                    }
                }
            }
            else if (newState < 0)
            {
                if (Utilities.IsValid(textBox))
                {
                    textBox.text = "Attached to " + sync.StateToString(newState);
                }
                if (audioSource)
                {
                    audioSource.PlayOneShot(attachSound);
                }
            }
            else
            {
                if (Utilities.IsValid(textBox))
                {
                    textBox.text = sync.StateToString(newState);
                }
                if (newState == SmartObjectSync.STATE_TELEPORTING)
                {
                    //just got respawned or teleported
                    if (audioSource)
                    {
                        audioSource.PlayOneShot(teleportSound);
                    }
                }
                else if (sync.IsHeld())
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
                    else if ((newState == SmartObjectSync.STATE_INTERPOLATING && (oldState == SmartObjectSync.STATE_INTERPOLATING || oldState == SmartObjectSync.STATE_FALLING))|| (oldState == SmartObjectSync.STATE_FALLING && newState == SmartObjectSync.STATE_FALLING))
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
