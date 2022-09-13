sampler2D texSampler : register(S0);

float2 texPix;
float3 outlineColor;

float sampleAlpha(float x, float y)
{
    return tex2D(texSampler, float2(x, y)).a;
}

float4 PSMain(float2 pos : TEXCOORD0) : COLOR
{
    float v = 0;
    
    v = max(v, sampleAlpha(pos.x           , pos.y - texPix.y));
    //v = max(v, sampleAlpha(pos.x + texPix.x, pos.y - texPix.y));
    v = max(v, sampleAlpha(pos.x + texPix.x, pos.y));
    //v = max(v, sampleAlpha(pos.x + texPix.x, pos.y + texPix.y));
    v = max(v, sampleAlpha(pos.x           , pos.y + texPix.y));
    //v = max(v, sampleAlpha(pos.x - texPix.x, pos.y + texPix.y));
    v = max(v, sampleAlpha(pos.x - texPix.x, pos.y));
    //v = max(v, sampleAlpha(pos.x - texPix.x, pos.y - texPix.y));
    
    float4 color = float4(outlineColor * v, v);
    float4 texColor = tex2D(texSampler, pos);
    return lerp(color, texColor, texColor.a);
}



technique Main
{
    pass Main
    {
        PixelShader = compile ps_2_0 PSMain();

    }
}