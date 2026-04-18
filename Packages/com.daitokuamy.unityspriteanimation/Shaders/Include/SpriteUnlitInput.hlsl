#ifndef UNITY_SPRITE_ANIMATION_SPRITE_UNLIT_INPUT_INCLUDED
#define UNITY_SPRITE_ANIMATION_SPRITE_UNLIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_PrevTex);
SAMPLER(sampler_PrevTex);
UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_MainTex);

CBUFFER_START(UnityPerMaterial)
    half4 _Color;
    half _Cutoff;
    float4 _CurrentTexUVRect;
    float4 _PrevTexUVRect;
    half4 _FlipBookBlendParams;
CBUFFER_END

//----------------------------------------
// Sprite UV を正規化する
//----------------------------------------
float2 NormalizeSpriteUV(float2 uv, float4 uvRect) {
    float2 safeSize = max(uvRect.zw, float2(0.00001, 0.00001));
    return saturate((uv - uvRect.xy) / safeSize);
}

//----------------------------------------
// Sprite UV を再マップする
//----------------------------------------
float2 RemapSpriteUV(float2 normalizedUv, float4 uvRect) {
    return uvRect.xy + (normalizedUv * uvRect.zw);
}

//----------------------------------------
// 現在 Sprite のテクスチャを取得する
//----------------------------------------
half4 SampleCurrentSpriteTexture(float2 uv, half4 color) {
    return color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
}

//----------------------------------------
// 前 Sprite のテクスチャを取得する
//----------------------------------------
half4 SamplePreviousSpriteTexture(float2 uv, half4 color) {
    float2 normalizedUv = NormalizeSpriteUV(uv, _CurrentTexUVRect);
    float2 previousUv = RemapSpriteUV(normalizedUv, _PrevTexUVRect);
    return color * SAMPLE_TEXTURE2D(_PrevTex, sampler_PrevTex, previousUv);
}

//----------------------------------------
// FlipBookBlend の色を合成する
//----------------------------------------
half4 BlendFlipBookSprite(half4 previousSprite, half4 currentSprite) {
    half blendProgress = saturate(_FlipBookBlendParams.y);
    if (blendProgress <= 0.0001h) {
        return previousSprite;
    }

    if (blendProgress >= 0.9999h) {
        return currentSprite;
    }

    half4 blendedSprite = lerp(previousSprite, currentSprite, blendProgress);
    blendedSprite.a = max(previousSprite.a, currentSprite.a);
    return blendedSprite;
}

#endif
