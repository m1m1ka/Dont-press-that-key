Shader "Custom/URP/Revolver Outline"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (1, 0.55, 0.08, 1)
        _OutlineWidth("Outline Width", Range(0.0005, 0.05)) = 0.008
        _OutlineDirection("Outline Direction", Vector) = (1, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry+10"
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Off
            ZWrite Off
            ZTest Always

            Stencil
            {
                Ref 42
                Comp NotEqual
                Pass Keep
            }

            HLSLPROGRAM
            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment

            #include "Assets/Shaders/RevolverOutlinePass.hlsl"
            ENDHLSL
        }
    }
}
