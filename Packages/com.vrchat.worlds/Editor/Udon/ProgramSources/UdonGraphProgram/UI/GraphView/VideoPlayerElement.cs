
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
#if UNITY_2019_3_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#else
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#endif

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class VideoPlayerElement : VisualElement
    {
        private VideoPlayer _player;
        private Scene _tempScene;
        private Toolbar _toolbar;
        private TextElement _header;
        private Image _videoProxy;

        public VideoPlayerElement()
        {
            // Constructing Items
            _header = new TextElement()
            {
                text = "Using Focused Search",
                name = "header",
            };

            _videoProxy = new Image()
            {
                name = "video-proxy",
                image = Resources.Load<Texture2D>("videoStill"),
                scaleMode = ScaleMode.ScaleToFit,
            };

            _toolbar = new Toolbar();
            _toolbar.Add(new ToolbarButton(Play) {text = "Play", name = "button-play"});
            _toolbar.Add(new ToolbarButton(Pause) {text = "Pause", name = "button-pause"});

            // Adding Items
            Add(_header);
            Add(_videoProxy);
            Add(_toolbar);
        }

        private void OnEnable()
        {
            ShowFrame();
            RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        private void OnDisable()
        {
            UnregisterCallback<MouseDownEvent>(OnMouseDown);
            if (_tempScene != null && _tempScene.IsValid())
            {
                EditorSceneManager.UnloadSceneAsync(_tempScene);
            }
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            PlayPauseToggle();
        }

        public void PlayPauseToggle()
        {
            if (GetCurrentPlayer().isPlaying)
            {
                Pause();
            }
            else if (GetCurrentPlayer().isPaused)
            {
                Play();
            }
        }

        public void Play()
        {
            GetCurrentPlayer().Play();
        }

        public void Pause()
        {
            GetCurrentPlayer().Pause();
        }

        public void LoadVideo(string url)
        {
            var player = GetCurrentPlayer();
            player.url = url;
            player.sendFrameReadyEvents = true;
            player.frameReady += OnFrameReady;
            player.isLooping = true;
            ShowFrame();
        }

        public void ShowFrame()
        {
            var player = GetCurrentPlayer();
            if (player == null || string.IsNullOrEmpty(player.url))
            {
                return;
            }

            player.frameReady += PauseOnNextFrame;
            player.Play();
        }

        public void UnloadVideo()
        {
            var player = GetCurrentPlayer();
            player.url = null;
            player.sendFrameReadyEvents = false;
            player.frameReady -= OnFrameReady;
        }

        private void OnFrameReady(VideoPlayer source, long frameIdx)
        {
            _videoProxy.image = source.texture;
            MarkDirtyRepaint();
        }

        private void PauseOnNextFrame(VideoPlayer source, long frameIdx)
        {
            var player = GetCurrentPlayer();
            player.frameReady -= PauseOnNextFrame;
            player.Pause();
        }

        private Scene GetTempScene()
        {
            if (_tempScene == null)
            {
                _tempScene = EditorSceneManager.NewPreviewScene();
                var root = new GameObject("VideoPlayer");
                EditorSceneManager.MoveGameObjectToScene(root, _tempScene);
            }

            return _tempScene;
        }

        private VideoPlayer GetCurrentPlayer()
        {
            // Try to get player from scene if it's not cached
            if (_player == null)
            {
                _player = GetTempScene().GetRootGameObjects()[0].GetComponent<VideoPlayer>();

                // Make new player if it doesn't exist in the scene
                if (_player == null)
                {
                    _player = GetTempScene().GetRootGameObjects()[0].AddComponent<VideoPlayer>();
                    _player.renderMode = VideoRenderMode.APIOnly;
                    _player.source = VideoSource.Url;
                    _player.audioOutputMode = VideoAudioOutputMode.None;
                    _player.playOnAwake = false;
                    _player.Prepare();
                }
            }

            return _player;
        }


        public new class UxmlFactory : UxmlFactory<VideoPlayerElement, UxmlTraits>
        {
        }

#if UNITY_2019_3_OR_NEWER
        public new class UxmlTraits : UnityEngine.UIElements.UxmlTraits
#else
        public new class UxmlTraits : UnityEngine.Experimental.UIElements.UxmlTraits
#endif
        {
            UxmlStringAttributeDescription m_Url = new UxmlStringAttributeDescription
                {name = "url-attr", defaultValue = ""};

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription { get; }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var ate = ve as VideoPlayerElement;

                ate.Clear();

                ate.urlAttr = m_Url.GetValueFromBag(bag, cc);
            }
        }

        public string urlAttr { get; set; }
    }
}