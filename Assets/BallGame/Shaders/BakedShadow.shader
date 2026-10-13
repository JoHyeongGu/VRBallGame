Shader "Custom/BakedShadow"
{
    Properties
    {
        _ShadowColor ("Shadow Color", Color) = (0.1, 0.1, 0.1, 0.5)
        _FloorHeight ("Floor Height", Float) = 0.01 // 그림자가 맺힐 바닥의 Y 높이
        
        [Header(VAT Alignment)]
        _VatRotation ("Rotation Correction (X,Y,Z)", Vector) = (0, 0, 0, 0)
        _VatOffset ("Position Offset (X,Y,Z)", Vector) = (0, 0, 0, 0)

        [Header(VAT General Settings)]
        _VatAnimFps ("VAT Animation FPS", Float) = 15.0
        _TransitionDuration ("Transition Duration (Sec)", Range(0.01, 2.0)) = 0.25
        [Enum(Slot 0, 0, Slot 1, 1)] _CurrentAnimIndex ("Current Anim Slot", Float) = 0.0
        
        [Header(VAT Slot 0)]
        [Toggle] _EnableVat0 ("Enable Slot 0 VAT", Float) = 1.0
        [Toggle] _VatLoop0 ("Slot 0 Loop", Float) = 1.0
        _VatPosTex0 ("Slot 0 Pos Tex", 2D) = "black" {}
        
        [Header(VAT Slot 1)]
        [Toggle] _EnableVat1 ("Enable Slot 1 VAT", Float) = 0.0
        [Toggle] _VatLoop1 ("Slot 1 Loop", Float) = 1.0
        _VatPosTex1 ("Slot 1 Pos Tex", 2D) = "black" {}

        [HideInInspector] _TransitionStartTime ("Transition Start Time", Float) = -10.0
        [HideInInspector] _StartTime0 ("Slot 0 Start Time", Float) = 0.0
        [HideInInspector] _StartTime1 ("Slot 1 Start Time", Float) = 0.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
            "Queue"="Transparent-1" // 투명 오브젝트보다 먼저 그려짐
        }
        LOD 100

        // 자기 자신과 겹쳐서 그림자가 진해지는 현상(Z-fighting 및 겹침) 방지
        Stencil 
        {
            Ref 1
            Comp NotEqual
            Pass Replace
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "PlanarShadow"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                uint vertexID       : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShadowColor;
                float _FloorHeight;
                float4 _VatRotation;
                float4 _VatOffset;
                float _VatAnimFps;
                float _TransitionDuration;
                float4 _VatPosTex0_TexelSize;
                float4 _VatPosTex1_TexelSize;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _CurrentAnimIndex)
                UNITY_DEFINE_INSTANCED_PROP(float, _TransitionStartTime)
                UNITY_DEFINE_INSTANCED_PROP(float, _EnableVat0)
                UNITY_DEFINE_INSTANCED_PROP(float, _VatLoop0)
                UNITY_DEFINE_INSTANCED_PROP(float, _StartTime0)
                UNITY_DEFINE_INSTANCED_PROP(float, _EnableVat1)
                UNITY_DEFINE_INSTANCED_PROP(float, _VatLoop1)
                UNITY_DEFINE_INSTANCED_PROP(float, _StartTime1)
            UNITY_INSTANCING_BUFFER_END(Props)

            TEXTURE2D(_VatPosTex0); SAMPLER(sampler_VatPosTex0);
            TEXTURE2D(_VatPosTex1); SAMPLER(sampler_VatPosTex1);

            float3x3 GetEulerRotationMatrix(float3 eulerAngles)
            {
                float3 r = radians(eulerAngles);
                float cx = cos(r.x), sx = sin(r.x);
                float cy = cos(r.y), sy = sin(r.y);
                float cz = cos(r.z), sz = sin(r.z);

                float3x3 rotX = float3x3(1, 0, 0, 0, cx, -sx, 0, sx, cx);
                float3x3 rotY = float3x3(cy, 0, sy, 0, 1, 0, -sy, 0, cy);
                float3x3 rotZ = float3x3(cz, -sz, 0, sz, cz, 0, 0, 0, 1);

                return mul(rotZ, mul(rotY, rotX));
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float currentTime = _Time.y;
                float targetSlot = UNITY_ACCESS_INSTANCED_PROP(Props, _CurrentAnimIndex);
                float transStart = UNITY_ACCESS_INSTANCED_PROP(Props, _TransitionStartTime);
                float enable0    = UNITY_ACCESS_INSTANCED_PROP(Props, _EnableVat0);
                float loop0      = UNITY_ACCESS_INSTANCED_PROP(Props, _VatLoop0);
                float start0     = UNITY_ACCESS_INSTANCED_PROP(Props, _StartTime0);
                float enable1    = UNITY_ACCESS_INSTANCED_PROP(Props, _EnableVat1);
                float loop1      = UNITY_ACCESS_INSTANCED_PROP(Props, _VatLoop1);
                float start1     = UNITY_ACCESS_INSTANCED_PROP(Props, _StartTime1);

                // VAT 샘플링
                float totalFrames0 = _VatPosTex0_TexelSize.w;
                float rawFrame0 = max(0.0, floor((currentTime - start0) * _VatAnimFps));
                float frame0 = (loop0 > 0.5) ? fmod(rawFrame0, totalFrames0) : clamp(rawFrame0, 0.0, max(0.0, totalFrames0 - 1.0));
                float2 uv0 = float2((input.vertexID + 0.5) * _VatPosTex0_TexelSize.x, (frame0 + 0.5) * _VatPosTex0_TexelSize.y);
                float3 pos0 = lerp(input.positionOS.xyz, SAMPLE_TEXTURE2D_LOD(_VatPosTex0, sampler_VatPosTex0, uv0, 0).xyz, enable0);

                float totalFrames1 = _VatPosTex1_TexelSize.w;
                float rawFrame1 = max(0.0, floor((currentTime - start1) * _VatAnimFps));
                float frame1 = (loop1 > 0.5) ? fmod(rawFrame1, totalFrames1) : clamp(rawFrame1, 0.0, max(0.0, totalFrames1 - 1.0));
                float2 uv1 = float2((input.vertexID + 0.5) * _VatPosTex1_TexelSize.x, (frame1 + 0.5) * _VatPosTex1_TexelSize.y);
                float3 pos1 = lerp(input.positionOS.xyz, SAMPLE_TEXTURE2D_LOD(_VatPosTex1, sampler_VatPosTex1, uv1, 0).xyz, enable1);

                float progress = saturate((currentTime - transStart) / max(0.0001, _TransitionDuration));
                float blendWeight = (targetSlot > 0.5) ? progress : (1.0 - progress);

                float3 finalPosOS = lerp(pos0, pos1, blendWeight);
                finalPosOS = mul(GetEulerRotationMatrix(_VatRotation.xyz), finalPosOS) + _VatOffset.xyz;

                // 1. 메쉬를 월드 좌표로 변환
                float3 posWS = TransformObjectToWorld(finalPosOS);

                // 2. 메인 라이트 방향 가져오기
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;
                lightDir.y = max(lightDir.y, 0.001); // 0으로 나누기 방지

                // 3. 빛의 방향을 따라 바닥(FloorHeight)으로 정점 투영 (핵심 연산)
                float t = (posWS.y - _FloorHeight) / lightDir.y;
                posWS.x -= lightDir.x * t;
                posWS.z -= lightDir.z * t;
                posWS.y = _FloorHeight;

                output.positionHCS = TransformWorldToHClip(posWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _ShadowColor;
            }
            ENDHLSL
        }
    }
}