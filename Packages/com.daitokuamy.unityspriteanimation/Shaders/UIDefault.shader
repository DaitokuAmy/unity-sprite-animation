Shader "Unity Sprite Animation/UI Default" {
    Properties {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Blend ("__mode", Float) = 1.0
        [HideInInspector] _BlendOp ("__blendop", Float) = 0.0
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 10.0
        [HideInInspector] _SrcBlendAlpha ("__srcA", Float) = 1.0
        [HideInInspector] _DstBlendAlpha ("__dstA", Float) = 10.0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [PerRendererData, HideInInspector] _PrevTex ("Previous Sprite Texture", 2D) = "black" {}
        [HideInInspector] _CurrentTexUVRect ("Current Sprite UV Rect", Vector) = (0,0,1,1)
        [HideInInspector] _PrevTexUVRect ("Previous Sprite UV Rect", Vector) = (0,0,1,1)
        [HideInInspector] _FlipBookBlendParams ("FlipBookBlend Params", Vector) = (0,1,0,0)

        [HideInInspector] _TextureSampleAdd ("Texture Sample Add", Vector) = (0,0,0,0)
        [HideInInspector] _ClipRect ("Clip Rect", Vector) = (-32767,-32767,32767,32767)
        [HideInInspector] _UIMaskSoftnessX ("UIMask Softness X", Float) = 0
        [HideInInspector] _UIMaskSoftnessY ("UIMask Softness Y", Float) = 0
    }

    SubShader {
        Tags {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Stencil {
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
        BlendOp [_BlendOp]
        Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
        ColorMask [_ColorMask]

        Pass {
            Name "Default"

            HLSLPROGRAM
            #pragma vertex UIDefaultPassVertex
            #pragma fragment UIDefaultPassFragment
            #pragma target 2.0

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local_fragment _ALPHAMODULATE_ON

            #include "Packages/com.daitokuamy.unityspriteanimation/Shaders/Include/UIDefaultInput.hlsl"
            #include "Packages/com.daitokuamy.unityspriteanimation/Shaders/Include/UIDefaultPass.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "UnitySpriteAnimation.Editor.UIDefaultShaderGUI"
}
