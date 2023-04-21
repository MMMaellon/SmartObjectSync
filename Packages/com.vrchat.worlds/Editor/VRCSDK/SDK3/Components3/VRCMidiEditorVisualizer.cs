using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Midi;

public class VRCMidiEditorVisualizer : EditorWindow
{
    private MidiFile _midiFile;
    private Vector2 _scroll;
    private bool _hasGenerated;
    private float _maxDistanceNotes;

    private PreviewRenderUtility _previewRenderUtility;
    private Texture _outputTexture;
    private Mesh _meshMidi;
    private Mesh _meshSideNotes;
    private Mesh _meshTime;
    private Material _mainMaterial;
    private List<int> _usedChannels;
    private Dictionary<int, Color> _colors;

    public static void Init(MidiFile midiFile)
    {
        // Get existing open window or if none, make a new one:
        VRCMidiEditorVisualizer window = GetWindow<VRCMidiEditorVisualizer>();
        window._midiFile = midiFile;
        window.Show();
    }

    private void OnEnable()
    {
        _previewRenderUtility = new PreviewRenderUtility(false);
        var camera = _previewRenderUtility.camera;
        camera.orthographic = true;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 10;
        camera.orthographicSize = 20f;
        camera.transform.position = new Vector3(0f, 2f, 0f);
        camera.transform.LookAt(Vector3.zero);

        InititlizeData();
        wantsMouseMove = true;
    }

    private void InititlizeData()
    {
        _mainMaterial = new Material(Shader.Find("VRChat/Mobile/Toon Lit"));

        _meshMidi = new Mesh();
        _meshSideNotes = new Mesh();
        _meshTime = new Mesh();

        _hasGenerated = false;
    }

    public void OnDisable()
    {
        if (_previewRenderUtility != null)
        {
            _previewRenderUtility.Cleanup();
        }

        if (_meshMidi != null)
        {
            DestroyImmediate(_meshMidi);
            DestroyImmediate(_meshSideNotes);
            DestroyImmediate(_meshTime);
            DestroyImmediate(_mainMaterial);
        }
    }

    void OnGUI()
    {
        if (_midiFile == null)
        {
            EditorGUILayout.HelpBox("Editor was created without Midi File.", MessageType.Error);
            return;
        }


        if (_meshMidi == null)
        {
            InititlizeData();
        }


        if (!_hasGenerated)
        {
            GenerateNoteObject();
            GenerateMeshTime();
        }

        GenerateSideNotes();
        ShowChannelLegend();

        EditorGUILayout.BeginHorizontal();

        // Main Note area
        _scroll = EditorGUILayout.BeginScrollView(_scroll, false, false, GUILayout.ExpandWidth(true));
        Rect rect = GUILayoutUtility.GetRect(_maxDistanceNotes*10, 1280);
        EditorGUILayout.EndScrollView();

        rect.width = position.width-16;
        rect.height = position.height - EditorGUIUtility.singleLineHeight*2 -3;
        rect.y += EditorGUIUtility.singleLineHeight+3;

        var camera = _previewRenderUtility.camera;
        camera.orthographicSize = rect.height / 20;
        camera.transform.position = new Vector3(rect.width / 20 + _scroll.x / 10, 2, 128 - rect.height / 20 - _scroll.y / 10);

        _previewRenderUtility.BeginPreview(rect, GUIStyle.none);
        _previewRenderUtility.DrawMesh(_meshTime, Matrix4x4.identity, _mainMaterial, 0);
        _previewRenderUtility.DrawMesh(_meshMidi,Matrix4x4.identity,_mainMaterial,0);
        _previewRenderUtility.DrawMesh(_meshSideNotes, Vector3.right * (_scroll.x / 10), Quaternion.identity, _mainMaterial,0);
        _previewRenderUtility.camera.Render();
        _previewRenderUtility.EndAndDrawPreview(rect);
        

        EditorGUILayout.EndHorizontal();

        if (Event.current.type == EventType.MouseMove)
            Repaint();
    }

