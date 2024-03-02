using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CSharp;
using UnityEngine;
using VRC.Udon.Compiler.Compilers;
using VRC.Udon.Graph;
using VRC.Udon.Graph.Interfaces;
using CompressionLevel = System.IO.Compression.CompressionLevel;
using Object = UnityEngine.Object;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI
{
    public static class UdonGraphExtensions
    {
        private static readonly Dictionary<string, string> FriendlyNameCache;

        static UdonGraphExtensions()
        {
            FriendlyNameCache = new Dictionary<string, string>();
            StartsWithCache = new Dictionary<(string s, string prefix), bool>();
        }

        #region Serialization Utilities
        private const byte ZIP_VERSION = 0;
        public static string ZipString(string str)
        {
            using (MemoryStream output = new MemoryStream())
            {
                using (DeflateStream gzip =
                    new DeflateStream(output, CompressionLevel.Optimal)) //, CompressionMode.Compress
                {
                    using (StreamWriter writer =
                        new StreamWriter(gzip, Encoding.UTF8))
                    {
                        writer.Write(str);
                    }
                }
                List<byte> outputList = output.ToArray().ToList();
                outputList.Insert(0, ZIP_VERSION); //Version Number
                return Convert.ToBase64String(outputList.ToArray());
            }
        }

        public static string UnZipString(string input)
        {
            List<byte> inputList = new List<byte>(Convert.FromBase64String(input));
            if (inputList[0] != ZIP_VERSION) //Version Number
            {
                return "";
            }
            inputList.RemoveAt(0);
            using (MemoryStream inputStream = new MemoryStream(inputList.ToArray()))
            {
                using (DeflateStream gzip =
                    new DeflateStream(inputStream, CompressionMode.Decompress))
                {
                    using (StreamReader reader =
                        new StreamReader(gzip, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }
        #endregion

        #region Color Utilities

        public static class NodeColors
        {
            public static readonly Color Base = new Color(77f / 255f, 157f / 255f, 1);
            public static readonly Color Const = new Color(0.4f, 0.14f, 1f);
            public static readonly Color Variable = new Color(1f, 0.86f, 0.3f);
            public static readonly Color Function = new Color(1f, 0.42f, 0.14f);
            public static readonly Color Event = new Color(0.53f, 1f, 0.3f);
            public static readonly Color Return = new Color(0.89f, 0.2f, 0.2f);
        }

        private static readonly MD5 _md5Hasher = MD5.Create();
        private static readonly Dictionary<Type, Color> _typeColors = new Dictionary<Type, Color>();

        public static Color MapTypeToColor(Type type)
        {
            if (type == null)
            {
                return Color.white;
            }

            if (type.IsPrimitive)
            {
                return new Color(0.12f, 0.53f, 0.9f);
            }

            if (typeof(Object).IsAssignableFrom(type))
            {
                return new Color(0.9f, 0.23f, 0.39f);
            }

            if (type.IsValueType)
            {
                return NodeColors.Variable;
            }

            if (_typeColors.ContainsKey(type))
            {
                return _typeColors[type];
            }

            byte[] hashed = _md5Hasher.ComputeHash(type.ToString() == "T"
                ? Encoding.UTF8.GetBytes("T")
                : Encoding.UTF8.GetBytes(type.Name));
            int iValue = BitConverter.ToInt32(hashed, 0);

            //TODO: Make this provide more varied colors
            Color color = Color.HSVToRGB((iValue & 0xff) / 255f, .69f, 1f);


            _typeColors.Add(type, color);
            return color;
        }
        #endregion

        #region Documentation Utilities

        public static bool ShouldShowDocumentationLink(UdonNodeDefinition definition)
        {
        List<string> specialNames = new List<string>
            {
                "Block",
                "Branch",
                "For",
                "While",
                "Foreach",
                "Get_Variable",
                "Set_Variable",
                "Set_ReturnValue",
                "Event_Custom",
				"Event_OnAvatarChanged",
                "Event_OnAvatarEyeHeightChanged",
                "Event_OnDataStorageAdded",
                "Event_OnDataStorageChanged",
                "Event_OnDataStorageRemoved",
                "Event_OnDrop",
                "Event_Interact",
                "Event_OnNetworkReady",
                "Event_OnOwnershipTransferred",
                "Event_OnPickup",
                "Event_OnPickupUseDown",
                "Event_OnPickupUseUp",
                "Event_OnPlayerJoined",
                "Event_OnPlayerLeft",
                "Event_OnSpawn",
                "Event_OnStationEntered",
                "Event_OnStationExited",
                "Event_OnVideoEnd",
                "Event_OnVideoPause",
                "Event_OnVideoPlay",
                "Event_OnVideoStart",
                "Event_MidiNoteOn",
                "Event_MidiNoteOff",
                "Event_MidiControlChange",
                "VRCUdonCommonInterfacesIUdonEventReceiver.__SendCustomEvent__SystemString__SystemVoid",
                "VRCUdonCommonInterfacesIUdonEventReceiver.__SetHeapVariable__SystemString_SystemObject__SystemVoid",
                "VRCUdonCommonInterfacesIUdonEventReceiver.__GetHeapVariable__SystemString__SystemObject",
                "Const_VRCUdonCommonInterfacesIUdonEventReceiver",
                "Event_OnPurchaseConfirmed",
                "Event_OnPurchaseUse",
                "Event_OnPurchaseExpired",
                "Event_OnListPurchases",
                "Event_OnListAvailableProducts"
            };

                // Don't show for any of these
                return !(definition.type == null ||
                    definition.type.Namespace == null ||
                    specialNames.Contains(definition.fullName) ||
                    (!definition.type.Namespace.Contains("UnityEngine") &&
                    !definition.type.Namespace.Contains("System")));
        }

        public static string GetDocumentationLink(UdonNodeDefinition definition)
        {
            if (definition.fullName.StartsWithCached("Event_"))
            {
                string url = "https://docs.unity3d.com/2018.4/Documentation/ScriptReference/MonoBehaviour.";
                url += definition.name;
                url += ".html";
                return url;
            }

            if (definition.fullName.Contains("Array.__ctor"))
            {
                //I couldn't find the array constructor documentation
                return "https://docs.microsoft.com/en-us/dotnet/api/system.array?view=netframework-4.8";
            }

            if (definition.fullName.Contains("Array.__Get"))
            {
                return "https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/member-access-operators#indexer-operator-";
            }

            if (definition.fullName.Contains(".__Equals__SystemObject"))
            {
                return "https://docs.microsoft.com/en-us/dotnet/api/system.object.equals?view=netframework-4.8";
            }

            if (definition.name.Contains("[]"))
            {
                string url = "https://docs.microsoft.com/en-us/dotnet/api/system.array.";
                url += definition.name.Split(' ')[1];
                url += "?view=netframework-4.8";
                return url;
            }

            if (definition.type.Namespace.Contains("UnityEngine"))
            {
                string url = "https://docs.unity3d.com/2018.4/Documentation/ScriptReference/";
                if (definition.type.Namespace != "UnityEngine")
                {
                    url += definition.type.Namespace.Replace("UnityEngine.", "");
                    url += ".";
                }
                url += definition.type.Name;

                if (definition.fullName.Contains("__get_") || definition.fullName.Contains("__set_"))
                {
                    if (definition.fullName.Contains("__get_"))
                    {
                        url += "-" + definition.name.Split(new[] { "get_" }, StringSplitOptions.None)[1];
                    }
                    else
                    {
                        url += "-" + definition.name.Split(new[] { "set_" }, StringSplitOptions.None)[1];
                    }

                    url += ".html";
                    return url;
                }

                if (definition.fullName.Contains("Const_") || definition.fullName.Contains("Type_") || definition.fullName.Contains("Variable_"))
                {
                    url += ".html";
                    return url;
                }

                {
                    // Methods
                    url += "." + definition.name.Split(' ')[1];
                    url += ".html";
                    return url;
                }
            }

            if (definition.type.Namespace.Contains("System"))
            {
                string url = "https://docs.microsoft.com/en-us/dotnet/api/system.";
                url += definition.type.Name;
                if (definition.fullName.Contains("__get_") || definition.fullName.Contains("__set_"))
                {
                    url += "." + definition.name.Split(' ')[1].Replace("get_", "").Replace("set_", "");
                    url += "?view=netframework-4.8";
                    return url;
                }

                if (definition.name == "ctor")
                {
                    url += ".-ctor";
                    url += "?view=netframework-4.8#System_";
                    url += definition.type.Name + "__ctor_";
                    foreach (var pType in definition.Inputs)
                    {
                        url += "System_" + pType.type.Name.Replace('[', '_').Replace(']', '_') + "_";
                    }

                    return url;
                }

                if (definition.fullName.Contains("Const_") || definition.fullName.Contains("Type_"))
                {
                    url += "?view=netframework-4.8";
                    return url;
                }

                {
                    // Methods
                    // not entirely sure what case this catches, but we were always doing the split before, and it was breaking if we didn't have . in the name.
                    if (definition.name.Contains('.'))
                    {
                        url += "." + definition.name.Split(' ')[1];
                    }
                    url += "?view=netframework-4.8";
                    return url;
                }
            }

            return "";
        }

        #endregion

        public static string GetVariableChangeEventName(string variableName)
        {
            return UdonGraphCompiler.GetVariableChangeEventName(variableName);
        }

        public static string FriendlyNameify(this string typeString)
        {
            if (typeString == null)
            {
                return null;
            }

            if (FriendlyNameCache.ContainsKey(typeString))
            {
                return FriendlyNameCache[typeString];
            }
            string originalString = typeString;
            typeString = typeString.Replace("Single", "float");
            typeString = typeString.Replace("Int32", "int");
            typeString = typeString.Replace("String", "string");
            typeString = typeString.Replace("VRCstring", "VRCString");
            typeString = typeString.Replace("VRCUdonCommonInterfacesIUdonEventReceiver", "UdonBehaviour");
            typeString = typeString.Replace("UdonCommonInterfacesIUdonEventReceiver", "UdonBehaviour");
            typeString = typeString.Replace("IUdonEventReceiver", "UdonBehaviour");
            typeString = typeString.Replace("Const_VRCUdonCommonInterfacesIUdonEventReceiver", "UdonBehaviour");


            if(typeString != "SystemArray")
            {
                typeString = typeString.Replace("Array", "[]");
            }

            typeString = typeString.Replace("SDK3VideoComponentsBaseBase", "");
            typeString = typeString.Replace("SDK3stringLoading", "");
            typeString = typeString.Replace("SDK3Image", "");
            typeString = typeString.Replace("VRCSDK3Data", "");
            typeString = typeString.Replace("SDK3Data", "");
            typeString = typeString.Replace("SDKBase", "");
            typeString = typeString.Replace("SDK3Components", "");
            typeString = typeString.Replace("VRCVRC", "VRC");
            typeString = typeString.Replace("TMPro", "");
            typeString = typeString.Replace("VideoVideo", "Video");
            typeString = typeString.Replace("VRCUdonCommon", "");
            typeString = typeString.Replace("Shuffle[]", "ShuffleArray");
            typeString = typeString.Replace("Economy", "");
            typeString = typeString.Replace("RenderingPostProcessing", "");
            typeString = typeString.Replace("VRCSDK3Rendering", "");
            typeString = typeString.Replace("VRCSDK3PlatformScreenUpdateData", "ScreenUpdateData");
            // ReSharper disable once StringLiteralTypo
            if (typeString.Replace("ector", "").Contains("ctor")) //Handle "Vector/vector"
            {
                typeString = typeString.ReplaceLast("ctor", "constructor");
            }

            if (typeString == "IUdonEventReceiver")
            {
                typeString = "UdonBehaviour";
            }
            FriendlyNameCache.Add(originalString, typeString);
            return typeString;
        }

        private static readonly Dictionary<(string s, string prefix), bool> StartsWithCache;
        public static bool StartsWithCached(this string s, string prefix)
        {
            if (StartsWithCache.ContainsKey((s, prefix)))
            {
                return StartsWithCache[(s, prefix)];
            }
            bool doesStartWith = s.StartsWith(prefix);
            StartsWithCache.Add((s, prefix), doesStartWith);
            return doesStartWith;
        }

        public static string UppercaseFirst(this string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        public static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search, StringComparison.Ordinal);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        public static Type SlotTypeConverter(Type type, string fullName)
        {
            if (type == null)
            {
                return typeof(object);
            }

            if (fullName.Contains("IUdonEventReceiver") && type == typeof(Object))
            {
                return typeof(UdonBehaviour);
            }

            return type;
        }

        public static string FriendlyTypeName(Type t)
        {
            if (t == null)
            {
                return "Flow";
            }

            if (!t.IsPrimitive)
            {
                if (t == typeof(Object))
                {
                    return "Unity Object";
                }
                return t.Name;
            }
            using (CSharpCodeProvider provider = new CSharpCodeProvider())
            {
                CodeTypeReference typeRef = new CodeTypeReference(t);
                return provider.GetTypeOutput(typeRef);
            }
        }

        public static string ReplaceLast(this string source, string find, string replace)
        {
            int place = source.LastIndexOf(find, StringComparison.Ordinal);

            if (place == -1)
                return source;

            string result = source.Remove(place, find.Length).Insert(place, replace);
            return result;
        }

        public static string PrettyBaseName(string baseIdentifier)
        {
            string result = baseIdentifier.Replace("UnityEngine", "").Replace("System", "");
            string[] resultSplit = result.Split(new[] { "__" }, StringSplitOptions.None);
            if (resultSplit.Length >= 2)
            {
                result = $"{resultSplit[0]}{resultSplit[1]}";
            }
            result = result.FriendlyNameify();
            result = result.Replace("op_", "");
            result = result.Replace("_", " ");
            return result;
        }

        public static string PrettyString(string s)
        {
            switch (s)
            {
                case "op_Equality":
                    s = "==";
                    break;

                case "op_Inequality":
                    s = "!=";
                    break;

                case "op_Addition":
                    s = "+";
                    break;
                case "VRCUdonCommonInterfacesIUdonEventReceiver":
                    s = "UdonBehaviour";
                    break;
                // ReSharper disable once RedundantEmptySwitchSection
                default:
                    break;
            }
            return s;
        }

        public static string ParseByCase(string strInput)
        {
            string strOutput = "";
            int intCurrentCharPos = 0;
            int intLastCharPos = strInput.Length - 1;
            for (intCurrentCharPos = 0; intCurrentCharPos <= intLastCharPos; intCurrentCharPos++)
            {
                char chrCurrentInputChar = strInput[intCurrentCharPos];
                char chrPreviousInputChar = chrCurrentInputChar;
                if (intCurrentCharPos > 0)
                {
                    chrPreviousInputChar = strInput[intCurrentCharPos - 1];
                }

                if (char.IsUpper(chrCurrentInputChar) && char.IsLower(chrPreviousInputChar))
                {
                    strOutput += " ";
                }

                strOutput += chrCurrentInputChar;
            }

            return strOutput;
        }

        public static string PrettyFullName(UdonNodeDefinition nodeDefinition, bool keepLong = false)
        {
            string fullName = nodeDefinition.fullName;
            string result;
            if (keepLong)
            {
                result = fullName.Replace("UnityEngine", "UnityEngine.").Replace("System", "System.");
            }
            else
            {
                result = fullName.Replace("UnityEngine", "").Replace("System", "");
            }

            string[] resultSplit = result.Split(new[] { "__" }, StringSplitOptions.None);
            if (resultSplit.Length >= 3)
            {
                string outName = "";
                if (nodeDefinition.type != typeof(void))
                {
                    if (nodeDefinition.Outputs.Count > 0)
                    {
                        outName = string.Join(", ", nodeDefinition.Outputs.Select(o => o.name));
                    }
                }

                result = nodeDefinition.Inputs.Count > 0
                    ? $"{resultSplit[0]}{resultSplit[1]}({string.Join(", ", nodeDefinition.Inputs.Select(s => s.name))}{outName})"
                    : $"{resultSplit[0]}{resultSplit[1]}({resultSplit[2].Replace("_", ", ")}{outName})";
            }
            else if (resultSplit.Length >= 2)
            {
                result = $"{resultSplit[0]}{resultSplit[1]}()";
            }

            if (!keepLong)
            {
                result = result.FriendlyNameify();
                result = result.Replace("op_", "");
                result = result.Replace("_", " ");
            }

            return result;
        }

        public static string GetSimpleNameForRegistry(INodeRegistry registry)
        {
            string registryName = registry.ToString().Replace("NodeRegistry", "").FriendlyNameify();
            registryName = registryName.Substring(registryName.LastIndexOf(".") + 1);
            registryName = registryName.Replace("UnityEngine", "");
            return registryName;
        }

        private static Dictionary<UdonNodeDefinition, INodeRegistry> _definitionToRegistryLookup;
        public static INodeRegistry GetRegistryForDefinition(UdonNodeDefinition definition)
        {
            // Create lookup if needed
            if(_definitionToRegistryLookup == null)
            {
                _definitionToRegistryLookup = new Dictionary<UdonNodeDefinition, INodeRegistry>();

                foreach (var registry in UdonEditorManager.Instance.GetNodeRegistries())
                {
                    foreach (var nodeDefinition in registry.Value.GetNodeDefinitions())
                    {
                        _definitionToRegistryLookup.Add(nodeDefinition, registry.Value);
                    }
                }
            }

            // Return found Registry or Null
            if (_definitionToRegistryLookup.ContainsKey(definition))
            {
                return _definitionToRegistryLookup[definition];
            }
            else
            {
                return null;
            }
        }

        public static string ToLowerFirstChar(this string input)
        {
            string newString = input;
            if (!String.IsNullOrEmpty(newString) && Char.IsUpper(newString[0]))
                newString = Char.ToLower(newString[0]) + newString.Substring(1);
            return newString;
        }

        public static string SanitizeVariableName(this string result)
        {
            result = result.Replace(" ", "");
            result = result.Replace("[]", "Array");
            if (char.IsNumber(result[0]))
            {
                result = $"A{result}";
            }
            Regex rgx = new Regex("[^a-zA-Z0-9 _]");
            result = rgx.Replace(result, "");
            return result;
        }

        public static char EscapeLikeALiteral(string src)
        {
            switch (src)
            {
                //case "\\'": return '\'';
                //case "\\"": return '\"';
                case "\\0": return '\0';
                case "\\a": return '\a';
                case "\\b": return '\b';
                case "\\f": return '\f';
                case "\\n": return '\n';
                case "\\r": return '\r';
                case "\\t": return '\t';
                case "\\v": return '\v';
                case "\\": return '\\';
                default:
                    throw new InvalidOperationException($"src was {src}");
            }
        }

        public static string UnescapeLikeALiteral(char src)
        {
            switch (src)
            {
                //case "\\'": return '\'';
                //case "\\"": return '\"';
                case '\0': return "\\0";
                case '\a': return "\\a";
                case '\b': return "\\b";
                case '\f': return "\\f";
                case '\n': return "\\n";
                case '\r': return "\\r";
                case '\t': return "\\t";
                case '\v': return "\\v";
                //case '\\': return "\\";
                default:
                    return src.ToString();
            }
        }
    }
}
