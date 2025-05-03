Shader "Custom/VirtualBackground" {
   Properties {
     _CameraFeedTex ("Camera Feed Texture", 2D) = "white" {}
     _BackgroundTex ("Background Texture", 2D) = "black" {}
     _BackgroundColor ("Background Color", Color) = (0, 1, 0, 1)
     _Threshold ("Threshold", Range(0, 1)) = 0.9
   }
   
   SubShader {
     Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
     LOD 100
     
     Pass {
       CGPROGRAM
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
       
       sampler2D _CameraFeedTex;
       sampler2D _BackgroundTex;
       float4 _BackgroundColor;
       float _Threshold;
       
       StructuredBuffer<float> _MaskBuffer;
       int _Width;
       int _Height;
       
       v2f vert(appdata v) {
         v2f o;
         o.vertex = UnityObjectToClipPos(v.vertex);
         o.uv = v.uv;
         return o;
       }
       
       fixed4 frag(v2f i) : SV_Target {
         // Sample camera feed texture
         fixed4 cameraColor = tex2D(_CameraFeedTex, i.uv);
         
         // Sample background texture
         fixed4 bgColor = tex2D(_BackgroundTex, i.uv);
         
         // If no background texture or it's transparent, use the background color
         if (bgColor.a < 0.01) {
           bgColor = _BackgroundColor;
         }
         
         // Calculate buffer index
         int x = i.uv.x * _Width;
         int y = i.uv.y * _Height;
         int index = y * _Width + x;
         
         // Get mask value (0 to 1)
         float maskValue = 0;
         if (index >= 0 && index < _Width * _Height) {
           maskValue = _MaskBuffer[index];
         }
         
         // Apply threshold for a clean separation
         float mask = maskValue > _Threshold ? 1.0 : 0.0;
         
         // Blend background and camera feed based on mask
         // Mask value of 1 means person (keep camera feed)
         // Mask value of 0 means background (use background texture/color)
         return lerp(bgColor, cameraColor, mask);
       }
       
       ENDCG
     }
   }
   
   Fallback "Diffuse"
}