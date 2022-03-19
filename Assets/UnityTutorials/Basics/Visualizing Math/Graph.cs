using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using static FunctionLibrary;

public class Graph : MonoBehaviour
{
    [SerializeField]
    private Transform pointPrefab;

    [SerializeField] 
    private Transform parentNode;

    // resolution不支持动态调整
    [SerializeField, Range(10,100)] 
    private int resolution = 10;

    [SerializeField, Range(0,5)] 
    private float speed = 1.0f;

    private List<Transform> m_Points;
    
    [FormerlySerializedAs("FunctionName")] [SerializeField]
    private FunctionName functionName = FunctionName.Wave;

    private void Awake()
    {
        Debug.Log("Graph Awake...");
        m_Points = new List<Transform>(resolution*resolution);
        // [-1,1] length 2, calculate step
        float step = 2.0f / resolution;
        Vector3 scale = Vector3.one * step;
        // Function function = GetFunction(functionName);
        // x
        for (int i = 0; i < m_Points.Capacity; ++i)
        {
            Transform point = Instantiate(pointPrefab);
            m_Points.Add(point);
            point.localScale = scale;
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
        Debug.Log("Graph Start...");
        foreach (Transform point in m_Points)
        {
            point.SetParent(parentNode);
        }
    }

    // Update is called once per frame
    private void Update()
    {
        Function function = GetFunction(functionName);
        float time = speed * Time.time;
        float step = 2.0f / resolution;
        for (int x = 0; x < resolution; ++x)
        {
            // 每次都需要重新计算u和v，有没有其他方法把他存储一下的？
            float u = (x + 0.5f) * step - 1.0f;
            for (int z = 0; z < resolution; ++z)
            {
                float v = (z + 0.5f) * step - 1.0f;
                m_Points[x * resolution + z].localPosition = function(u, v, time);
            }
        }
    }
}
