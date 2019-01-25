#pragma warning disable 0649

#if ENABLE_DEBUG
#define SYNC_BARRAGE_PARAM
#endif

namespace MainContents
{
    using System;
    using Unity.Entities;
    using Unity.Rendering;
    using Unity.Mathematics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    using UniRx;

    using MainContents.ECS;
    using MainContents.ScriptableObjects;

    using LocalToWorld = Unity.Transforms.LocalToWorld;
    using GameStatus = GameManager.GameStatus;

    /// <summary>
    /// ECS管理クラス
    /// </summary>
    public unsafe class ECSManager : IDisposable
    {
        // ------------------------------
        #region // Private Fields

        // ScriptableObjects
        LookSettings _lookSettings = null;
        EnemySettings _enemySettings = null;
        Collider2DSettings _collider2DSettings = null;

        // ComponentSystem
        EntityManager _entityManager = null;
        Collision2DSystem _collision2DSystem = null;

        // MeshInstanceRenderer
        MeshInstanceRenderer[] _enemyLooks;
        MeshInstanceRenderer _playerBulletLook;
        MeshInstanceRenderer _enemyBulletLook;

        // Entity Prefabs
        Entity _playerBulletPrefab;
        Entity _enemyBulletPrefab;
        Entity[] _enemyPrefabs;
        Entity _playerPrefab;

        // Entity Prefabs(Generate Event)
        Entity _playerBulletGeneratePrefab;
        Entity _enemyGeneratePrefab;

        // Pointer
        BarrageParam* _barrageParamPtr = null;
        NativeArray<EnemyParam> _enemyParams;

#if SYNC_BARRAGE_PARAM
        IDisposable _debugEveryUpdate = null;
#endif

        #endregion // Private Fields

        // ------------------------------
        #region // Properties

        /// <summary>
        /// EntityManagerの取得
        /// </summary>
        public EntityManager EntityManager { get { return this._entityManager; } }

        /// <summary>
        /// 敵が破壊された時
        /// </summary>
        /// <value>破壊された位置(float2)</value>
        public IObservable<float2> OnDestroyEnemy { get { return this._collision2DSystem.OnDestroyEnemy; } }

        #endregion // Properties


        // ----------------------------------------------------
        #region // Public Methods

