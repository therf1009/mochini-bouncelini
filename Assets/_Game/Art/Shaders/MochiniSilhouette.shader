// Silueta: se deseneaza DOAR acolo unde obiectul e ascuns de ceva din fata lui (ZTest Greater).
// Coada Geometry+50: dupa blocuri si mochi, dar INAINTEA textelor (TextMeshPro scrie in depth si ar declansa silueta).
// Offset -2: silueta e impinsa putin spre camera, ca sa nu "castige" niciodata in fata propriului corp (z-fighting).
// Culoarea e fixa in shader (fara proprietati de material): asa nu o incurca SRP Batcher-ul cu culoarea altui obiect.
// Pusa pe o copie a corpului lui mochi: cand trece prin spatele unui bloc, ramane o umbra inchisa la culoare.
Shader "Mochini/Silhouette"
{
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Geometry+50" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Silhouette"
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
                return half4(0.12, 0.08, 0.14, 0.72);
            }
            ENDHLSL
        }
    }
}
