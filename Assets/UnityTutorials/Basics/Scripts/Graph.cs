using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Basics.FunctionLibrary;

namespace Basics
{
    public class Graph : MonoBehaviour
    {
        private enum TransitionMode
        {
            Cycle,
            Random
        }
        
        [SerializeField] private Transform pointPrefab;

        [SerializeField] private Transform parentNode;
        
        [SerializeField, Range(2, 100)] private int resolution = 2;

        [SerializeField, Range(0, 5)] private float speed = 1.0f;


        [SerializeField] private FunctionLibrary.FunctionName functionName = FunctionLibrary.FunctionName.Wave;

        [SerializeField, Min(0.0f)] private float functionDuration = 5.0f, transitionDuration = 1.0f;
        
        [SerializeField] private TransitionMode transitionMode = TransitionMode.Cycle;

        private List<Transform> m_Points;

        private float m_DurationPassed = 0.0f;

        private bool m_Transitioning = false;

        private FunctionLibrary.FunctionName m_TransitionFunctionName;

        private void SetupPrimitives()
        {
            m_Points = new List<Transform>(resolution * resolution);
            float step = 2.0f / resolution;
            Vector3 scale = Vector3.one * step;
            for (int i = 0; i < m_Points.Capacity; ++i)
            {
                // 这里初始化的时候会进行拷贝
                Transform point = Instantiate(pointPrefab);
                m_Points.Add(point);
                point.localScale = scale;
            }
        }

        private void Awake()
        {
            SetupPrimitives();
        }

        private void Start()
        {
            foreach (Transform point in m_Points)
            {
                point.SetParent(transform);
            }
        }

        private void UpdateFunctionTransition()
        {
            FunctionLibrary.Function from = GetFunction(m_TransitionFunctionName);
            FunctionLibrary.Function to = GetFunction(functionName);
            float progress = m_DurationPassed / transitionDuration;
            float time = speed * Time.time;
            float step = 2.0f / resolution;
            for (int x = 0; x < resolution; ++x)
            {
                float u = (x + 0.5f) * step - 1.0f;
                for (int z = 0; z < resolution; ++z)
                {
                    float v = (z + 0.5f) * step - 1.0f;
                    m_Points[x * resolution + z].localPosition = Morph(u, v, time, from, to, progress);
                }
            }
        }

        private void UpdateFunction()
        {
            FunctionLibrary.Function function = GetFunction(functionName);
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

        void PickNextFunction()
        {
            switch (transitionMode)
            {
                case TransitionMode.Cycle:
                    functionName = GetNextFunction(functionName);
                    break;
                case TransitionMode.Random:
                    functionName = GetRandomFunctionOtherThan(functionName);
                    break;
            }
        }

        // Update is called once per frame
        private void Update()
        {
            m_DurationPassed += Time.deltaTime;
            if (m_Transitioning && m_DurationPassed >= transitionDuration)
            {
                m_DurationPassed -= transitionDuration;
                m_Transitioning = false;
            }
            else if (m_DurationPassed >= functionDuration)
            {
                m_Transitioning = true;
                m_DurationPassed = 0.0f;
                // from transitionFunction to functionName, so transitionFunction is updated
                m_TransitionFunctionName = functionName;
                PickNextFunction();
            }

            if (m_Transitioning)
            {
                UpdateFunctionTransition();
            }
            else
            {
                UpdateFunction();
            }
        }
    }
}