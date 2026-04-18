Shader "Unity Sprite Animation/Sprite Unlit" {
    Properties {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}

        _Surface ("__surface", Float) = 1.0
        _Blend ("__mode", Float) = 0.0
        [ToggleUI] _AlphaClip ("__clip", Float) = 0.0
        _Cutoff ("__cutoff", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _BlendOp ("__blendop", Float) = 0.0
        [HideInInspector] _SrcBlend ("__src", Float) = 5.0
        [HideInInspector] _DstBlend ("__dst", Float) = 10.0
        [HideInInspector] _SrcBlendAlpha ("__srcA", Float) = 1.0
        [HideInInspector] _DstBlendAlpha ("__dstA", Float) = 10.0
        [HideInInspector] _ZWrite ("__zw", Float) = 0.0
        [HideInInspector] _Cull ("__cull", Float) = 0.0
        [HideInInspector] _QueueOffset ("Queue Offset", Float) = 0.0

        // Legacy properties. They're here so that materials using this shader can gracefully fallback to the legacy sprite shader.
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0

        [HideInInspector] _PrevTex ("Previous Sprite Texture", 2D) = "black" {}
        [HideInInspector] _CurrentTexUVRect ("Current Sprite UV Rect", Vector) = (0,0,1,1)
        [HideInInspector] _PrevTexUVRect ("Previous Sprite UV Rect", Vector) = (0,0,1,1)
        [HideInInspector] _FlipBookBlendParams ("FlipBookBlend Params", Vector) = (0,1,0,0)
    }

    SubShader {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        BlendOp [_BlendOp]
        Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
        Cull [_Cull]
        ZWrite [_ZWrite]

        Pass {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex SpriteUnlitPassVertex
            #pragma fragment SpriteUnlitPassFragment

            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local_fragment _ALPHAMODULATE_ON

            #include "Packages/com.daitokuamy.unityspriteanimation/Shaders/Include/SpriteUnlitInput.hlsl"
            #include "Packages/com.daitokuamy.unityspriteanimation/Shaders/Include/SpriteUnlitPass.hlsl"
            ENDHLSL
        }

        Pass {
            Tags { "LightMode" = "UniversalForward" "Queue" = "Transparent" "RenderType" = "Transparent" }

            HLSLPROGRAM
            #pragma vertex SpriteUnlitPassVertex
            #pragma fragment SpriteUnlitPassFragment

            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local_fragment _ALPHAMODULATE_ON

            #include "Packages/com.daitokuamy.unityspriteanimation/Shaders/Include/SpriteUnlitInput.hlsl"
            #include "Packages/com.daitokuamy.unityspriteanimation/Shaders/Include/SpriteUnlitPass.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "UnitySpriteAnimation.Editor.SpriteUnlitShaderGUI"
}
