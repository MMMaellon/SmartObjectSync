#define ENABLE_PARALLEL_PRELOAD
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Udon.ClientBindings;
using VRC.Udon.ClientBindings.Interfaces;
using VRC.Udon.Common;
using VRC.Udon.Common.Enums;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Serialization.OdinSerializer;
using VRC.Udon.Serialization.OdinSerializer.Utilities;
using Logger = VRC.Core.Logger;
using Object = UnityEngine.Object;

#if VRC_CLIENT
using VRC.Core;
using VRC.Core.Pool;
#endif

#if ENABLE_PARALLEL_PRELOAD
using System.Threading.Tasks;
#endif

namespace VRC.Udon
{
    [AddComponentMenu("")]
    public class UdonManager : MonoBehaviour, IUdonClientInterface
    {
        public static event Action<IUdonProgram> OnUdonProgramLoaded;

        public UdonBehaviour currentlyExecuting;

        #region Singleton

        private static UdonManager _instance;

        [PublicAPI]
        public static UdonManager Instance
        {
            get
            {
                #if !VRC_CLIENT
                if(_instance != null)
                {
                    return _instance;
                }

                GameObject udonManagerGameObject = new GameObject("UdonManager");
                DontDestroyOnLoad(udonManagerGameObject);
                _instance = udonManagerGameObject.AddComponent<UdonManager>();
                #endif

                return _instance;
            }
        }

        #endregion

        private static readonly UpdateOrderComparer _udonBehaviourUpdateOrderComparer = new UpdateOrderComparer();

        private bool _isUdonEnabled = true;

        private readonly Dictionary<Scene, Dictionary<GameObject, HashSet<UdonBehaviour>>>
            _sceneUdonBehaviourDirectories =
                new Dictionary<Scene, Dictionary<GameObject, HashSet<UdonBehaviour>>>();

        #region Private Update Data

        private readonly SortedSet<UdonBehaviour> _updateUdonBehaviours =
            new SortedSet<UdonBehaviour>(_udonBehaviourUpdateOrderComparer);

        private readonly SortedSet<UdonBehaviour> _lateUpdateUdonBehaviours =
            new SortedSet<UdonBehaviour>(_udonBehaviourUpdateOrderComparer);

        private readonly SortedSet<UdonBehaviour> _fixedUpdateUdonBehaviours =
            new SortedSet<UdonBehaviour>(_udonBehaviourUpdateOrderComparer);

        private readonly SortedSet<UdonBehaviour> _postLateUpdateUdonBehaviours =
            new SortedSet<UdonBehaviour>(_udonBehaviourUpdateOrderComparer);

        private readonly Queue<(UdonBehaviour udonBehaviour, bool newState)> _updateUdonBehavioursRegistrationQueue =
            new Queue<(UdonBehaviour udonBehaviour, bool newState)>();

        private readonly Queue<(UdonBehaviour udonBehaviour, bool newState)> _lateUpdateUdonBehavioursRegistrationQueue
            = new Queue<(UdonBehaviour udonBehaviour, bool newState)>();

        private readonly Queue<(UdonBehaviour udonBehaviour, bool newState)> _fixedUpdateUdonBehavioursRegistrationQueue
            = new Queue<(UdonBehaviour udonBehaviour, bool newState)>();

        private readonly Queue<(UdonBehaviour udonBehaviour, bool newState)> _postLateUpdateUdonBehavioursRegistrationQueue
            = new Queue<(UdonBehaviour udonBehaviour, bool newState)>();

        private PostLateUpdater _postLateUpdater;

        #endregion

        #region Private Input Data

        private readonly Dictionary<string, HashSet<UdonBehaviour>> _inputUdonBehaviours =
            new Dictionary<string, HashSet<UdonBehaviour>>();

        private readonly Queue<(UdonBehaviour udonBehaviour, string udonEventName, bool newState)>
            _inputUpdateUdonBehavioursRegistrationQueue =
                new Queue<(UdonBehaviour udonBehaviour, string udonEventName, bool newState)>();

        #endregion

        #region Constants

        [PublicAPI]
        public const string UDON_EVENT_ONPLAYERRESPAWN = "_onPlayerRespawn";
        
        [PublicAPI]
        public const string UDON_EVENT_ONSCREENUPDATE = "_onScreenUpdate";

        #region Input Actions and Axes

