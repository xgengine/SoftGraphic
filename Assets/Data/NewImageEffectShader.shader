Shader "Hidden/NewImageEffectShader"
{
   Properties
   {
    _MainTex ("Base (RGB)", 2D) = "white" {}
    _Bloom ("Bloom (RGB)", 2D) = "black" {}
    _LuminanceThreshold ("Luminance Threshold", Float) = 0.5        // 亮度阈值
    _BlurSize ("Blur Size", Float) = 1.0
   }
    SubShader
    {


        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _Bloom;
        sampler2D _MainTex;
        float4 _MainTex_TexelSize;
        float4 _Bloom_TexelSize;
        float4 _MainTex_ST;
        float _LuminanceThreshold;
        float _BlurSize;

        struct v2f
        {
            float4 pos : SV_POSITION; 
            half2 uv : TEXCOORD0;         
        };
        v2f vertExtractBright(appdata_img v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
  o.uv = v.texcoord;
            if (_MainTex_TexelSize.y <0.0)
            {
                 o.uv.y = 1.0 - o.uv.y;
            }

          
            return o;
        }
        fixed4 fragExtractBright(v2f i) : SV_Target
        {
            fixed4 c = tex2D(_MainTex, i.uv);                                       // 原像素值
            fixed val = clamp(c.r- _LuminanceThreshold, 0.0, 1.0);        // 亮度减去阈值，再截取到0~1
            return c * val;     // 得到提取后的亮部区域
        }
        struct v2fBloom
        {
            float4 pos : SV_POSITION; 
            half4 uv : TEXCOORD0;           // uv.xy：原图像的纹理坐标。uv.zw：_Bloom，较亮区域纹理坐标
        };
        v2fBloom vertBloom(appdata_img v)
        {
            v2fBloom o;
            o.pos = UnityObjectToClipPos (v.vertex);
            o.uv.xy = v.texcoord;       
            o.uv.zw = v.texcoord;
            #if UNITY_UV_STARTS_AT_TOP      // 起始坐标在上面的话，就反过来
            if (_MainTex_TexelSize.y < 0.0)
            {
                 o.uv.w = 1.0 - o.uv.w;
            }
            
            #endif
            return o; 
        }


        fixed4 fragBloom(v2fBloom i) : SV_Target
        {
            return tex2D(_MainTex, i.uv.xy) + tex2D(_Bloom, i.uv.zw)*4;
        }
        ENDCG

        ZTest Always Cull Off ZWrite Off

        Pass {
            CGPROGRAM  
            #pragma vertex vertExtractBright  
            #pragma fragment fragExtractBright  
            ENDCG  

        }
        Pass
        {
            CGPROGRAM  
            #pragma vertex vertBloom  
            #pragma fragment fragBloom  
            ENDCG  
        } 


    }
}
