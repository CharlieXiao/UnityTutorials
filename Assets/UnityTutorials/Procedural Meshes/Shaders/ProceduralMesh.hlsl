
#define PI 3.14159265358979323846

void Ripple_float(
    float3 PositionIn,float3 Origin,
    float Period,float Speed,float Amplitude,
    out float3 PositionOut,out float3 NormalOut,out float3 TangentOut)
{
    float3 p = PositionIn - Origin;
    float d = length(p);
    // _Time will be in shader graph variable
    float f = 2.0 * PI * Period * ( d - Speed * _Time.y);
    // 仅设置了y值
    PositionOut = PositionIn + float3(0.0,Amplitude * sin(f),0.0);

    float2 derivatives = (2.0 * PI * Amplitude * Period * cos(f) / max(d,0.0001)) * p.xz;

    TangentOut = float3(1.0,derivatives.x,0.0);
    NormalOut = float3(-derivatives.x,1.0,-derivatives.y);
}
