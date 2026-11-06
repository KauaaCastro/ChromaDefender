Shader "ChromaDefender/SpriteElementTint"
{
    // Sprite unlit que troca SOMENTE os pixels alaranjados/dourados (lâmina da espada e efeitos de corte
    // desenhados no sprite) por azul gélido, preservando a armadura preta. Funciona no URP 2D.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0

        _TargetHue ("Hue Alvo (gelo)", Range(0,1)) = 0.56
        _HueMin ("Hue Mínimo Origem", Range(0,1)) = 0.02
        _HueMax ("Hue Máximo Origem", Range(0,1)) = 0.17
        _MinSat ("Saturação Mínima", Range(0,1)) = 0.30
        _Brightness ("Brilho", Range(0.5,2)) = 1.15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment ElementFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            float _TargetHue;
            float _HueMin;
            float _HueMax;
            float _MinSat;
            float _Brightness;

            float3 RgbToHsv(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 HsvToRgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            fixed4 ElementFrag(v2f IN) : SV_Target
            {
                fixed4 tex = SampleSpriteTexture(IN.texcoord);

                float3 hsv = RgbToHsv(tex.rgb);
                float inHue = smoothstep(_HueMin - 0.02, _HueMin + 0.01, hsv.x) * (1.0 - smoothstep(_HueMax - 0.01, _HueMax + 0.02, hsv.x));
                float inSat = smoothstep(_MinSat - 0.1, _MinSat, hsv.y);
                float mask = inHue * inSat;

                float3 iceRgb = HsvToRgb(float3(_TargetHue, saturate(hsv.y * 0.85), saturate(hsv.z * _Brightness)));
                tex.rgb = lerp(tex.rgb, iceRgb, mask);

                fixed4 c = tex * IN.color;
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
