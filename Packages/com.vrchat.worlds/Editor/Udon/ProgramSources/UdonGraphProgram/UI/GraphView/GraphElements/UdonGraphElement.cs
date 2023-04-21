#if UNITY_2019_3_OR_NEWER
using UnityEditor.Experimental.GraphView;
#else
using UnityEditor.Experimental.UIElements.GraphView;
#endif
using System;
using UnityEngine;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class UdonGraphElement : GraphElement
    {
#if UNITY_2019_3_OR_NEWER
        public string uid { get => viewDataKey; set => viewDataKey = value; }
#else
        public string uid { get => persistenceKey; set => persistenceKey = value; }
#endif
        internal UdonGraphElementType type = UdonGraphElementType.GraphElement;

        internal UdonGraphElement()
        {
        }
    }

    public interface IUdonGraphElementDataProvider
    {
        UdonGraphElementData GetData();
    }

    [Serializable]
    public class GraphRect
    {
        public float x;
        public float y;
        public float width;
        public float height;

        public GraphRect(Rect input)
        {
            this.x = Mathf.Round(input.x);
            this.y = Mathf.Round(input.y);
            this.width = Mathf.Round(input.width);
            this.height = Mathf.Round(input.height);
        }

        public Rect rect => new Rect(this.x, this.y, this.width, this.height);
    }
}