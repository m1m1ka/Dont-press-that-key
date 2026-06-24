#ifndef REVOLVER_OUTLINE_PASS_INCLUDED
#define REVOLVER_OUTLINE_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
};

CBUFFER_START(UnityPerMaterial)
    half4 _OutlineColor;
    float _OutlineWidth;
    float4 _OutlineDirection;
CBUFFER_END

Varyings OutlineVertex(Attributes input)
{
    Varyings output;
    float4 positionCS = TransformObjectToHClip(input.positionOS.xyz);
    float directionLength = max(length(_OutlineDirection.xy), 0.0001);
    float2 pixelDirection = _OutlineDirection.xy / directionLength;
    float2 pixelOffset = pixelDirection * (_OutlineWidth * 1000.0);

    positionCS.xy += (pixelOffset * 2.0 / _ScreenParams.xy) * positionCS.w;
    output.positionCS = positionCS;
    return output;
}

half4 OutlineFragment(Varyings input) : SV_Target
{
    return _OutlineColor;
}

#endif
