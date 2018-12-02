// ヒット情報 & 衝突プリミティブ 定義
// ▽参照
// https://github.com/Unity-Technologies/AnotherThreadECS
// AnotherThreadECS/Assets/Scripts/ECSCollider.cs
namespace MainContents.ECS
{
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Collections;
    using Unity.Burst;

    /// <summary>
    /// 衝突プリミティブの更新処理
    /// </summary>
    [UpdateAfter(typeof(TransformBarrierSystem))]
    public sealed class Collider2DUpdate : JobComponentSystem
    {
        [BurstCompile]
        public struct UpdateJob : IJobProcessComponentData<Position2D, SphereCollider2D>
        {
            public void Execute([ReadOnly] ref Position2D position, ref SphereCollider2D collider)
            {
                collider.Position = collider.OffsetPosition + position.Value;
                collider.IsUpdated = 1;
            }
        }
        protected override JobHandle OnUpdate(JobHandle inputDeps) => new UpdateJob().Schedule(this, inputDeps);
    }
}
