Shader "Unlit/Background"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0,0,0.1,1)

        _RippleSpeed ("Ripple Speed", Float) = 1
        _RippleMin ("Ripple Min Thickness", Float) = 0.01
        _RippleMax ("Ripple Max Thickness", Float) = 0.05
        _RippleColorSpeed ("Ripple Color Speed", Float) = 0.2

        _StarCount ("Star Count", Int) = 200
        _StarSpeed ("Star Speed", Float) = 0.2
        _StarSizeMin ("Star Size Min", Float) = 0.01
        _StarSizeMax ("Star Size Max", Float) = 0.03

        _Color0 ("White", Color) = (1,1,1,1)
        _Color1 ("Red", Color) = (1,0,0,1)
        _Color2 ("Blue", Color) = (0,0,1,1)
        _Color3 ("Green", Color) = (0,1,0,1)
        _Color4 ("Yellow", Color) = (1,1,0,1)

        _TimeValue ("Time Value", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _BaseColor;

            float _RippleSpeed;
            float _RippleMin;
            float _RippleMax;
            float _RippleColorSpeed;

            int _StarCount;
            float _StarSpeed;
            float _StarSizeMin;
            float _StarSizeMax;

            float4 _Color0,_Color1,_Color2,_Color3,_Color4;

            float _TimeValue;

            // C# から渡される配列
            float2 _StarPos[1024];
            float _StarSize[1024];
            int _StarColorIndex[1024];

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * 2 - 1;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float dist = length(uv);

                float4 col = _BaseColor;

                // -------------------------
                // ① 波紋（5色ゆるやかループ）
                // -------------------------
                float ripplePos = dist * 20 - _TimeValue * _RippleSpeed * 4;
                float ripple = sin(ripplePos);

                float thickness = lerp(_RippleMin, _RippleMax, dist);
                ripple = smoothstep(1 - thickness, 1, ripple);

                float t = frac(_TimeValue * _RippleColorSpeed);

                float4 rippleColors[5] = {
                    _Color0,_Color1,_Color2,_Color3,_Color4
                };

                float ft = t * 5;
                int idx = (int)ft;
                int next = (idx + 1) % 5;
                float blend = frac(ft);

                float4 rippleColor = lerp(rippleColors[idx], rippleColors[next], blend);

                col += rippleColor * ripple;

                // -------------------------
                // ② 星（中心白＋外側5色縁取り）
                // -------------------------
                for (int s = 0; s < _StarCount; s++)
                {
                    float2 pos = _StarPos[s];
                    float size = _StarSize[s];
                    int cidx = _StarColorIndex[s];

                    float d = length(uv - pos);

                    float core = smoothstep(size * 0.75, 0, d);
                    float edge = smoothstep(size, size * 0.75, d);

                    float4 starCol = core * _Color0 + edge * rippleColors[cidx];

                    col += starCol;
                }

                return col;
            }
            ENDCG
        }
    }
}
