Shader "AVProVideo/Internal/UI/Transparent Packed (stereo)"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		[PerRendererData] _ChromaTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		_ColorMask ("Color Mask", Float) = 15
		_ClipRect ("Clip Rect", vector) = (-32767, -32767, 32767, 32767)

		_VertScale("Vertical Scale", Range(-1, 1)) = 1.0

		[KeywordEnum(None, Top_Bottom, Left_Right)] AlphaPack("Alpha Pack", Float) = 0
		[KeywordEnum(None, Top_Bottom, Left_Right)] Stereo("Stereo Mode", Float) = 0
		[KeywordEnum(None, Left, Right)] ForceEye ("Force Eye Mode", Float) = 0
		[Toggle(STEREO_DEBUG)] _StereoDebug("Stereo Debug Tinting", Float) = 0
		[Toggle(APPLY_GAMMA)] _ApplyGamma("Apply Gamma", Float) = 0
		[Toggle(USE_YPCBCR)] _UseYpCbCr("Use YpCbCr", Float) = 0

		[Header(Blending)]
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
		[Header(Alpha Adjustment)]
		_AlphaSmoothMin ("Alpha Smooth Min", Range(0, 1)) = 0.0
		_AlphaSmoothMax ("Alpha Smooth Max", Range(0, 1)) = 1.0
		
		[Header(Edge Dilation)]
		_EdgeDilation ("Edge Dilation", Range(0, 10)) = 0.0
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
		Fog { Mode Off }
		Blend [_SrcBlend] [_DstBlend]
		ColorMask [_ColorMask]

		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// TODO: replace use multi_compile_local instead (Unity 2019.1 feature)
			#pragma multi_compile ALPHAPACK_NONE ALPHAPACK_TOP_BOTTOM ALPHAPACK_LEFT_RIGHT
			#pragma multi_compile MONOSCOPIC STEREO_TOP_BOTTOM STEREO_LEFT_RIGHT
			#pragma multi_compile FORCEEYE_NONE FORCEEYE_LEFT FORCEEYE_RIGHT
			#pragma multi_compile __ APPLY_GAMMA
			#pragma multi_compile __ STEREO_DEBUG
			#pragma multi_compile __ USE_YPCBCR

#if APPLY_GAMMA
			//#pragma target 3.0
#endif
			#include "UnityCG.cginc"
            // TODO: once we drop support for Unity 4.x then we can include this
			//#include "UnityUI.cginc"    
			#include "../AVProVideo.cginc"
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex		 : SV_POSITION;
				fixed4 color		 : COLOR;
				half4 uv			 : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
			};
			
			uniform fixed4 _Color;
			uniform sampler2D _MainTex;
#if USE_YPCBCR
			uniform sampler2D _ChromaTex;
			uniform float4x4 _YpCbCrTransform;
#endif
			uniform float4 _MainTex_TexelSize;
			uniform float _VertScale;
			uniform float4 _ClipRect;
			uniform float _AlphaSmoothMin;
			uniform float _AlphaSmoothMax;
			uniform float _EdgeDilation;

			inline float UnityGet2DClipping (in float2 position, in float4 clipRect)
			{
			 	float2 inside = step(clipRect.xy, position.xy) * step(position.xy, clipRect.zw);
			 	return inside.x * inside.y;
			}

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.worldPosition = IN.vertex;

				OUT.vertex = XFormObjectToClip(IN.vertex);

#ifdef UNITY_HALF_TEXEL_OFFSET
				OUT.vertex.xy += (_ScreenParams.zw-1.0)*float2(-1,1);
#endif

				OUT.uv.xy = IN.texcoord.xy;

				// Horrible hack to undo the scale transform to fit into our UV packing layout logic...
				if (_VertScale < 0.0)
				{
					OUT.uv.y = 1.0 - OUT.uv.y;
				}

