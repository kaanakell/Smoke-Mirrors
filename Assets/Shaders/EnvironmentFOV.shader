Shader "SmokeAndMirrors/EnvironmentFOV"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Out Of Sight Settings)]
        _DarknessTint ("Darkness Tint", Color) = (0.2, 0.2, 0.25, 1) // Slightly blue/dark tint
        _Desaturation ("Desaturation Amount", Range(0, 1)) = 1.0 // 1 = fully gray
        
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
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
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            fixed4 _Color;
            fixed4 _DarknessTint;
            float _Desaturation;
            sampler2D _MainTex;
            sampler2D _VisionMask; 

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                OUT.screenPos = ComputeScreenPos(OUT.vertex);
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Get the original sprite color
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                // Sample the global Vision Mask
                float2 screenUV = IN.screenPos.xy / max(IN.screenPos.w, 0.001);
                fixed4 mask = tex2D(_VisionMask, screenUV);

                // --- Grayscale Math ---
                // Calculate the true grayscale value of the pixel
                float luminance = dot(c.rgb, fixed3(0.299, 0.587, 0.114));
                fixed3 grayColor = fixed3(luminance, luminance, luminance);
                
                // Blend between original color and gray based on our setting
                fixed3 outOfSightColor = lerp(c.rgb, grayColor, _Desaturation);
                
                // Tint it darker so it actually looks like shadow
                outOfSightColor *= _DarknessTint.rgb;

                // --- Apply the Mask ---
                // mask.r is 1.0 (white) if in the light, 0.0 (black) if in the dark.
                // We lerp between the dark/gray color and the full color!
                c.rgb = lerp(outOfSightColor, c.rgb, mask.r);

                // Premultiplied alpha
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}