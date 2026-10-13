Shader "Octopus/ArmStretch"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Range ("Range", Float) = 1
        _BodyRadius ("Body Radius", Float) = 0.15
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex ArmVertex
            #pragma fragment ArmFragment

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Range;
                float _BodyRadius;
            CBUFFER_END

            Varyings ArmVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            float2 StretchUv(float2 uv)
            {
                float range = max(_Range, 0.001);
                float2 f = (uv - 0.5) * range;
                float dist = length(f);
                float body = _BodyRadius;
                float2 o = f;
                if (dist > body)
                    o = f / dist * (body + (dist - body) / range);
                return 0.5 + o;
            }

            half4 ArmFragment(Varyings input) : SV_Target
            {
                float2 sampleUv = StretchUv(input.uv);
                if (sampleUv.x < 0.0 || sampleUv.x > 1.0 || sampleUv.y < 0.0 || sampleUv.y > 1.0)
                    discard;

                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUv) * input.color;
                if (color.a < 0.02)
                    discard;
                return color;
            }
            ENDHLSL
        }
    }
}
