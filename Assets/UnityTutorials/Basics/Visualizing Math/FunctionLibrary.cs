using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;

public static class FunctionLibrary
{
    // 创建一个函数类型，类似于c++中的 std::function<Vector3(float,float,float)>
    public delegate Vector3 Function(float u, float v, float t);

    public enum FunctionName
    {
        Wave,
        MultiWave,
        Ripple,
        Sphere,
        Torus,
    }

    public static Function GetFunction(FunctionName name)
    {
        return Functions[name];
    }

    private static Dictionary<FunctionName, Function> Functions = new Dictionary<FunctionName, Function>()
    {
        { FunctionName.Wave, Wave },
        { FunctionName.MultiWave, MultiWave },
        { FunctionName.Ripple, Ripple },
        { FunctionName.Sphere, Sphere },
        { FunctionName.Torus, Torus }
    };

    // 通过参数方程描述曲面
    private static Vector3 Wave(float u, float v, float t)
    {
        Vector3 p;
        p.x = u;
        p.y = Sin(PI * (u + v + t));
        p.z = v;
        return p;
    }

    private static Vector3 MultiWave(float u, float v, float t)
    {
        Vector3 p;
        p.x = u;
        p.y = (Sin(PI * (u + 0.5f * t)) + 0.5f * Sin(2.0f * PI * (v + t)) + Sin(PI * (u + v + 0.25f * t))) * 0.4f;
        p.z = v;
        return p;
    }

    // 波纹函数
    private static Vector3 Ripple(float u, float v, float t)
    {
        // 经典勾股定理奥
        float d = Sqrt(u * u + v * v);
        Vector3 p;
        p.x = u;
        p.y = Sin(PI * (4.0f * d - t)) / (1.0f + 10.0f * d);
        p.z = v;
        return p;
    }

    private static Vector3 Sphere(float u, float v, float t)
    {
        // 球的半径随着时间改变，这样我们的球就会不断的变化
        // float r = 0.5f + 0.5f * Sin(PI * t);
        // float r = 0.9f + 0.1f * Sin(8.0f * PI * u);
        // float r = 0.9f * 0.1f * Sin(8.0f * PI * v);
        // twisted sphere
        float r = 0.1f * (9.0f + Sin(PI * (6.0f * u + 4.0f * v + t)));
        float phi = 0.5f * PI * v;
        float theta = PI * u;
        float s = r * Cos(phi);
        Vector3 p;
        p.x = s * Sin(theta);
        p.y = r * Sin(phi);
        p.z = s * Cos(theta);
        return p;
    }

    private static Vector3 Torus(float u, float v, float t)
    {
        float r1 = 0.7f + 0.1f * Sin(PI * (6.0f * u + 0.5f * t));
        float r2 = 0.15f + 0.05f * Sin(PI * (8.0f * u + 4.0f * v + 2.0f * t));
        // float r = 0.1f * (9.0f + Sin(PI * (6.0f * u + 4.0f * v + t)));
        float phi = PI * v;
        float theta = PI * u;
        float s = r1 + r2 * Cos(phi);
        Vector3 p;
        p.x = s * Sin(theta);
        p.y = r2 * Sin(phi);
        p.z = s * Cos(theta);
        return p;
    }
}