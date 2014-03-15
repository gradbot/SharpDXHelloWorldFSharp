cbuffer Transforms
{
    row_major matrix world;
    row_major matrix view;
    row_major matrix projection;
    row_major matrix wvp;
};

struct VS_INPUT
{
    float3 position : POSITION;
};

struct VS_OUTPUT
{
    float4 position : SV_POSITION;
    float3 wpos     : POSITION;
};

VS_OUTPUT VShader(VS_INPUT input)
{
    VS_OUTPUT output;

    output.position = mul(float4(input.position, 1), wvp);
    output.wpos = input.position;

    return output;
}

float4 PShader(VS_OUTPUT input) : SV_Target
{
    return float4(1, 1, 1, 1);
}
