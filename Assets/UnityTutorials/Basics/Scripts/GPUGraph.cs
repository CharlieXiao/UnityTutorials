using UnityEngine;
using static Basics.GPUFunctionLibrary;

namespace Basics
{
    public class GPUGraph : MonoBehaviour
    {
        private const int MaxResolution = 1000;

        [SerializeField, Range(10, MaxResolution)]
        private int resolution = 10;

        [SerializeField] private GPUFunctionLibrary.FunctionName function = GPUFunctionLibrary.FunctionName.Sphere;

        private enum Transition
        {
            Cycle,
            Random
        }

        [SerializeField] private Transition transition = Transition.Cycle;

        [SerializeField, Min(0.0f)] private float functionDuration = 1.0f, transitionDuration = 1.0f;

        [SerializeField] private ComputeShader computeShader;

        [SerializeField] private Material material;

        [SerializeField] private Mesh mesh;

        // get variable handler so that we can specify value for shader
        private static readonly int
            PositionId = Shader.PropertyToID("_Positions"),
            ResolutionId = Shader.PropertyToID("_Resolution"),
            TimeId = Shader.PropertyToID("_Time"),
            StepId = Shader.PropertyToID("_Step"),
            TransitionProgressId = Shader.PropertyToID("_TransitionProgress");

        // private int KernelId;

        // 当前动画播放时间
        private float duration;

        // 是否处于转换状态（从一个函数转换到另外一个函数）
        private bool transitioning;

        private GPUFunctionLibrary.FunctionName transitionFunction;

        private ComputeBuffer positionBuffer;

        private const int SizeOfVector3 = 3 * sizeof(float);

        // for hot-reloading
        private void OnEnable()
        {
            // 创建空间（一次拉满）
            positionBuffer = new ComputeBuffer(MaxResolution * MaxResolution, SizeOfVector3);
            // KernelId = computeShader.FindKernel("FunctionKernel");
            // TODO: using reflection to automatically add mapping 
        }

        private void OnDisable()
        {
            positionBuffer.Release();
            // explicitly mark it null 
            positionBuffer = null;
        }

        private void PickNextFunction()
        {
            switch (transition)
            {
                case Transition.Cycle:
                    function = GetNextFunction(function);
                    break;
                case Transition.Random:
                    function = GetRandomFunctionOtherThan(function);
                    break;
            }
        }

        private void UpdateFunctionOnGPU()
        {
            int kernelId = 0;
            duration += Time.deltaTime;
            // TODO change this to state machine
            if (transitioning)
            {
                // Transition state
                if (duration >= transitionDuration)
                {
                    // enter display state
                    transitioning = false;
                    duration -= transitionDuration;
                    kernelId = computeShader.FindKernel(function + "Kernel");
                }
                else
                {
                    float progress = duration / transitionDuration;
                    // 只要处于transition状态，其对应的KernelId就是Morph状态的Kernel
                    kernelId = computeShader.FindKernel(transitionFunction + "To" + function + "Kernel");
                    computeShader.SetFloat(TransitionProgressId, progress);
                }
            }
            else
            {
                // Display state
                if (duration >= functionDuration)
                {
                    // enter transition state
                    transitioning = true;
                    duration = 0.0f;
                    transitionFunction = function;
                    PickNextFunction();
                    kernelId = computeShader.FindKernel(transitionFunction + "To" + function + "Kernel");
                    computeShader.SetFloat(TransitionProgressId, 0.0f);
                }
                else
                {
                    kernelId = computeShader.FindKernel(function + "Kernel");
                }
            }

            float step = 2.0f / resolution;
            // 设置compute shader
            // set data
            computeShader.SetInt(ResolutionId, resolution);
            computeShader.SetFloat(StepId, step);
            computeShader.SetFloat(TimeId, Time.time);
            // bind buffer
            computeShader.SetBuffer(kernelId, PositionId, positionBuffer);
            // dispatch task
            // 向上取整
            int groups = Mathf.CeilToInt(resolution / 8.0f);
            // 将任务划分为groups*groups份，每份是一个8x8的线程执行
            computeShader.Dispatch(kernelId, groups, groups, 1);

            // 设置绘制的shader
            material.SetBuffer(PositionId, positionBuffer);
            material.SetFloat(StepId, step);
            var bounds = new Bounds(Vector3.zero, Vector3.one * (2.0f + step));
            // finally, use shader to draw it
            // count指定为resolution * resolution，使其支持动态变更分辨率
            Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, resolution * resolution);
        }

        // Update is called once per frame
        private void Update()
        {
            UpdateFunctionOnGPU();
        }
    }
}
