using UnityEngine;
using System;
using System.Collections.Generic;

namespace VRC.Udon
{
    public static class UdonNetworkTypes
    {
        public static bool CanSync(Type type) => _syncTypes.Contains(type);
        public static bool CanSyncLinear(Type type) => _linearTypes.Contains(type);
        public static bool CanSyncSmooth(Type type) => _smoothTypes.Contains(type);

        private static readonly HashSet<Type> _syncTypes = new HashSet<Type>{
            typeof(bool),
            typeof(char),
            typeof(byte),
            typeof(uint),
            typeof(int),
            typeof(long),
            typeof(sbyte),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(short),
            typeof(ushort),
            typeof(string),
            typeof(bool[]),
            typeof(char[]),
            typeof(byte[]),
            typeof(uint[]),
            typeof(int[]),
            typeof(long[]),
            typeof(sbyte[]),
            typeof(ulong[]),
            typeof(float[]),
            typeof(double[]),
            typeof(short[]),
            typeof(ushort[]),
            typeof(string[]),
            typeof(Color),
            typeof(Color32),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Quaternion),
            typeof(Vector2[]),
            typeof(Vector3[]),
            typeof(Vector4[]),
            typeof(Quaternion[]),
            typeof(Color[]),
            typeof(Color32[]),
            typeof(SDKBase.VRCUrl),
            typeof(SDKBase.VRCUrl[]),
        };

        private static readonly HashSet<Type> _linearTypes = new HashSet<Type>{
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(uint),
            typeof(int),
            typeof(ulong),
            typeof(long),
            typeof(float),
            typeof(double),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Quaternion),
            typeof(Color),
            typeof(Color32),
        };

        private static readonly HashSet<Type> _smoothTypes = new HashSet<Type> {
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(uint),
            typeof(int),
            typeof(ulong),
            typeof(long),
            typeof(float),
            typeof(double),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Quaternion),
        };
    }
}
