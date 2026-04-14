Shader "Custom/OneSidedSprite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        
        _RoundedCorner ("Rounded Corner", Vector) = (0,0,0,0)
        _BorderWidth ("Border Width", Float) = 0
        _BorderColor ("Border Color", Color) = (0,0,0,0)
        _Width ("Width", Float) = 100
        _Height ("Height", Float) = 100
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float4 _RoundedCorner; // x=TopLeft, y=TopRight, z=BottomRight, w=BottomLeft
            float _BorderWidth;
            float4 _BorderColor;
            float _Width;
            float _Height;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            // Signed Distance Function for a box with variable rounded corners
            float sdRoundedBox(float2 p, float2 b, float4 r)
            {
                r.xy = (p.x > 0.0) ? r.yz : r.xw; // Select right or left corners
                r.x  = (p.y > 0.0) ? r.x  : r.y;  // Select top or bottom
                
                float2 q = abs(p) - b + r.x;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r.x;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd);
                
                // Use the color passed from C# as the base tint, combined with vertex color
                color *= IN.color;

                // Calculate SDF
                // UV is 0..1, convert to centered coordinates relative to pixel size
                float2 uv = IN.texcoord * float2(_Width, _Height);
                float2 center = float2(_Width * 0.5, _Height * 0.5);
                float2 pos = uv - center;
                float2 halfSize = center;

                // _RoundedCorner: x=TopLeft, y=TopRight, z=BottomRight, w=BottomLeft
                // SDF function expects: x=TopRight, y=BottomRight, z=TopLeft, w=BottomLeft (usually)
                // Let's map our corners to the SDF logic.
                // Our Logic in vert/frag:
                // pos.x > 0 is Right, pos.y > 0 is Top (assuming UV 0,0 is Bottom Left? Unity UI default)
                // Unity UI UV: (0,0) is Bottom-Left, (1,1) is Top-Right.
                
                // C# sends: x=TL, y=TR, z=BR, w=BL
                // sdRoundedBox logic:
                // if x>0 (Right) -> use yz (TR, BR)
                // if x<0 (Left)  -> use xw (TL, BL)
                // Then:
                // if y>0 (Top) -> use first of pair (TR or TL)
                // if y<0 (Bottom) -> use second of pair (BR or BL)
                
                // Mapping:
                // x>0, y>0 (Top Right) -> Needs TR (y)
                // x>0, y<0 (Bottom Right) -> Needs BR (z)
                // x<0, y>0 (Top Left) -> Needs TL (x)
                // x<0, y<0 (Bottom Left) -> Needs BL (w)
                
                // Re-packing for sdRoundedBox:
                // The function I wrote:
                // r.xy = (p.x > 0.0) ? r.yz : r.xw; -> Right: (TR, BR) | Left: (TL, BL) -> Wait, r passed is (TL, TR, BR, BL)
                // if x>0: (TR, BR) matches r.yz correctly.
                // if x<0: (TL, BL) matches r.xw correctly.
                // Then r.x = (p.y > 0.0) ? r.x : r.y;
                // if x>0 (Right): Pair is (TR, BR). y>0 (Top) -> TR. y<0 (Bottom) -> BR. Correct.
                // if x<0 (Left): Pair is (TL, BL). y>0 (Top) -> TL. y<0 (Bottom) -> BL. Correct.
                
                // So the mapping is correct assuming C# sends (TL, TR, BR, BL) and UV is standard.
                
                float dist = sdRoundedBox(pos, halfSize, _RoundedCorner);

                // Anti-aliasing for the main shape
                float alpha = 1.0 - smoothstep(0.0, 1.0, dist);
                
                // Clip
                color.a *= alpha;
                
                // Border
                if (_BorderWidth > 0)
                {
                    // Inside border
                    // Distance is negative inside.
                    // Border area is where dist is between -_BorderWidth and 0.
                    // Or simply: mix based on distance.
                    
                    float borderAlpha = 1.0 - smoothstep(_BorderWidth - 1.0, _BorderWidth, abs(dist)); 
                    // Wait, this is for a stroke centered on the edge.
                    // Usually border is "inside".
                    // If dist is 0 (edge), we want border.
                    // If dist is -BorderWidth, we stop border.
                    
                    // Simple mix:
                    // If dist > -_BorderWidth: it's border or outside.
                    // Since we already clipped outside (alpha), we only care about inside.
                    
                    float insideBorder = 1.0 - smoothstep(-_BorderWidth - 0.5, -_BorderWidth + 0.5, dist);
                    
                    // insideBorder is 1 for inner content, 0 for border area.
                    color = lerp(_BorderColor, color, insideBorder);
                    // Ensure border color alpha is applied
                    color.a = alpha * (insideBorder * color.a + (1-insideBorder) * _BorderColor.a); 
                    // Actually simpler:
                    // If we are in the border region (-BorderWidth < dist < 0)
                    if (dist > -_BorderWidth && dist <= 0) {
                         color = _BorderColor;
                         color.a *= alpha; // Anti-aliasing from outer edge
                    }
                }

                // Unity UI Clipping
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                // Always clip transparent pixels to support Mask component (Stencil)
                // If we don't clip, the invisible parts of the quad still write to Stencil, making the mask rectangular.
                clip (color.a - 0.001);

                return color;
            }
            ENDCG
        }
    }
}
