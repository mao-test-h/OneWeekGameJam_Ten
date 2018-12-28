#pragma warning disable 0649

#if ENABLE_DEBUG

//#define ENABLE_DRAW_COLLIDER2D_SYSTEM

namespace MainContents.DebugUtility
{
    using UnityEngine;
    using Unity.Collections;
    using Unity.Mathematics;

    using MainContents.ECS;
    using MainContents.ScriptableObjects;

    /// <summary>
    /// 衝突範囲のデバッグ表示
    /// </summary>
    public sealed class DrawCollider2D : MonoBehaviour
    {
        [SerializeField] bool _isEnable = false;

        [SerializeField] Collider2DSettings _collider2DSettings;

        [SerializeField] Color _playerColor;
        [SerializeField] Color _playerBulletColor;
        [SerializeField] Color _enemyColor;
        [SerializeField] Color _enemyBulletColor;

        public static NativeArray<SphereCollider2D> PlayerPositions;
        public static NativeArray<SphereCollider2D> PlayerBulletPositions;
        public static NativeArray<SphereCollider2D> EnemyPositions;
        public static NativeArray<SphereCollider2D> EnemyBulletPositions;

        /// <summary>
        /// MonoBehaviour.OnDrawGizmos
        /// </summary>
        void OnDrawGizmos()
        {
            if (!this._isEnable) { return; }

            // コリジョンとなる球体形状の半径
            var radiusSettings = this._collider2DSettings.Radius;
            if (PlayerPositions.IsCreated)
            {
                foreach (var pos in PlayerPositions)
                {
                    Gizmos.color = this._playerColor;
                    Gizmos.DrawSphere(pos.Position.ToVector3(), radiusSettings.Player);
                }
            }

            if (PlayerBulletPositions.IsCreated)
            {
                foreach (var pos in PlayerBulletPositions)
                {
                    Gizmos.color = this._playerBulletColor;
                    Gizmos.DrawSphere(pos.Position.ToVector3(), radiusSettings.PlayerBullet);
                }
            }

            if (EnemyPositions.IsCreated)
            {
                foreach (var pos in EnemyPositions)
                {
                    Gizmos.color = this._enemyColor;
                    Gizmos.DrawSphere(pos.Position.ToVector3(), radiusSettings.Enemy);
                }
            }

            if (EnemyBulletPositions.IsCreated)
            {
                foreach (var pos in EnemyBulletPositions)
                {
                    Gizmos.color = this._enemyBulletColor;
                    Gizmos.DrawSphere(pos.Position.ToVector3(), radiusSettings.EnemyBullet);
                }
            }
        }
    }

    static class DrawCollider2DExtensions
    {
        public static Vector3 ToVector3(this float2 pos)
        {
            return new Vector3(pos.x, pos.y, 0f);
        }
    }
}

namespace MainContents.DebugUtility
{
    using Unity.Entities;

    /// <summary>
    /// 衝突範囲のデバッグ表示(同期用ComponentSystem)
    /// </summary>
    [UpdateAfter(typeof(Unity.Rendering.MeshInstanceRendererSystem))]
    public class DrawCollider2DSystem : ComponentSystem
    {
#if ENABLE_DRAW_COLLIDER2D_SYSTEM
        ComponentGroup _playerGroup;
        ComponentGroup _playerBulletGroup;
        ComponentGroup _enemyGroup;
        ComponentGroup _enemyBulletGroup;

        protected override void OnCreateManager()
        {
            this._playerGroup = base.GetComponentGroup(
                ComponentType.ReadOnly<PlayerTag>(),
                ComponentType.Subtractive<BulletTag>(),
                ComponentType.ReadOnly<SphereCollider2D>());

            this._playerBulletGroup = base.GetComponentGroup(
                ComponentType.ReadOnly<PlayerTag>(),
                ComponentType.ReadOnly<BulletTag>(),
                ComponentType.ReadOnly<SphereCollider2D>());

            this._enemyGroup = base.GetComponentGroup(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.Subtractive<BulletTag>(),
                ComponentType.ReadOnly<SphereCollider2D>());

            this._enemyBulletGroup = base.GetComponentGroup(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadOnly<BulletTag>(),
                ComponentType.ReadOnly<SphereCollider2D>());
        }

        protected override void OnDestroyManager() => this.DisposeBuffers();

        void DisposeBuffers()
        {
            if (DrawCollider2D.PlayerPositions.IsCreated) { DrawCollider2D.PlayerPositions.Dispose(); }
            if (DrawCollider2D.PlayerBulletPositions.IsCreated) { DrawCollider2D.PlayerBulletPositions.Dispose(); }
            if (DrawCollider2D.EnemyPositions.IsCreated) { DrawCollider2D.EnemyPositions.Dispose(); }
            if (DrawCollider2D.EnemyBulletPositions.IsCreated) { DrawCollider2D.EnemyBulletPositions.Dispose(); }
        }

        protected override void OnUpdate()
        {
            this.DisposeBuffers();

            var playerGroupLength = this._playerGroup.CalculateLength();
            var playerBulletGroupLength = this._playerBulletGroup.CalculateLength();
            var enemyGroupLength = this._enemyGroup.CalculateLength();
            var enemyBulletGroupLength = this._enemyBulletGroup.CalculateLength();

            if (playerGroupLength > 0)
            {
                DrawCollider2D.PlayerPositions = new NativeArray<SphereCollider2D>(playerGroupLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                new CopyComponentData<SphereCollider2D>
                {
                    Source = this._playerGroup.GetComponentDataArray<SphereCollider2D>(),
                    Results = DrawCollider2D.PlayerPositions,
                }.Schedule(playerGroupLength, 32)
                .Complete();
            }

            if (playerBulletGroupLength > 0)
            {
                DrawCollider2D.PlayerBulletPositions = new NativeArray<SphereCollider2D>(playerBulletGroupLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                new CopyComponentData<SphereCollider2D>
                {
                    Source = this._playerBulletGroup.GetComponentDataArray<SphereCollider2D>(),
                    Results = DrawCollider2D.PlayerBulletPositions,
                }.Schedule(playerBulletGroupLength, 32)
                .Complete();
            }

            if (enemyGroupLength > 0)
            {
                DrawCollider2D.EnemyPositions = new NativeArray<SphereCollider2D>(enemyGroupLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                new CopyComponentData<SphereCollider2D>
                {
                    Source = this._enemyGroup.GetComponentDataArray<SphereCollider2D>(),
                    Results = DrawCollider2D.EnemyPositions,
                }.Schedule(enemyGroupLength, 32)
                .Complete();
            }

            if (enemyBulletGroupLength > 0)
            {
                DrawCollider2D.EnemyBulletPositions = new NativeArray<SphereCollider2D>(enemyBulletGroupLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                new CopyComponentData<SphereCollider2D>
                {
                    Source = this._enemyBulletGroup.GetComponentDataArray<SphereCollider2D>(),
                    Results = DrawCollider2D.EnemyBulletPositions,
                }.Schedule(enemyBulletGroupLength, 32)
                .Complete();
            }
        }
#else
        protected override void OnUpdate() { }
#endif
    }
}

#else

namespace MainContents.DebugUtility
{
    using UnityEngine;
    public sealed class DrawCollider2D : MonoBehaviour
    {
        // MonoBehaviour.Start
        void Start() { Destroy(this); }
    }
}

#endif