        public ECSManager(LookSettings lookSettings, Collider2DSettings collider2DSettings, EnemySettings enemySettings, GameStatus gameStatus)
        {
            // 各種ScriptableObjectの参照を保持
            this._lookSettings = lookSettings;
            this._collider2DSettings = collider2DSettings;
            this._enemySettings = enemySettings;

            // ComponentSystem(JobComponentSystem)にパラメータを渡すために一部の設定データはポインタとして持つ
            {
                // 弾幕設定のポインタを取得
                var barrageParamSize = UnsafeUtility.SizeOf<BarrageParam>();
                this._barrageParamPtr = (BarrageParam*)UnsafeUtility.Malloc(barrageParamSize, UnsafeUtility.AlignOf<BarrageParam>(), Allocator.Persistent);
                UnsafeUtility.MemClear(this._barrageParamPtr, barrageParamSize);
                UnsafeUtility.CopyStructureToPtr<BarrageParam>(ref this._enemySettings.Barrage, this._barrageParamPtr);

                // 敵情報をNativeArrayに格納
                var enemyParamsSource = this._enemySettings.EnemyParams;
                this._enemyParams = new NativeArray<EnemyParam>(enemyParamsSource.Length, Allocator.Persistent);
                this._enemyParams.CopyFrom(enemyParamsSource);
            }

#if SYNC_BARRAGE_PARAM
            // シンボル有効時は毎フレーム構造体の値をポインタにコピーして常時反映されるようにする
            this._debugEveryUpdate = Observable.EveryUpdate().Subscribe(_ =>
            {
                UnsafeUtility.CopyStructureToPtr<BarrageParam>(ref this._enemySettings.Barrage, this._barrageParamPtr);
                this._enemyParams.CopyFrom(this._enemySettings.EnemyParams);
            });
#endif

            // World Settings
            World.Active = new World(Constants.WorldName);
            this._entityManager = World.Active.CreateManager<EntityManager>();
            World.Active.CreateManager(typeof(TransformBarrierSystem));
            World.Active.CreateManager(typeof(DestroyBarrier));
            World.Active.CreateManager(typeof(DestroySystem));
            // ※ポインタを直接object型にはキャスト出来ないので、遠回りでは有るが一つ構造体に挟んで渡す
            var mainRoutineSystem = World.Active.CreateManager<MainRoutineSystem>(
                new MainRoutineSystem.ConstructorParam { BarrageParamPtr = this._barrageParamPtr, EnemyParams = this._enemyParams });
            World.Active.CreateManager(typeof(Transform2DSystem));
            World.Active.CreateManager(typeof(Collider2DUpdate));
            this._collision2DSystem = World.Active.CreateManager<Collision2DSystem>(gameStatus);
            World.Active.CreateManager(typeof(RenderingSystemBootstrap));
#if ENABLE_DEBUG
            World.Active.CreateManager(typeof(DebugUtility.DrawCollider2DSystem));
#endif
            ScriptBehaviourUpdateOrder.UpdatePlayerLoop(World.Active);

            // 敵の最大数
            var maxEnemyNum = this._lookSettings.EnemyLooks.Length;

            // MeshInstanceRendererの生成
            // SpriteをMeshに変換する形で持たせる
            this._enemyLooks = new MeshInstanceRenderer[maxEnemyNum];
            for (int i = 0; i < this._enemyLooks.Length; i++)
            {
                this._enemyLooks[i] = SpriteUtility.CreateMeshInstanceRenderer(this._lookSettings.EnemyLooks[i]);
            }
            this._playerBulletLook = SpriteUtility.CreateMeshInstanceRenderer(this._lookSettings.PlayerBulletLook);
            this._enemyBulletLook = SpriteUtility.CreateMeshInstanceRenderer(this._lookSettings.EnemyBulletLook);

            // コリジョンとなる球体形状の半径と位置のオフセット
            var radiusSettings = this._collider2DSettings.Radius;
            var offsetSettings = this._collider2DSettings.Offset;

            // Create Entity Prefabs
            // ※Prefabベースでの生成を想定しているのでArchetypeは保存しない
            {
                // Player Bullet
                var playerBulletArchetype = this._entityManager.CreateArchetype(
                    ComponentType.Create<BulletTag>(), ComponentType.Create<PlayerTag>(),
                    ComponentType.Create<BulletData>(),
                    // 衝突判定、破棄可能
                    ComponentType.Create<SphereCollider2D>(), ComponentType.Create<Destroyable>(),
                    // Transform
                    ComponentType.Create<Position2D>(),
                    // Built-in ComponentData
                    ComponentType.Create<Prefab>(), ComponentType.Create<LocalToWorld>(), ComponentType.Create<MeshInstanceRenderer>());
                this._playerBulletPrefab = this._entityManager.CreateEntity(playerBulletArchetype);
                this._entityManager.SetComponentData(
                    this._playerBulletPrefab,
                    new SphereCollider2D { Radius = radiusSettings.PlayerBullet, OffsetPosition = offsetSettings.PlayerBullet });
                this._entityManager.SetSharedComponentData(this._playerBulletPrefab, this._playerBulletLook);

                // Enemy Bullet
                var enemyBulletArchetype = this._entityManager.CreateArchetype(
                    ComponentType.Create<BulletTag>(), ComponentType.Create<EnemyTag>(),
                    ComponentType.Create<BulletData>(),
                    // 衝突判定、破棄可能
                    ComponentType.Create<SphereCollider2D>(), ComponentType.Create<Destroyable>(),
                    // Transform
                    ComponentType.Create<Position2D>(),
                    // Built-in ComponentData
                    ComponentType.Create<Prefab>(), ComponentType.Create<LocalToWorld>(), ComponentType.Create<MeshInstanceRenderer>());
                this._enemyBulletPrefab = this._entityManager.CreateEntity(enemyBulletArchetype);
                this._entityManager.SetComponentData(
                    this._enemyBulletPrefab,
                    new SphereCollider2D { Radius = radiusSettings.EnemyBullet, OffsetPosition = offsetSettings.EnemyBullet });
                this._entityManager.SetSharedComponentData(this._enemyBulletPrefab, this._enemyBulletLook);
            }

            this._enemyPrefabs = new Entity[maxEnemyNum];
            {
                // Enemy Prefab
                var enemyArchetype = this._entityManager.CreateArchetype(
                    ComponentType.Create<EnemyTag>(),
                    ComponentType.Create<EnemyData>(),
                    // 衝突判定、破棄可能
                    ComponentType.Create<SphereCollider2D>(), ComponentType.Create<Destroyable>(),
                    // Transform
                    ComponentType.Create<Position2D>(),
                    // Built-in ComponentData
                    ComponentType.Create<Prefab>(), ComponentType.Create<LocalToWorld>(), ComponentType.Create<MeshInstanceRenderer>());
                for (int i = 0; i < this._enemyLooks.Length; i++)
                {
                    var prefab = this._entityManager.CreateEntity(enemyArchetype);
                    this._entityManager.SetComponentData(
                        prefab,
                        new SphereCollider2D { Radius = radiusSettings.Enemy, OffsetPosition = offsetSettings.Enemy });
                    this._entityManager.SetSharedComponentData(prefab, this._enemyLooks[i]);
                    this._enemyPrefabs[i] = prefab;
                }
            }

            {
                // Player prefab
                var playerArchetype = this._entityManager.CreateArchetype(
                    ComponentType.Create<PlayerTag>(),
                    ComponentType.Create<PlayerStatus>(),
                    // 衝突判定、破棄可能
                    ComponentType.Create<SphereCollider2D>(), ComponentType.Create<Destroyable>(),
                    // Transform
                    ComponentType.Create<Position2D>(),
                    // Built-in ComponentData
                    ComponentType.Create<Prefab>());
                this._playerPrefab = this._entityManager.CreateEntity(playerArchetype);
                this._entityManager.SetComponentData(
                    this._playerPrefab,
                    new SphereCollider2D { Radius = radiusSettings.Player, OffsetPosition = offsetSettings.Player });
            }

            {
                // Generate Events
                // ※生成通知は生成したらその場で破棄するのでDestroyableは付けない
                var playerBulletGenerateArchetype = this._entityManager.CreateArchetype(
                    ComponentType.Create<PlayerBulletGenerate>(),
                    ComponentType.Create<BulletData>(),
                    ComponentType.Create<Prefab>());
                this._playerBulletGeneratePrefab = this._entityManager.CreateEntity(playerBulletGenerateArchetype);

                var enemyGenerateArchetype = this._entityManager.CreateArchetype(
                    ComponentType.Create<EnemyGenerate>(),
                    ComponentType.Create<EnemyData>(),
                    ComponentType.Create<Prefab>());
                this._enemyGeneratePrefab = this._entityManager.CreateEntity(enemyGenerateArchetype);
            }

            // ComponentSystem内でEntityの生成を行うSystemに対し、Prefabを渡す。
            // HACK: 渡し方が若干力技感あるのでなんとかしたい..
            mainRoutineSystem.SetPrefabEntities(new MainRoutineSystem.PrefabEntities
            {
                PlayerBulletPrefab = this._playerBulletPrefab,
                EnemyBulletPrefab = this._enemyBulletPrefab,
                EnemyPrefabs = this._enemyPrefabs,
            });
        }

