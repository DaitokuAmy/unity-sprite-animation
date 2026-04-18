#ifndef UNITY_SPRITE_ANIMATION_SPRITE_UNLIT_PASS_INCLUDED
#define UNITY_SPRITE_ANIMATION_SPRITE_UNLIT_PASS_INCLUDED

#if defined(DEBUG_DISPLAY)
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/InputData2D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"
#endif

//----------------------------------------
// 頂点入力
//----------------------------------------
struct Attributes {
    float3 positionOS : POSITION;
    float4 color : COLOR;
    float2 uv : TEXCOORD0;
    #if defined(DEBUG_DISPLAY)
    float3 normal : NORMAL;
    #endif
    UNITY_SKINNED_VERTEX_INPUTS
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

//----------------------------------------
// 頂点出力
//----------------------------------------
struct Varyings {
    float4 positionCS : SV_POSITION;
    half4 color : COLOR;
    float2 uv : TEXCOORD0;
    #if defined(DEBUG_DISPLAY)
    float3 positionWS : TEXCOORD2;
    half3 normalWS : TEXCOORD3;
    #endif
    UNITY_VERTEX_OUTPUT_STEREO
};

//----------------------------------------
// 頂点シェーダー
//----------------------------------------
Varyings SpriteUnlitPassVertex(Attributes input) {
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    UNITY_SKINNED_VERTEX_COMPUTE(input);

    SetUpSpriteInstanceProperties();
    input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
    output.positionCS = TransformObjectToHClip(input.positionOS);
    #if defined(DEBUG_DISPLAY)
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.normalWS = TransformObjectToWorldDir(input.normal);
    #endif
    output.uv = input.uv;
    output.color = input.color * _Color * unity_SpriteColor;
    return output;
}

//----------------------------------------
// フラグメントシェーダー
//----------------------------------------
half4 SpriteUnlitPassFragment(Varyings input) : SV_Target {
    half4 currentSprite = SampleCurrentSpriteTexture(input.uv, input.color);
    half4 outputSprite = currentSprite;

    if (_FlipBookBlendParams.x > 0.5h) {
        half4 previousSprite = SamplePreviousSpriteTexture(input.uv, input.color);
        outputSprite = BlendFlipBookSprite(previousSprite, currentSprite);
    }

    #if defined(_ALPHATEST_ON)
    clip(outputSprite.a - _Cutoff);
    #endif

    #if defined(_ALPHAPREMULTIPLY_ON)
    outputSprite.rgb *= outputSprite.a;
    #endif

    #if defined(_ALPHAMODULATE_ON)
    outputSprite.rgb = lerp(half3(1.0h, 1.0h, 1.0h), outputSprite.rgb, outputSprite.a);
    #endif

    #if !defined(_SURFACE_TYPE_TRANSPARENT)
    outputSprite.a = 1.0h;
    #endif

    #if defined(DEBUG_DISPLAY)
    SurfaceData2D surfaceData;
    InputData2D inputData;
    half4 debugColor = 0;

    InitializeSurfaceData(outputSprite.rgb, outputSprite.a, surfaceData);
    InitializeInputData(input.uv, inputData);
    SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, input.positionWS, input.positionCS, _MainTex);
    surfaceData.normalWS = input.normalWS;

    if (CanDebugOverrideOutputColor(surfaceData, inputData, debugColor)) {
        return debugColor;
    }
    #endif

    return outputSprite;
}

#endif
