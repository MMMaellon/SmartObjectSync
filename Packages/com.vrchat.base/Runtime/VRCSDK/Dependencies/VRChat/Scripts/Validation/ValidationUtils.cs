using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEngine;
using System.Reflection;

namespace VRC.SDKBase.Validation
{
    public static class ValidationUtils
    {
        private static string _EDITOR_ONLY_TAG = "EditorOnly";

        private static bool IsEditorOnly(Component component)
        {
            if (component.CompareTag(_EDITOR_ONLY_TAG))
            {
                return true;
            }

            foreach (var t in component.transform.GetComponentsInParent<Transform>(true))
            {
                if (t.CompareTag(_EDITOR_ONLY_TAG))
                {
                    return true;
                }
            }
            return false;
        }

        public static List<T> GetComponentsInChildrenExcludingEditorOnly<T>(this GameObject target, bool includeInactive) where T : Component
        {
            List<T> foundComponents = new List<T>();
            T[] allComponents = target.GetComponentsInChildren<T>(includeInactive);
            foreach (T component in allComponents)
            {
                if (component == null || IsEditorOnly(component))
                {
                    continue;
                }

                foundComponents.Add(component);
            }

            return foundComponents;
        }

        public static List<Component> GetComponentsInChildrenExcludingEditorOnly(this GameObject target, Type type, bool includeInactive)
        {
            List<Component> foundComponents = new List<Component>();
            Component[] allComponents = target.GetComponentsInChildren(type, includeInactive);
            foreach (Component component in allComponents)
            {
                if (component == null || IsEditorOnly(component))
                {
                    continue;
                }

                foundComponents.Add(component);
            }

            return foundComponents;
        }

        public static void RemoveIllegalComponents(GameObject target, HashSet<Type> whitelist, bool retry = true, bool onlySceneObjects = false, bool logStripping = true)
        {
            List<Component> foundComponents = FindIllegalComponents(target, whitelist);
            foreach(Component component in foundComponents)
            {
                if(component == null)
                {
                    continue;
                }
                
                if(onlySceneObjects && component.GetInstanceID() < 0)
                {
                    continue;
                }

                if(logStripping)
                {
                    Core.Logger.LogWarning($"Removing {component.GetType().Name} comp from {component.gameObject.name}");
                }

                RemoveComponent(component);
            }
        }

        public static List<Component> FindIllegalComponents(GameObject target, HashSet<Type> whitelist)
        {
            List<Component> components = target.GetComponentsInChildrenExcludingEditorOnly<Component>(true);
            for (int i = components.Count - 1; i >= 0; i--)
            {
                Component component = components[i];
                if (component == null || whitelist.Contains(component.GetType()) || component is IEditorOnly)
                {
                    components.RemoveAt(i);
                }
            }
            return components;
        }

