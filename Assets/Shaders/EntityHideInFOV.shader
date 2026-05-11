Shader "SmokeAndMirrors/EntityHideInFOV"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
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
        Blend One OneMinusSrcAlpha // Premultiplied alpha blending

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
                float4 screenPos : TEXCOORD1; // Needed to sample the screen-space global mask
            };

            fixed4 _Color;
            sampler2D _MainTex;
            
            // The global texture we set in the VisionMaskManager script!
            sampler2D _VisionMask; 

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                
                // Calculate screen position to sample the vision mask correctly
                OUT.screenPos = ComputeScreenPos(OUT.vertex);
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 1. Get the sprite pixel
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                // 2. Get the screen-space UVs
                float2 screenUV = IN.screenPos.xy / max(IN.screenPos.w, 0.001);
                
                // 3. Sample the global Vision Mask
                fixed4 mask = tex2D(_VisionMask, screenUV);

                // 4. If the mask is dark (meaning outside FOV), kill the pixel entirely
                if (mask.r < 0.1) 
                {
                    discard;
                }

                // If inside FOV, render normally (with premultiplied alpha)
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}