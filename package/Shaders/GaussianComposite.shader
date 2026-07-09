// SPDX-License-Identifier: MIT
Shader "Hidden/Gaussian Splatting/Composite"
{
    SubShader
    {
        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma require compute
#pragma use_dxc
#include "UnityCG.cginc"

struct v2f
{
    float4 vertex : SV_POSITION;
};

v2f vert (uint vtxID : SV_VertexID)
{
    v2f o;
    float2 quadPos = float2(vtxID&1, (vtxID>>1)&1) * 4.0 - 1.0;
	o.vertex = float4(quadPos, 1, 1);
    return o;
}

Texture2D _GaussianSplatRT;
int _UseSortFree;
float _SortFreeBackgroundWeight;
float _SortFreeExposure;


half4 frag (v2f i) : SV_Target
{
    float4 col = _GaussianSplatRT.Load(int3(i.vertex.xy, 0));
    
    if (_UseSortFree != 0)
    {
        // float count = max(col.a, 1.0);
        // float accumWeight = max(col.a, 1e-6);
        // float3 avgColor = col.rgb / count;
        // return float4(saturate(avgColor), 1.0);
        float w = max(col.a + _SortFreeBackgroundWeight, 1e-6);
        float3 avgColor = col.rgb / w;
        float outAlpha = saturate(col.a / w);
        return float4(GammaToLinearSpace(saturate(avgColor)), outAlpha);
    }

    float alpha = max(col.a, 1e-6);
    return float4(GammaToLinearSpace(col.rgb / alpha), col.a);
}
ENDCG
        }
    }
}