        public void Clear()
        {
            // Prefab以外全て削除する
            using (var entities = this._entityManager.GetAllEntities())
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    if (this._playerPrefab == entity
                        || this._playerBulletPrefab == entity
                        || this._enemyBulletPrefab == entity
                        || this._playerBulletGeneratePrefab == entity
                        || this._enemyGeneratePrefab == entity)
                    {
                        continue;
                    }
                    bool isCompare = false;
                    foreach (var enemyEntity in this._enemyPrefabs)
                    {
                        if (enemyEntity == entity) { isCompare = true; break; }
                    }
                    if (isCompare) { continue; }

                    this._entityManager.DestroyEntity(entity);
                }
            }
        }

        public void Dispose()
        {
            // 生成したインスタンスや確保したアンマネージドメモリなどを破棄
#if SYNC_BARRAGE_PARAM
            this._debugEveryUpdate.Dispose();
            this._debugEveryUpdate = null;
#endif
            // ※Materialも内部的には新規でインスタンス化しているので、最後にDestroyをしないと漏れる。
            foreach (var look in this._enemyLooks)
            {
                UnityEngine.Object.Destroy(look.material);
            }
            UnityEngine.Object.Destroy(this._playerBulletLook.material);
            UnityEngine.Object.Destroy(this._enemyBulletLook.material);

            World.DisposeAllWorlds();
            UnsafeUtility.Free(this._barrageParamPtr, Allocator.Persistent);
            this._enemyParams.Dispose();
        }

        // =========================================
        #region // Create Entity

        /// <summary>
        /// プレイヤー弾の生成
        /// </summary>
        /// <param name="bulletData">弾情報</param>
        /// <param name="position2D">生成位置</param>
        public void CreatePlayerBullet(BulletData bulletData, float2 position2D)
        {
            // 弾が関連する処理の実行順などを踏まえて、ここでは生成用通知であるEntityを生成するだけ。
            // →実際の弾のEntity自体はComponentSystem側で作られる。
            var entity = this._entityManager.Instantiate(this._playerBulletGeneratePrefab);
            this._entityManager.SetComponentData(entity, bulletData);
            this._entityManager.SetComponentData(entity, new PlayerBulletGenerate
            {
                CreatePosition = position2D,
            });
        }

        /// <summary>
        /// 敵の生成
        /// </summary>
        /// <param name="position2D">生成位置</param>
        /// <param name="data">敵情報</param>
        public void CreateEnemy(float2 position2D, EnemyData data)
        {
            // 弾と同様に生成通知用Entityを出すだけ
            var entity = this._entityManager.Instantiate(this._enemyGeneratePrefab);
            this._entityManager.SetComponentData(entity, data);
            this._entityManager.SetComponentData(entity, new EnemyGenerate
            {
                CreatePosition = position2D,
            });
        }

        /// <summary>
        /// プレイヤーの生成
        /// </summary>
        /// <param name="status">プレイヤーの状態</param>
        /// <returns>生成したEntity</returns>
        public Entity CreatePlayer(PlayerStatus status)
        {
            // こちらのみ初期化時に呼ばれることが保証されている前提なので、MonoBehaviour側から直接生成してしまう。
            var entity = this._entityManager.Instantiate(this._playerPrefab);
            this._entityManager.SetComponentData(entity, status);
            return entity;
        }

        #endregion // Create Entity

        #endregion // Public Methods
    }
}
