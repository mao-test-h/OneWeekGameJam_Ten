namespace MainContents.ECS
{
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Burst;
    using Unity.Transforms;
    using Unity.Jobs;
    using Unity.Collections;

    /// <summary>
    /// Transform関連用 BarrierSystem
    /// </summary>
    [UpdateAfter(typeof(Transform2DSystem))]
    public sealed class TransformBarrierSystem : BarrierSystem { }

    /// <summary>
    /// 2D用 TransformSystem(平行移動のみ)
    /// </summary>
    [UpdateAfter(typeof(MainRoutineSystem))]
    public sealed unsafe class Transform2DSystem : JobComponentSystem
    {
        [BurstCompile]
        struct TransToMatrix : IJobProcessComponentData<Position2D, LocalToWorld>
        {
            public void Execute([ReadOnly]ref Position2D position, ref LocalToWorld localToWorld)
            {
                // 平行移動行列だけ作る
                var pos = position.Value;
                localToWorld.Value = float4x4.Translate(new float3(pos.x, pos.y, 0.0f));
            }
        }

#if ENABLE_TRANSFORM2D_ROTATION

        /// <summary>
        /// 2D用 TransformSystem(回転処理)
        /// </summary>
        [BurstCompile]
        struct RotTransToMatrix : IJobProcessComponentData<Position2D, RotationZ, LocalToWorld>
        {
            public void Execute([ReadOnly]ref Position2D position, [ReadOnly]ref RotationZ rotationZ, ref LocalToWorld localToWorld)
            {
                float2 pos = position.Value;
                var sin = math.sin(rotationZ.Value);
                var cos = math.cos(rotationZ.Value);
                // 列優先のZ軸回転
                localToWorld.Value = new float4x4
                {
                    c0 = new float4(cos, sin, 0f, 0f),
                    c1 = new float4(-sin, cos, 0f, 0f),
                    c2 = new float4(0f, 0f, 1f, 0f),
                    c3 = new float4(pos.x, pos.y, 0f, 1f)
                };
            }
        }

#endif

        protected override unsafe JobHandle OnUpdate(JobHandle inputDeps)
        {
            var handle = inputDeps;
            handle = new TransToMatrix().Schedule(this, handle);
#if ENABLE_TRANSFORM2D_ROTATION
            handle = new RotTransToMatrix().Schedule(this, handle);
#endif
            return handle;
        }
    }
}
