//UNITY_SHADER_NO_UPGRADE
#ifndef Noise3D_INCLUDED
#define Noise3D_INCLUDED

#include "WorleyUtilities.hlsl"

void Worley3D_float(float3 Position, float Frequency, out float Out)
{
    Out = WorleyNoise(Position,Frequency);
}

void Gradient3D_float(float3 Position, float Frequency, out float Out)
{
    Out = GradientNoise(Position,Frequency);
}

void PerlinFBM3D_float(float3 Position, float Frequency, int Octaves, out float Out)
{
    Out = EvaluatePerlinFractalBrownianMotion(Position,Frequency, Octaves);
}

#include "VFXNoise.hlsl"

void CellularNoise3D_float(float3 Position, out float Out)
{
    Out = GenerateCellularNoise3D(Position);
}

void ValueNoise3D_float(float3 Position, out float Out)
{
    Out = GenerateValueNoise3D(Position);
}

void PerlinNoise3D_float(float3 Position, out float Out)
{
    Out = GeneratePerlinNoise3D(Position);
}

#endif //Noise3D_INCLUDED
