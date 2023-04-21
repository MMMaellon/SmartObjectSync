Shader "VRChat/Examples/MiniMap/MiniMap Blit"
{
    Properties
    {
        _MainTex ("Blit Source (Map Capture)", 2D) = "white" {}
        [HDR]_Color("Map Tint", Color) = (1,1,1,1)
        
        [Header(Local Player)]
        [HideInInspector]_PlayerPos ("Player Position", Vector) = (0,0,0,0)
        [HDR]_PlayerDotColor("Player Dot Color", Color) = (1,1,1,1)
        _PlayerDotSize("Player Dot Size", Float) = 1
        
        [Header(Map Movement)]
        [ToggleUI]_FollowPlayer("Follow Player", Int) = 1
        _ZoomLevel("Zoom Level", Range(1, 10)) = 3
        
        [Header(Other Players)]
        [Toggle(SHOW_OTHERS)]_ShowOthers("Show Other Players", Int) = 0
        [IntRange]_MaxPlayers("Maximum Players", Range(0, 82)) = 80
        [HDR]_OtherPlayersDotColor("Other Players Dot Color", Color) = (1,1,1,1)
        _OtherPlayersDotSize("Other Players Dot Size", Float) = 1
        
        [HideInInspector]_CameraSize("Camera Size", Float) = 100
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "IgnoreProjector"="True" "PreviewType"="Plane" }
        ZTest Always // ZTest Always is required when running on quest

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ SHOW_OTHERS // we use multi_compile so Udon can change this keyword at runtime

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _PlayerPos;
            half4 _PlayerDotColor;
            half _PlayerDotSize;
            int _MaxPlayers;
            half4 _OtherPlayersDotColor;
            half _OtherPlayersDotSize;
            int _FollowPlayer;
            half _ZoomLevel;

            // Since we can't resize the array, we'll always use the max size and iterate up to _MaxPlayers
            uniform float4 _PlayerPositions[82];
            uniform float4x4 _CameraMatrix;
            float _CameraSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float circleSdf(float2 p, float2 c, float r)
            {
                return length(p - c) - r;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // This converts the player position in world-space to 0-1 UV space
                _PlayerPos.w = 1;
                float4 localPlayer = mul(_CameraMatrix, _PlayerPos);
                localPlayer /= _CameraSize;
                localPlayer.xyz += 0.5;

                // This re-centers the UVs and zooms the map on the player position
                if (_FollowPlayer)
                {
                    i.uv -= 0.5;
                    i.uv /= _ZoomLevel;
                    i.uv += localPlayer.xy;
                }
                fixed4 col = tex2D(_MainTex, i.uv);
                float playerDist = circleSdf(i.uv, localPlayer.xy, _PlayerDotSize / 100.0);

                // To avoid unnecessary code for self-only minimaps
                // we keyword-out the other players distance calculations
                #if defined(SHOW_OTHERS)
                    float othersDist = 1000.0;
                    half otherSize = _OtherPlayersDotSize / 100.0;
                
                    for (int j = 0; j < _MaxPlayers; j++)
                    {
                        // we store the "is this player in game" flag in the w component
                        if (_PlayerPositions[j].w < 1) continue;
                        
                        float4 playerPos = mul(_CameraMatrix, _PlayerPositions[j]);
                        playerPos /= _CameraSize;
                        playerPos.xyz += 0.5;
                        othersDist = min(othersDist, circleSdf(i.uv, playerPos.xy, otherSize));
                    }

                    if (othersDist < 0)
                    {
                        col = lerp(col, _OtherPlayersDotColor, smoothstep(0.0, -0.003, othersDist));
                    }
                #endif
                
                if (playerDist < 0)
                {
                    // We use smoothstep here to make a smooth gradient instead of a hard circle
                    col = lerp(col, _PlayerDotColor, smoothstep(0, -0.003, playerDist));
                }
                return col;
            }
            ENDCG
        }
    }
}
