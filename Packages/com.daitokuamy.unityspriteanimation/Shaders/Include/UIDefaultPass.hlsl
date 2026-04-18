#ifndef UNITY_SPRITE_ANIMATION_UI_DEFAULT_PASS_INCLUDED
#define UNITY_SPRITE_ANIMATION_UI_DEFAULT_PASS_INCLUDED

//----------------------------------------
// 頂点シェーダー
//----------------------------------------
Varyings UIDefaultPassVertex(Attributes input) {
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = input.uv;
    output.color = input.color * _Color;
    output.mask = CalculateMask(input.positionOS, output.positionCS);
    return output;
}

//----------------------------------------
// UI マスクのアルファを適用する
//----------------------------------------
void ApplyClipRect(inout half4 color, float4 mask) {
    #ifdef UNITY_UI_CLIP_RECT
    half2 maskValue = saturate((_ClipRect.zw - _ClipRect.xy - abs(mask.xy)) * mask.zw);
    color.a *= maskValue.x * maskValue.y;
    #endif
}

//----------------------------------------
// フラグメントシェーダー
//----------------------------------------
half4 UIDefaultPassFragment(Varyings input) : SV_Target {
    const half alphaPrecision = half(255.0h);
    const half invAlphaPrecision = half(1.0h / 255.0h);

    input.color.a = round(input.color.a * alphaPrecision) * invAlphaPrecision;

    half4 currentSprite = SampleCurrentSpriteTexture(input.uv);
    half4 outputSprite = currentSprite;

    if (_FlipBookBlendParams.x > 0.5h) {
        half4 previousSprite = SamplePreviousSpriteTexture(input.uv);
        outputSprite = BlendFlipBookSprite(previousSprite, currentSprite);
    }

    outputSprite *= input.color;

    ApplyClipRect(outputSprite, input.mask);

    #ifdef _ALPHAMODULATE_ON
    outputSprite.rgb = lerp(half3(1.0h, 1.0h, 1.0h), outputSprite.rgb, outputSprite.a);
    #endif

    #ifdef _ALPHAPREMULTIPLY_ON
    outputSprite.rgb *= outputSprite.a;
    #endif

    return outputSprite;
}

#endif