    private void ShowChannelLegend()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Channels:");
        foreach (int channel in _usedChannels)
        {
            EditorGUILayout.LabelField(channel.ToString(),  new GUIStyle(){alignment = TextAnchor.MiddleCenter, normal = new GUIStyleState(){textColor = _colors[channel]}}, new GUILayoutOption[]{GUILayout.Width(20)});
        }
        EditorGUILayout.EndHorizontal();
    }

    public void GenerateNoteObject()
    {
        _meshMidi.Clear();
        _usedChannels = new List<int>();

        MidiData data = _midiFile.data;

        List<Vector3> positions = new List<Vector3>();
        List<int> tri = new List<int>();
        List<Color> colors = new List<Color>();
        
        // Iterate over the list once to get all the used channels
        foreach (var track in data.tracks)
        {
            foreach (var block in track.blocks)
            {
                if(!_usedChannels.Contains(block.channel)) _usedChannels.Add(block.channel);
            }
        }

        // Generate colors for all channels, spread across spectrum evenly
        _colors = new Dictionary<int, Color>();
        for (int i = 0; i < _usedChannels.Count; i++)
        {
            float normalizedPosition = Mathf.InverseLerp(0, _usedChannels.Count, i);
            _colors.Add(_usedChannels[i], Color.HSVToRGB(normalizedPosition, .9f, .9f));
        }

        int index = 0;

        for (int TrackIndex = 0; TrackIndex < data.tracks.Length; TrackIndex++)
        {
            MidiData.MidiTrack track = data.tracks[TrackIndex];
            MidiData.MidiBlock[] blocks = track.blocks;

            for(int blockIndex = 0; blockIndex < blocks.Length; blockIndex++)
            {
                MidiData.MidiBlock block = blocks[blockIndex];
                float endTime = block.endTimeMs / 100 + 10;
                if ( _maxDistanceNotes < endTime)
                {
                    _maxDistanceNotes = endTime;
                }

                positions.Add(new Vector3((block.startTimeMs / 100f) + 10, 0, block.note ));
                positions.Add(new Vector3(endTime, 0, block.note ));
                positions.Add(new Vector3((block.startTimeMs / 100f) + 10, 0, block.note + 1f));
                positions.Add(new Vector3(endTime, 0, block.note + 1f));

                tri.Add(index);
                tri.Add(index + 2);
                tri.Add(index + 1);
                tri.Add(index + 1);
                tri.Add(index + 2);
                tri.Add(index + 3);
                index += 4;

                colors.Add(_colors[block.channel]);
                colors.Add(_colors[block.channel]);
                colors.Add(_colors[block.channel]);
                colors.Add(_colors[block.channel]);
                
                if(!_usedChannels.Contains(block.channel)) _usedChannels.Add(block.channel);
            }
        }


        var camera = _previewRenderUtility.camera;
        camera.transform.position = new Vector3(_maxDistanceNotes/2, 2f, 64);

        _meshMidi.vertices = positions.ToArray();
        _meshMidi.triangles = tri.ToArray();
        _meshMidi.colors = colors.ToArray();
        _hasGenerated = true;
    }

    public void GenerateSideNotes()
    {

        _meshSideNotes.Clear();

        List<Vector3> positions = new List<Vector3>();
        List<int> tri = new List<int>();
        List<Color> colors = new List<Color>();

        int index = 0;
        int skip = -1;
        bool whiteNote = true;

        Color color;
        for (int Notes = 0; Notes < 128; Notes++)
        {
            skip++;
            skip %= 12;

            if (skip == 5 || skip == 0)
            {
                whiteNote = !whiteNote;
            }

            whiteNote = !whiteNote;

            if (whiteNote)
            {
                positions.Add(new Vector3(0, 0.3f, Notes + 0.05f));
                positions.Add(new Vector3(10, 0.3f, Notes + 0.05f));
                positions.Add(new Vector3(0, 0.3f, Notes + 0.95f));
                positions.Add(new Vector3(10, 0.3f, Notes + 0.95f));

                color = Color.white;
            }
            else
            {
                positions.Add(new Vector3(0, 0.3f, Notes + 0.05f));
                positions.Add(new Vector3(10, 0.3f, Notes + 0.05f));
                positions.Add(new Vector3(0, 0.3f, Notes + 0.95f));
                positions.Add(new Vector3(10, 0.3f, Notes + 0.95f));
                color = Color.black;

            }

            if(Mathf.FloorToInt((1280 - (GUIUtility.GUIToScreenPoint(Event.current.mousePosition).y + _scroll.y - position.y - EditorGUIUtility.singleLineHeight*2.3f))/ 10) == Notes)
            {
                color = Color.gray;
            }

            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);

            tri.Add(index);
            tri.Add(index + 2);
            tri.Add(index + 1);
            tri.Add(index + 1);
            tri.Add(index + 2);
            tri.Add(index + 3);
            index += 4;
        }

        _meshSideNotes.vertices = positions.ToArray();
        _meshSideNotes.triangles = tri.ToArray();
        _meshSideNotes.colors = colors.ToArray();
    }

    private void GenerateMeshTime()
    {
        _meshTime.Clear();

        List<Vector3> positions = new List<Vector3>();
        List<int> tri = new List<int>();
        List<Color> colors = new List<Color>();

        int index = 0;
        int amount = Mathf.CeilToInt(((_maxDistanceNotes-10) / 600) * _midiFile.data.bpm);

        Color color = Color.black;
        for (int beats = 0; beats < amount; beats++)
        {
            float offset = (beats * (600f / _midiFile.data.bpm)) + 10;
            positions.Add(new Vector3(-0.1f + offset, -0.3f, 0));
            positions.Add(new Vector3(0.1f + offset, -0.3f, 0));
            positions.Add(new Vector3(-0.1f + offset, -0.3f, 128));
            positions.Add(new Vector3(0.1f + offset, -0.3f, 128));

            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);

            tri.Add(index);
            tri.Add(index + 2);
            tri.Add(index + 1);
            tri.Add(index + 1);
            tri.Add(index + 2);
            tri.Add(index + 3);
            index += 4;
        }

        _meshTime.vertices = positions.ToArray();
        _meshTime.triangles = tri.ToArray();
        _meshTime.colors = colors.ToArray();
    }
}
