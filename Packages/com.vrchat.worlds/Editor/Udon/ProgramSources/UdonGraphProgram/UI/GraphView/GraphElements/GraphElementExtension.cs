#if UNITY_2019_3_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif
using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView;

#if UNITY_2019_3_OR_NEWER
namespace UnityEditor.Experimental.GraphView
#else
namespace UnityEditor.Experimental.UIElements.GraphView
#endif
{
    public static class GraphElementExtension
    {

        public static void Reload(this GraphElement element)
        {
            var evt = new Event()
            {
                type = EventType.ExecuteCommand,
                commandName = UdonGraphCommands.Reload
            };
            using (var e = ExecuteCommandEvent.GetPooled(evt))
            {
                element.SendEvent(e);
            }
        }

        public static void Compile(this GraphElement element)
        {
            var evt = new Event()
            {
                type = EventType.ExecuteCommand,
                commandName = UdonGraphCommands.Compile
            };
            using (var e = ExecuteCommandEvent.GetPooled(evt))
            {
                element.SendEvent(e);
            }
        }
        
        public static string GetUid(this GraphElement element)
        {
#if UNITY_2019_3_OR_NEWER
            return element.viewDataKey;
#else
            return element.persistenceKey;
#endif
        }

        public static void MarkDirty()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

        public static Vector2 GetSnappedPosition(Vector2 position)
        {
            // don't snap at 0 size
            var snap = Settings.GridSnapSize;
            if (snap == 0) return position;

            position.x = (float)Math.Round(position.x / snap) * snap;
            position.y = (float)Math.Round(position.y / snap) * snap;

            return position;
        } 

        public static Rect GetSnappedRect(Rect rect)
        {
            rect.position = GetSnappedPosition(rect.position);
            return rect;
        }
    }
}