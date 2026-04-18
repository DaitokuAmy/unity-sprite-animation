#ifndef UNITY_SPRITE_ANIMATION_UI_DEFAULT_INPUT_INCLUDED
#define UNITY_SPRITE_ANIMATION_UI_DEFAULT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_PrevTex);
SAMPLER(sampler_PrevTex);

CBUFFER_START(UnityPerMaterial)
    half4 _Color;
    half4 _TextureSampleAdd;
    float4 _ClipRect;
    float _UIMaskSoftnessX;
    float _UIMaskSoftnessY;
    float4 _CurrentTexUVRect;
    float4 _PrevTexUVRect;
    half4 _FlipBookBlendParams;
CBUFFER_END

//----------------------------------------
// 頂点入力
//----------------------------------------
struct Attributes {
    float4 positionOS : POSITION;
    float4 color : COLOR;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

//----------------------------------------
// 頂点出力
//----------------------------------------
struct Varyings {
    float4 positionCS : SV_POSITION;
    half4 color : COLOR;
    float2 uv : TEXCOORD0;
    float4 mask : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};

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
// UI マスク用の係数を計算する
//----------------------------------------
float4 CalculateMask(float4 positionOS, float4 positionCS) {
    float2 pixelSize = positionCS.w;
    pixelSize /= abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

    float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
    return float4(
        positionOS.xy * 2.0 - clampedRect.xy - clampedRect.zw,
        0.25 / (0.25 * float2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize)));
}

//----------------------------------------
// 現在 Sprite のテクスチャを取得する
//----------------------------------------
half4 SampleCurrentSpriteTexture(float2 uv) {
    return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) + _TextureSampleAdd;
}

//----------------------------------------
// 前 Sprite のテクスチャを取得する
//----------------------------------------
half4 SamplePreviousSpriteTexture(float2 uv) {
    float2 normalizedUv = NormalizeSpriteUV(uv, _CurrentTexUVRect);
    float2 previousUv = RemapSpriteUV(normalizedUv, _PrevTexUVRect);
    return SAMPLE_TEXTURE2D(_PrevTex, sampler_PrevTex, previousUv) + _TextureSampleAdd;
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