#if STEREO_TOP_BOTTOM | STEREO_LEFT_RIGHT
				float4 scaleOffset = GetStereoScaleOffset(IsStereoEyeLeft(), _MainTex_TexelSize.y < 0.0);
				OUT.uv.xy *= scaleOffset.xy;
				OUT.uv.xy += scaleOffset.zw;
#endif

				OUT.uv = OffsetAlphaPackingUV(_MainTex_TexelSize.xy, OUT.uv.xy, _VertScale < 0.0);

				OUT.color = IN.color * _Color;
#if STEREO_DEBUG
				OUT.color *= GetStereoDebugTint(IsStereoEyeLeft());
#endif			
				return OUT;
			}

			half4 frag(v2f i) : SV_Target
			{
				half4 col;
#if USE_YPCBCR
				col = SampleYpCbCr(_MainTex, _ChromaTex, i.uv.xy, _YpCbCrTransform);
#else
				col = SampleRGBA(_MainTex, i.uv.xy);
#endif

#if ALPHAPACK_TOP_BOTTOM | ALPHAPACK_LEFT_RIGHT
				col.a = SamplePackedAlpha(_MainTex, i.uv.zw);
#endif
				// col *= i.color; // Moved to end to allow smoothstep to work on raw alpha
				
				// Edge Dilation (Fill Gaps)
				if (_EdgeDilation > 0.0)
				{
					// Only process pixels that are currently transparent (or semi-transparent)
					if (col.a < _AlphaSmoothMax) 
					{
						float2 offset = _MainTex_TexelSize.xy * _EdgeDilation;
						
						// Sample 8 neighbors for better coverage
						half4 c1 = SampleRGBA(_MainTex, i.uv.xy + float2(offset.x, 0));
						half4 c2 = SampleRGBA(_MainTex, i.uv.xy - float2(offset.x, 0));
						half4 c3 = SampleRGBA(_MainTex, i.uv.xy + float2(0, offset.y));
						half4 c4 = SampleRGBA(_MainTex, i.uv.xy - float2(0, offset.y));
						
						half4 c5 = SampleRGBA(_MainTex, i.uv.xy + float2(offset.x, offset.y));
						half4 c6 = SampleRGBA(_MainTex, i.uv.xy + float2(offset.x, -offset.y));
						half4 c7 = SampleRGBA(_MainTex, i.uv.xy + float2(-offset.x, offset.y));
						half4 c8 = SampleRGBA(_MainTex, i.uv.xy + float2(-offset.x, -offset.y));

						// Find the neighbor with the strongest alpha
						half4 maxNeighbor = half4(0,0,0,0);
						if (c1.a > maxNeighbor.a) maxNeighbor = c1;
						if (c2.a > maxNeighbor.a) maxNeighbor = c2;
						if (c3.a > maxNeighbor.a) maxNeighbor = c3;
						if (c4.a > maxNeighbor.a) maxNeighbor = c4;
						if (c5.a > maxNeighbor.a) maxNeighbor = c5;
						if (c6.a > maxNeighbor.a) maxNeighbor = c6;
						if (c7.a > maxNeighbor.a) maxNeighbor = c7;
						if (c8.a > maxNeighbor.a) maxNeighbor = c8;

						// If we found a solid neighbor, use its color and boost our alpha
						if (maxNeighbor.a > 0.1)
						{
							// Blend towards the neighbor color
							col.rgb = lerp(col.rgb, maxNeighbor.rgb, 0.8);
							
							// Key Fix: Boost alpha to match the neighbor, effectively "growing" the shape
							// We make sure it's at least as opaque as the neighbor, slightly reduced to fade out if needed
							col.a = max(col.a, maxNeighbor.a); 
						}
					}
				}

				col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
				col.a = smoothstep(_AlphaSmoothMin, _AlphaSmoothMax, col.a);
				
				col *= i.color; // Apply Vertex Color (CanvasGroup) and Tint here

				clip(col.a - 0.001);

				return col;
			}

		ENDCG
		}
	}
}