        private static readonly Dictionary<string, HashSet<Type>> _whitelistCache = new Dictionary<string, HashSet<Type>>();
        public static HashSet<Type> WhitelistedTypes(string whitelistName, IEnumerable<string> componentTypeWhitelist)
        {
            if (_whitelistCache.ContainsKey(whitelistName))
            {
                return _whitelistCache[whitelistName];
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            HashSet<Type> whitelist = new HashSet<Type>();
            foreach(string whitelistedTypeName in componentTypeWhitelist)
            {
                Type whitelistedType = TypeUtils.GetTypeFromName(whitelistedTypeName, assemblies);
                if(whitelistedType == null)
                {
                    continue;
                }

                if(whitelist.Contains(whitelistedType))
                {
                    continue;
                }

                whitelist.Add(whitelistedType);
            }

            AddDerivedClasses(whitelist);

            _whitelistCache[whitelistName] = whitelist;

            return _whitelistCache[whitelistName];
        }

        public static HashSet<Type> WhitelistedTypes(string whitelistName, IEnumerable<Type> componentTypeWhitelist)
        {
            if (_whitelistCache.ContainsKey(whitelistName))
            {
                return _whitelistCache[whitelistName];
            }

            HashSet<Type> whitelist = new HashSet<Type>();
            whitelist.UnionWith(componentTypeWhitelist);

            AddDerivedClasses(whitelist);

            _whitelistCache[whitelistName] = whitelist;

            return _whitelistCache[whitelistName];
        }

        private static void AddDerivedClasses(HashSet<Type> whitelist)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach(Assembly assembly in assemblies)
            {
                foreach(Type type in assembly.GetTypes())
                {
                    if(whitelist.Contains(type))
                    {
                        continue;
                    }

                    if(!typeof(Component).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    Type currentType = type;
                    while(currentType != typeof(object) && currentType != null)
                    {
                        if(whitelist.Contains(currentType))
                        {
                            whitelist.Add(type);
                            break;
                        }

                        currentType = currentType.BaseType;
                    }
                }
            }
        }

        private static readonly Dictionary<Type, ImmutableArray<RequireComponent>> _requireComponentsCache = new Dictionary<Type, ImmutableArray<RequireComponent>>();
        private static void RemoveDependencies(Component rootComponent)
        {
            if (rootComponent == null)
            {
                return;
            }

            Component[] components = rootComponent.GetComponents<Component>();
            if (components == null || components.Length == 0)
            {
                return;
            }

            Type compType = rootComponent.GetType();
            foreach (var siblingComponent in components)
            {
                if (siblingComponent == null)
                {
                    continue;
                }

                Type siblingComponentType = siblingComponent.GetType();
                if(!_requireComponentsCache.TryGetValue(siblingComponentType, out ImmutableArray<RequireComponent> requiredComponentAttributes))
                {
                    requiredComponentAttributes = siblingComponentType.GetCustomAttributes(typeof(RequireComponent), true).Cast<RequireComponent>().ToImmutableArray();
                    _requireComponentsCache.Add(siblingComponentType, requiredComponentAttributes);
                }

                bool deleteMe = false;
                foreach (RequireComponent requireComponent in requiredComponentAttributes)
                {
                    if (requireComponent == null)
                    {
                        continue;
                    }

                    bool needsDeletion(Type reqType)
                    {
                        if (reqType == null) return false;

                        // check if the rootComponent fullfills this type requirement of siblingComponent
                        if (!reqType.IsAssignableFrom(compType)) return false;

                        // check if we're _the only one_ fullfilling it, as there might be subclasses of the base reqType we do allow
                        // in which case we don't need to remove this sibling
                        var fullfilledByOthers = false;
                        foreach (var candidate in components)
                        {
                            if (candidate == null) continue;
                            if (candidate == siblingComponent) continue;
                            if (candidate == rootComponent) continue;
                            if (reqType.IsAssignableFrom(candidate.GetType()))
                                fullfilledByOthers = true;
                        }
                        if (fullfilledByOthers) return false; // something else fullfills us, we can stay

                        return true;
                    }

                    deleteMe |= needsDeletion(requireComponent.m_Type0);
                    deleteMe |= deleteMe || needsDeletion(requireComponent.m_Type1);
                    deleteMe |= deleteMe || needsDeletion(requireComponent.m_Type2);

                    if (deleteMe) break;
                }

                if (deleteMe && siblingComponent != rootComponent)
                {
                    RemoveComponent(siblingComponent);
                }
            }
        }

        public static void RemoveComponent(Component comp)
        {
            if (comp == null)
            {
                return;
            }

            RemoveDependencies(comp);

#if VRC_CLIENT
            UnityEngine.Object.DestroyImmediate(comp, true);
#else
            UnityEngine.Object.DestroyImmediate(comp, false);
#endif
        }

        public static void RemoveComponentsOfType<T>(GameObject target) where T : Component
        {
            if (target == null)
            {
                return;
            }

            foreach (T comp in target.GetComponentsInChildren<T>(true))
            {
                if (comp == null)
                {
                    continue;
                }

                RemoveComponent(comp);
            }
        }
    }
}