        private readonly HashSet<string> _inputActionNames = new HashSet<string>()
        {
            UDON_INPUT_JUMP,
            UDON_INPUT_USE,
            UDON_INPUT_GRAB,
            UDON_INPUT_DROP,
            UDON_MOVE_VERTICAL,
            UDON_MOVE_HORIZONTAL,
            UDON_LOOK_VERTICAL,
            UDON_LOOK_HORIZONTAL
        };

        // Buttons
        [PublicAPI]
        public const string UDON_INPUT_JUMP = "_inputJump";

        [PublicAPI]
        public const string UDON_INPUT_USE = "_inputUse";

        [PublicAPI]
        public const string UDON_INPUT_GRAB = "_inputGrab";

        [PublicAPI]
        public const string UDON_INPUT_DROP = "_inputDrop";

        // Axes
        [PublicAPI]
        public const string UDON_MOVE_VERTICAL = "_inputMoveVertical";

        [PublicAPI]
        public const string UDON_MOVE_HORIZONTAL = "_inputMoveHorizontal";

        [PublicAPI]
        public const string UDON_LOOK_VERTICAL = "_inputLookVertical";

        [PublicAPI]
        public const string UDON_LOOK_HORIZONTAL = "_inputLookHorizontal";

        #endregion

        #endregion

        private readonly IUdonClientInterface _udonClientInterface = new UdonClientInterface();
        private UdonTimeSource _udonTimeSource;
        private IUdonEventScheduler _udonEventScheduler;

        #region SDK Only Methods

        #if !VRC_CLIENT
        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            UdonManager udonManager = Instance;
            List<UdonBehaviour> udonBehavioursWorkingList = new List<UdonBehaviour>();
            int sceneCount = SceneManager.sceneCount;
            for(int i = 0; i < sceneCount; ++i)
            {
                Scene currentScene = SceneManager.GetSceneAt(i);
                if(!currentScene.isLoaded)
                {
                    continue;
                }

                foreach(GameObject rootObject in currentScene.GetRootGameObjects())
                {
                    rootObject.GetComponentsInChildren(true, udonBehavioursWorkingList);
                    foreach(UdonBehaviour udonBehaviour in udonBehavioursWorkingList)
                    {
                        udonManager.RegisterUdonBehaviour(udonBehaviour);
                    }
                }
            }
        }
        #endif

        #endregion

        #region Unity Event Methods

