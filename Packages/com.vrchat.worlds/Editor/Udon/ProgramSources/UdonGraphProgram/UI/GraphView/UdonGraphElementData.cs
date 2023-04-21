using System;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram
{
    [Serializable]
    public class UdonGraphElementData
    {
        public UdonGraphElementType type;
        public string uid;
        public string jsonData;

        public UdonGraphElementData(UdonGraphElementType type, string uid, string jsonData)
        {
            this.type = type;
            this.jsonData = jsonData;
            this.uid = uid;
        }
    }

    public enum UdonGraphElementType
    {
        GraphElement,
        UdonStackNode,
        UdonGroup,
        UdonComment,
        Minimap,
        VariablesWindow,
    };
}