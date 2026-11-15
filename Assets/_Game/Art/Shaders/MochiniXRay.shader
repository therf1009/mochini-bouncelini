// X-ray: se deseneaza DOAR acolo unde obiectul e ascuns de ceva din fata lui (ZTest Greater), intr-un cyan care straluceste.
// Pus pe cristalele de dash: cand un cristal e in spatele turnului, tot se vede unde e.
// Culoarea e fixa in shader (fara proprietati), ca la silueta lui mochi (SRP Batcher).
Shader "Mochini/XRay"
{
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Geometry+50" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "XRay"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            ZTest Greater
            ZWrite Off
            Offset -2, -2
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert (Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                return half4(0.45, 0.95, 1.0, 0.75);
            }
            ENDHLSL
        }
    }
}