        public void Awake()
        {
            if(_instance == null)
            {
                _instance = this;
            }

            DebugLogging = Application.isEditor;

            if(Instance != this)
            {
                if(Application.isPlaying)
                {
                    Destroy(this);
                }
                else
                {
                    DestroyImmediate(this);
                }

                return;
            }

            _udonTimeSource = new UdonTimeSource();
            _udonEventScheduler = new UdonEventScheduler(_udonTimeSource);
            _postLateUpdater = gameObject.AddComponent<PostLateUpdater>();
            _postLateUpdater.udonManager = this;
            if(!Application.isPlaying)
            {
                return;
            }

            #if !VRC_CLIENT
            PrimitiveType[] primitiveTypes = (PrimitiveType[])Enum.GetValues(typeof(PrimitiveType));
            foreach(PrimitiveType primitiveType in primitiveTypes)
            {
                GameObject go = GameObject.CreatePrimitive(primitiveType);
                Mesh primitiveMesh = go.GetComponent<MeshFilter>().sharedMesh;
                Destroy(go);
                Blacklist(primitiveMesh);
            }
            #endif
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void Update()
        {
            _udonTimeSource.UpdateTime(Time.deltaTime);

            bool anyNull = false;
            foreach(UdonBehaviour udonBehaviour in _updateUdonBehaviours)
            {
                if(udonBehaviour == null)
                {
                    anyNull = true;
                    continue;
                }

                udonBehaviour.ManagedUpdate();
            }

            while(_updateUdonBehavioursRegistrationQueue.Count > 0)
            {
                (UdonBehaviour udonBehaviour, bool newState) = _updateUdonBehavioursRegistrationQueue.Dequeue();
                if(newState)
                {
                    _updateUdonBehaviours.Add(udonBehaviour);
                }
                else
                {
                    _updateUdonBehaviours.Remove(udonBehaviour);
                }
            }

            if(anyNull)
            {
                _updateUdonBehaviours.RemoveWhere(o => o == null);
            }

            UpdateInputQueue();
            _udonEventScheduler.RunScheduledEvents(EventTiming.Update);
        }

        private void LateUpdate()
        {
            bool anyNull = false;
            foreach(UdonBehaviour udonBehaviour in _lateUpdateUdonBehaviours)
            {
                if(udonBehaviour == null)
                {
                    anyNull = true;
                    continue;
                }

                udonBehaviour.ManagedLateUpdate();
            }

            while(_lateUpdateUdonBehavioursRegistrationQueue.Count > 0)
            {
                (UdonBehaviour udonBehaviour, bool newState) = _lateUpdateUdonBehavioursRegistrationQueue.Dequeue();
                if(newState)
                {
                    _lateUpdateUdonBehaviours.Add(udonBehaviour);
                }
                else
                {
                    _lateUpdateUdonBehaviours.Remove(udonBehaviour);
                }
            }

            if(anyNull)
            {
                _lateUpdateUdonBehaviours.RemoveWhere(o => o == null);
            }

            _udonEventScheduler.RunScheduledEvents(EventTiming.LateUpdate);
        }

        private void FixedUpdate()
        {
            bool anyNull = false;
            foreach(UdonBehaviour udonBehaviour in _fixedUpdateUdonBehaviours)
            {
                if(udonBehaviour == null)
                {
                    anyNull = true;
                    continue;
                }

                udonBehaviour.ManagedFixedUpdate();
            }

            while(_fixedUpdateUdonBehavioursRegistrationQueue.Count > 0)
            {
                (UdonBehaviour udonBehaviour, bool newState) = _fixedUpdateUdonBehavioursRegistrationQueue.Dequeue();
                if(newState)
                {
                    _fixedUpdateUdonBehaviours.Add(udonBehaviour);
                }
                else
                {
                    _fixedUpdateUdonBehaviours.Remove(udonBehaviour);
                }
            }

            if(anyNull)
            {
                _fixedUpdateUdonBehaviours.RemoveWhere(o => o == null);
            }
        }

        internal void PostLateUpdate()
        {
            bool anyNull = false;
            foreach(UdonBehaviour udonBehaviour in _postLateUpdateUdonBehaviours)
            {
                if(udonBehaviour == null)
                {
                    anyNull = true;
                    continue;
                }

                udonBehaviour.PostLateUpdate();
            }

            while(_postLateUpdateUdonBehavioursRegistrationQueue.Count > 0)
            {
                (UdonBehaviour udonBehaviour, bool newState) = _postLateUpdateUdonBehavioursRegistrationQueue.Dequeue();
                if(newState)
                {
                    _postLateUpdateUdonBehaviours.Add(udonBehaviour);
                }
                else
                {
                    _postLateUpdateUdonBehaviours.Remove(udonBehaviour);
                }

                _postLateUpdater.enabled = _postLateUpdateUdonBehaviours.Count != 0;
            }

            if(anyNull)
            {
                _postLateUpdateUdonBehaviours.RemoveWhere(o => o == null);
            }
        }

        #endregion

        #region Input Methods

        [PublicAPI]
        public void RegisterInput(UdonBehaviour udonBehaviour, string udonEventName, bool doRegister)
        {
            _inputUpdateUdonBehavioursRegistrationQueue.Enqueue((udonBehaviour, udonEventName, doRegister));
        }

        [PublicAPI]
        public void RunInputAction(string inputEvent, UdonInputEventArgs args)
        {
            if(!_inputUdonBehaviours.TryGetValue(inputEvent, out HashSet<UdonBehaviour> udonBehaviours))
            {
                return;
            }

            foreach(UdonBehaviour udonBehaviour in udonBehaviours)
            {
                // need to use this equals style, just checking if(udonBehaviour) does not return correctly
                if(udonBehaviour == null)
                {
                    _inputUpdateUdonBehavioursRegistrationQueue.Enqueue((udonBehaviour, inputEvent, false));
                    continue;
                }

                // Easier to check here than adding / removing from lookup
                if(udonBehaviour.enabled)
                {
                    udonBehaviour.RunInputEvent(inputEvent, args);
                }
            }
        }

        private void UpdateInputQueue()
        {
            while(_inputUpdateUdonBehavioursRegistrationQueue.Count > 0)
            {
                // Get next item in queue
                (UdonBehaviour udonBehaviour, string eventName, bool newState) =
                    _inputUpdateUdonBehavioursRegistrationQueue.Dequeue();

                // Skip if this is not an input event
                if(!(_inputActionNames.Contains(eventName)))
                {
                    continue;
                }

                // Needs to be added to lookup
                if(newState)
                {
                    // Add to existing set
                    if(_inputUdonBehaviours.TryGetValue(eventName, out HashSet<UdonBehaviour> udonBehaviours))
                    {
                        udonBehaviours.Add(udonBehaviour);
                    }
                    // Or create new one with this UdonBehaviour in it
                    else
                    {
                        _inputUdonBehaviours.Add(eventName, new HashSet<UdonBehaviour>() { udonBehaviour });
                    }
                }
                // Needs to be removed from lookup
                else
                {
                    if(_inputUdonBehaviours.TryGetValue(eventName, out HashSet<UdonBehaviour> udonBehaviours))
                    {
                        udonBehaviours.Remove(udonBehaviour);
                    }
                }
            }
        }

        #endregion

        #region Scene Load Methods

        private ProfilerMarker _preloadProfilerMarker = new ProfilerMarker("UdonManager.OnSceneLoaded Preload");
        private ProfilerMarker _initializeProfilerMarker = new ProfilerMarker("UdonManager.OnSceneLoaded Initialize");

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            try
            {
                if(loadSceneMode == LoadSceneMode.Single)
                {
                    _sceneUdonBehaviourDirectories.Clear();
                }

                Dictionary<GameObject, HashSet<UdonBehaviour>> sceneUdonBehaviourDirectory =
                    new Dictionary<GameObject, HashSet<UdonBehaviour>>();

#if !VRC_CLIENT
                List<Transform> transformsTempList = new List<Transform>();
#endif
                foreach(GameObject rootGameObject in scene.GetRootGameObjects())
                {
#if VRC_CLIENT
                    using (rootGameObject.GetComponentsInChildrenPooled(out List<Transform> transformsTempList, true))
#else
                    rootGameObject.GetComponentsInChildren(true, transformsTempList);
#endif
                    {
                        foreach(Transform currentTransform in transformsTempList)
                        {
                            GameObject currentGameObject = currentTransform.gameObject;
#if VRC_CLIENT
                            using (currentGameObject.GetComponentsPooled(out List<UdonBehaviour> currentGameObjectUdonBehaviours))
#else
                            List<UdonBehaviour> currentGameObjectUdonBehaviours = new List<UdonBehaviour>();
                            currentGameObject.GetComponents(currentGameObjectUdonBehaviours);
#endif
                            {
                                if(currentGameObjectUdonBehaviours.Count > 0)
                                {
                                    sceneUdonBehaviourDirectory.Add(
                                        currentGameObject,
                                        new HashSet<UdonBehaviour>(currentGameObjectUdonBehaviours));
                                }
                            }
                        }
                    }
                }

                if(!_isUdonEnabled)
                {
                    Logger.LogWarning(
                        "Udon is disabled globally, Udon components will be removed from the scene.");

                    foreach(HashSet<UdonBehaviour> udonBehaviours in sceneUdonBehaviourDirectory.Values)
                    {
                        foreach(UdonBehaviour udonBehaviour in udonBehaviours)
                        {
                            Destroy(udonBehaviour);
                        }
                    }

                    return;
                }

                _sceneUdonBehaviourDirectories.Add(scene, sceneUdonBehaviourDirectory);

                // Initialize Event Queues - we don't want any cached UdonBehaviours or Events from previous scenes
                _updateUdonBehaviours.Clear();
                _lateUpdateUdonBehaviours.Clear();
                _fixedUpdateUdonBehaviours.Clear();
                _postLateUpdateUdonBehaviours.Clear();
                _postLateUpdater.enabled = false;
                _updateUdonBehavioursRegistrationQueue.Clear();
                _lateUpdateUdonBehavioursRegistrationQueue.Clear();
                _fixedUpdateUdonBehavioursRegistrationQueue.Clear();
                _postLateUpdateUdonBehavioursRegistrationQueue.Clear();
                _inputUdonBehaviours.Clear();
                _inputUpdateUdonBehavioursRegistrationQueue.Clear();
                _udonEventScheduler.ClearScheduledEvents();

                #if ENABLE_PARALLEL_PRELOAD
                Parallel.ForEach(
                    sceneUdonBehaviourDirectory.Values,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = 3
                    }, 
                    udonBehaviourList =>
                    {
                        foreach(UdonBehaviour udonBehaviour in udonBehaviourList)
                        {
                            using(_preloadProfilerMarker.Auto())
                            {
                                udonBehaviour.PreloadUdonProgram();
                            }
                        }
                    });
                #else
                foreach(HashSet<UdonBehaviour> udonBehaviourList in sceneUdonBehaviourDirectory.Values)
                {
                    foreach(UdonBehaviour udonBehaviour in udonBehaviourList)
                    {
                        using(_preloadProfilerMarker.Auto())
                        {
                            udonBehaviour.PreloadUdonProgram();
                        }
                    }
                }
                #endif

                // Initialize all UdonBehaviours in the scene so their Public Variables are populated.
                foreach(HashSet<UdonBehaviour> udonBehaviourList in sceneUdonBehaviourDirectory.Values)
                {
                    foreach(UdonBehaviour udonBehaviour in udonBehaviourList)
                    {
                        // All UdonBehaviours that exist in the scene get networking setup automatically.
                        udonBehaviour.IsNetworkingSupported = true;
                        using(_initializeProfilerMarker.Auto())
                        {
                            udonBehaviour.InitializeUdonContent();
                        }
                    }
                }
            }
            finally
            {
                PurgeSerializationCaches();
                Logger.Log($"UdonManager.OnSceneLoaded took '{timer.Elapsed.TotalSeconds:N3}'");
            }
        }

        [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
        public void ProcessUdonProgram(IUdonProgram udonProgram)
        {
            OnUdonProgramLoaded?.Invoke(udonProgram);
        }

        private void OnSceneUnloaded(Scene scene)
        {
            PurgeSerializationCaches();
            _sceneUdonBehaviourDirectories.Remove(scene);
        }
        
        private void PurgeSerializationCaches()
        {
            Cache<DeserializationContext>.Purge();
            Cache<SerializationContext>.Purge();
            Cache<BinaryDataReader>.Purge();
            Cache<UnityReferenceResolver>.Purge();
            Cache<BinaryDataWriter>.Purge();
        }

        public ulong GetTotalLoadedProgramSize()
        {
            ulong totalSize = 0L;

#if VRC_CLIENT
            using (HashSetPool.Get(out HashSet<int> observedProgramIds))
#else
            HashSet<int> observedProgramIds = new HashSet<int>();
#endif
            {
                foreach(Dictionary<GameObject, HashSet<UdonBehaviour>> sceneUdonBehaviourDirectory in _sceneUdonBehaviourDirectories.Values)
                {
                    foreach(HashSet<UdonBehaviour> udonBehaviourList in sceneUdonBehaviourDirectory.Values)
                    {
                        foreach(UdonBehaviour udonBehaviour in udonBehaviourList)
                        {
                            if(udonBehaviour != null)
                            {
                                int programId = udonBehaviour.ProgramId;
                                if (programId != 0 && !observedProgramIds.Contains(programId))
                                {
                                    totalSize += udonBehaviour.ProgramSize;
                                    observedProgramIds.Add(udonBehaviour.ProgramId);
                                }
                            }
                        }
                    }
                }
            }

            return totalSize;
        }

        #endregion

        #region Update Registration Methods

        internal void RegisterUdonBehaviourUpdate(UdonBehaviour udonBehaviour) => _updateUdonBehavioursRegistrationQueue.Enqueue((udonBehaviour, true));
        internal void RegisterUdonBehaviourLateUpdate(UdonBehaviour udonBehaviour) => _lateUpdateUdonBehavioursRegistrationQueue.Enqueue((udonBehaviour, true));
        internal void RegisterUdonBehaviourFixedUpdate(UdonBehaviour udonBehaviour) => _fixedUpdateUdonBehavioursRegistrationQueue.Enqueue((udonBehaviour, true));

        internal void RegisterUdonBehaviourPostLateUpdate(UdonBehaviour udonBehaviour)
        {
            _postLateUpdater.enabled = true;
            _postLateUpdateUdonBehavioursRegistrationQueue.Enqueue((udonBehaviour, true));
        }

        internal void UnregisterUdonBehaviourUpdate(UdonBehaviour udonBehaviour) => _updateUdonBehavioursRegistrationQueue.Enqueue((udonBehaviour, false));
        internal void UnregisterUdonBehaviourLateUpdate(UdonBehaviour udonBehaviour) => _lateUpdateUdonBehavioursRegistrationQueue.Enqueue((udonBehaviour, false));
        internal void UnregisterUdonBehaviourFixedUpdate(UdonBehaviour udonBehaviour) => _fixedUpdateUdonBehavioursRegistrationQueue.Enqueue((udonBehaviour, false));
        internal void UnregisterUdonBehaviourPostLateUpdate(UdonBehaviour udonBehaviour) => _postLateUpdateUdonBehavioursRegistrationQueue.Enqueue((udonBehaviour, false));

        #endregion

        #region Event Scheduler Methods

        [PublicAPI]
        public void ScheduleDelayedEvent(IUdonEventReceiver eventReceiver, string eventName, float delaySeconds, EventTiming eventTiming) =>
            _udonEventScheduler.ScheduleDelayedSecondsEvent(eventReceiver, eventName, delaySeconds, eventTiming);

        [PublicAPI]
        public void ScheduleDelayedEvent(IUdonEventReceiver eventReceiver, string eventName, int delayFrames, EventTiming eventTiming) =>
            _udonEventScheduler.ScheduleDelayedFramesEvent(eventReceiver, eventName, delayFrames, eventTiming);

        #endregion

        #region Control Methods

        [PublicAPI]
        public void SetUdonEnabled(bool isEnabled)
        {
            _isUdonEnabled = isEnabled;
        }

        #endregion

        #region IUdonClientInterface Methods

        public bool DebugLogging
        {
            get => _udonClientInterface.DebugLogging;
            set => _udonClientInterface.DebugLogging = value;
        }

        public IUdonVM ConstructUdonVM()
        {
            return !_isUdonEnabled ? null : _udonClientInterface.ConstructUdonVM();
        }

        public void FilterBlacklisted<T>(ref T objectToFilter) where T : class
        {
            _udonClientInterface.FilterBlacklisted(ref objectToFilter);
        }

        public void Blacklist(Object objectToBlacklist, bool includeParents = true)
        {
            _udonClientInterface.Blacklist(objectToBlacklist, includeParents);
        }

        public void Blacklist(IEnumerable<Object> objectsToBlacklist, bool includeParents = true)
        {
            _udonClientInterface.Blacklist(objectsToBlacklist, includeParents);
        }

        public void FilterBlacklisted(ref Object objectToFilter)
        {
            _udonClientInterface.FilterBlacklisted(ref objectToFilter);
        }

        public void CleanBlacklist()
        {
            _udonClientInterface.CleanBlacklist();
        }

        public bool IsBlacklisted(Object objectToCheck)
        {
            return _udonClientInterface.IsBlacklisted(objectToCheck);
        }

        public bool IsBlacklisted<T>(T objectToCheck)
        {
            return _udonClientInterface.IsBlacklisted(objectToCheck);
        }

        public IUdonWrapper GetWrapper()
        {
            return _udonClientInterface.GetWrapper();
        }

        #endregion

        [PublicAPI]
        public void RegisterUdonBehaviour(UdonBehaviour udonBehaviour)
        {
            GameObject udonBehaviourGameObject = udonBehaviour.gameObject;
            Scene udonBehaviourScene = udonBehaviourGameObject.scene;
            if(!_sceneUdonBehaviourDirectories.TryGetValue(
                   udonBehaviourScene,
                   out Dictionary<GameObject, HashSet<UdonBehaviour>> sceneUdonBehaviourDirectory))
            {
                sceneUdonBehaviourDirectory = new Dictionary<GameObject, HashSet<UdonBehaviour>>();
                _sceneUdonBehaviourDirectories.Add(udonBehaviourScene, sceneUdonBehaviourDirectory);
            }

            if(sceneUdonBehaviourDirectory.TryGetValue(
                   udonBehaviourGameObject,
                   out HashSet<UdonBehaviour> gameObjectUdonBehaviours))
            {
                gameObjectUdonBehaviours.Add(udonBehaviour);
            }
            else
            {
                gameObjectUdonBehaviours = new HashSet<UdonBehaviour> { udonBehaviour };
                sceneUdonBehaviourDirectory.Add(udonBehaviourGameObject, gameObjectUdonBehaviours);
            }

            udonBehaviour.InitializeUdonContent();
        }

        public List<UdonBehaviour> UdonBehavioursInScene = new List<UdonBehaviour>();
        [PublicAPI]
        public List<UdonBehaviour> GetUdonBehavioursInScene()
        {
            UdonBehavioursInScene.Clear();
            foreach(Dictionary<GameObject, HashSet<UdonBehaviour>> sceneUdonBehaviourDirectory in
                    _sceneUdonBehaviourDirectories.Values)
            {
                foreach(HashSet<UdonBehaviour> udonBehaviourList in sceneUdonBehaviourDirectory.Values)
                {
                    foreach(UdonBehaviour udonBehaviour in udonBehaviourList)
                    {
                        if(udonBehaviour != null)
                        {
                            UdonBehavioursInScene.Add(udonBehaviour);
                        }
                    }
                }
            }
            return UdonBehavioursInScene;
        }

        #region Global RunEvent Methods

        //Run an udon event on all objects
        [PublicAPI]
        public void RunEvent(string eventName)
        {
            foreach(Dictionary<GameObject, HashSet<UdonBehaviour>> sceneUdonBehaviourDirectory in
                    _sceneUdonBehaviourDirectories.Values)
            {
                foreach(HashSet<UdonBehaviour> udonBehaviourList in sceneUdonBehaviourDirectory.Values)
                {
                    foreach(UdonBehaviour udonBehaviour in udonBehaviourList)
                    {
                        if(udonBehaviour != null)
                        {
                            udonBehaviour.RunEvent(eventName);
                        }
                    }
                }
            }
        }

        [PublicAPI]
        public void RunEvent<T0>(string eventName, (string symbolName, T0 value) parameter0)
        {
            foreach(Dictionary<GameObject, HashSet<UdonBehaviour>> sceneUdonBehaviourDirectory in
                _sceneUdonBehaviourDirectories.Values)
            {
                foreach(HashSet<UdonBehaviour> udonBehaviourList in sceneUdonBehaviourDirectory.Values)
                {
                    foreach(UdonBehaviour udonBehaviour in udonBehaviourList)
                    {
                        if(udonBehaviour != null)
                        {
                            udonBehaviour.RunEvent(eventName, parameter0);
                        }
                    }
                }
            }
        }

        [PublicAPI]
        public void RunEvent<T0, T1>(string eventName, (string symbolName, T0 value) parameter0, (string symbolName, T1 value) parameter1)
        {
            foreach(Dictionary<GameObject, HashSet<UdonBehaviour>> sceneUdonBehaviourDirectory in
                _sceneUdonBehaviourDirectories.Values)
            {
                foreach(HashSet<UdonBehaviour> udonBehaviourList in sceneUdonBehaviourDirectory.Values)
                {
                    foreach(UdonBehaviour udonBehaviour in udonBehaviourList)
                    {
                        if(udonBehaviour != null)
                        {
                            udonBehaviour.RunEvent(eventName, parameter0, parameter1);
                        }
                    }
                }
            }
        }

        [PublicAPI]
        public void RunEvent<T0, T1, T2>(string eventName, (string symbolName, T0 value) parameter0, (string symbolName, T1 value) parameter1, (string symbolName, T2 value) parameter2)
        {
            foreach(Dictionary<GameObject, HashSet<UdonBehaviour>> sceneUdonBehaviourDirectory in
                _sceneUdonBehaviourDirectories.Values)
            {
                foreach(HashSet<UdonBehaviour> udonBehaviourList in sceneUdonBehaviourDirectory.Values)
                {
                    foreach(UdonBehaviour udonBehaviour in udonBehaviourList)
                    {
                        if(udonBehaviour != null)
                        {
                            udonBehaviour.RunEvent(eventName, parameter0, parameter1, parameter2);
                        }
                    }
                }
            }
        }

        [PublicAPI]
        public void RunEvent(string eventName, params (string symbolName, object value)[] eventParameters)
        {
            foreach(Dictionary<GameObject, HashSet<UdonBehaviour>> sceneUdonBehaviourDirectory in
                _sceneUdonBehaviourDirectories.Values)
            {
                foreach(HashSet<UdonBehaviour> udonBehaviourList in sceneUdonBehaviourDirectory.Values)
                {
                    foreach(UdonBehaviour udonBehaviour in udonBehaviourList)
                    {
                        if(udonBehaviour != null)
                        {
                            udonBehaviour.RunEvent(eventName, eventParameters);
                        }
                    }
                }
            }
        }

        //Run an udon event on a specific gameObject
        [PublicAPI]
        public void RunEvent(GameObject eventReceiverObject, string eventName)
        {
            if(!_sceneUdonBehaviourDirectories.TryGetValue(
                   eventReceiverObject.scene,
                   out Dictionary<GameObject, HashSet<UdonBehaviour>> sceneUdonBehaviourDirectory))
            {
                return;
            }

            if(!sceneUdonBehaviourDirectory.TryGetValue(
                   eventReceiverObject,
                   out HashSet<UdonBehaviour> eventReceiverBehaviourList))
            {
                return;
            }

            foreach(UdonBehaviour udonBehaviour in eventReceiverBehaviourList)
            {
                udonBehaviour.RunEvent(eventName);
            }
        }

        [PublicAPI]
        public void RunEvent<T0>(GameObject eventReceiverObject, string eventName, (string symbolName, T0 value) parameter0)
        {
            if(!_sceneUdonBehaviourDirectories.TryGetValue(
                eventReceiverObject.scene,
                out Dictionary<GameObject, HashSet<UdonBehaviour>> sceneUdonBehaviourDirectory))
            {
                return;
            }

            if(!sceneUdonBehaviourDirectory.TryGetValue(
                eventReceiverObject,
                out HashSet<UdonBehaviour> eventReceiverBehaviourList))
            {
                return;
            }

            foreach(UdonBehaviour udonBehaviour in eventReceiverBehaviourList)
            {
                udonBehaviour.RunEvent(eventName, parameter0);
            }
        }

        [PublicAPI]
        public void RunEvent<T0, T1>(GameObject eventReceiverObject, string eventName, (string symbolName, T0 value) parameter0, (string symbolName, T1 value) parameter1)
        {
            if(!_sceneUdonBehaviourDirectories.TryGetValue(
                eventReceiverObject.scene,
                out Dictionary<GameObject, HashSet<UdonBehaviour>> sceneUdonBehaviourDirectory))
            {
                return;
            }

            if(!sceneUdonBehaviourDirectory.TryGetValue(
                eventReceiverObject,
                out HashSet<UdonBehaviour> eventReceiverBehaviourList))
            {
                return;
            }

            foreach(UdonBehaviour udonBehaviour in eventReceiverBehaviourList)
            {
                udonBehaviour.RunEvent(eventName, parameter0, parameter1);
            }
        }

        [PublicAPI]
        public void RunEvent<T0, T1, T2>(GameObject eventReceiverObject,
            string eventName,
            (string symbolName, T0 value) parameter0,
            (string symbolName, T1 value) parameter1,
            (string symbolName, T2 value) parameter2)
        {
            if(!_sceneUdonBehaviourDirectories.TryGetValue(
                eventReceiverObject.scene,
                out Dictionary<GameObject, HashSet<UdonBehaviour>> sceneUdonBehaviourDirectory))
            {
                return;
            }

            if(!sceneUdonBehaviourDirectory.TryGetValue(
                eventReceiverObject,
                out HashSet<UdonBehaviour> eventReceiverBehaviourList))
            {
                return;
            }

            foreach(UdonBehaviour udonBehaviour in eventReceiverBehaviourList)
            {
                udonBehaviour.RunEvent(eventName, parameter0, parameter1, parameter2);
            }
        }

        [PublicAPI]
        public void RunEvent(GameObject eventReceiverObject, string eventName, params (string symbolName, object value)[] eventParameters)
        {
            if(!_sceneUdonBehaviourDirectories.TryGetValue(
                eventReceiverObject.scene,
                out Dictionary<GameObject, HashSet<UdonBehaviour>> sceneUdonBehaviourDirectory))
            {
                return;
            }

            if(!sceneUdonBehaviourDirectory.TryGetValue(
                eventReceiverObject,
                out HashSet<UdonBehaviour> eventReceiverBehaviourList))
            {
                return;
            }

            foreach(UdonBehaviour udonBehaviour in eventReceiverBehaviourList)
            {
                udonBehaviour.RunEvent(eventName, eventParameters);
            }
        }

        #endregion

        #region Helper Classes

        private class UpdateOrderComparer : IComparer<UdonBehaviour>
        {
            public int Compare(UdonBehaviour x, UdonBehaviour y)
            {
                if(x == null)
                {
                    return y != null ? -1 : 0;
                }

                if(y == null)
                {
                    return 1;
                }

                int updateOrderComparison = x.UpdateOrder.CompareTo(y.UpdateOrder);
                if(updateOrderComparison != 0)
                {
                    return updateOrderComparison;
                }

                return x.GetInstanceID().CompareTo(y.GetInstanceID());
            }
        }


        private class UdonTimeSource : IUdonEventSchedulerTimeSource
        {
            public double CurrentTime { get; private set; } = 0;
            public long CurrentFrame { get; private set; } = 0;

            public float MinimumDelay => 0.001f;

            public void UpdateTime(float deltaTime)
            {
                CurrentTime += deltaTime;
                CurrentFrame++;
            }
        }

        #endregion
    }
}
