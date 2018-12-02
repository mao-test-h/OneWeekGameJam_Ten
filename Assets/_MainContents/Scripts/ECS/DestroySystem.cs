// ▽参照
// https://github.com/Unity-Technologies/AnotherThreadECS
// AnotherThreadECS/Assets/Scripts/ECSDestroy.cs

namespace MainContents.ECS
{
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Collections;
    using Unity.Burst;

    /// <summary>
    /// 破棄処理用 BarrierSystem
    /// </summary>
    [UpdateAfter(typeof(DestroySystem))]
    public sealed class DestroyBarrier : BarrierSystem { }

    /// <summary>
    /// 破棄処理
    /// </summary>
    [UpdateAfter(typeof(EndFrameBarrier))]
    public sealed class DestroySystem : JobComponentSystem
    {
        [BurstCompile]
        struct DestroyJob : IJobProcessComponentDataWithEntity<Destroyable>
        {
            public EntityCommandBuffer.Concurrent CommandBuffer;
            public void Execute(Entity entity, int index, [ReadOnly]ref Destroyable destroyable)
            {
                if (destroyable.Killed == 0) { return; }
                this.CommandBuffer.DestroyEntity(index, entity);
            }
        }

        [Inject] DestroyBarrier _destroyBarrier = null;

        protected override JobHandle OnUpdate(JobHandle inputDep) => new DestroyJob
        {
            CommandBuffer = this._destroyBarrier.CreateCommandBuffer().ToConcurrent()
        }.Schedule(this, inputDep);
    }
}
