namespace MainContents.ECS
{
    using UnityEngine;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Jobs;
    using Unity.Collections;
    using Unity.Burst;
    using Unity.Collections.LowLevel.Unsafe;

    using MainContents.ScriptableObjects;

    using Random = Unity.Mathematics.Random;

    /// <summary>
    /// 主要処理部分
    /// </summary>
    [UpdateAfter(typeof(DestroySystem))]
    public sealed unsafe class MainRoutineSystem : JobComponentSystem
    {
        // ------------------------------
        #region // Defines

        /// <summary>
        /// コンストラクタのパラメータ
        /// </summary>
        /// <remarks>※ポインタを直接コンストラクタから渡すことが出来ないので、一つ構造体を挟める形で渡す</remarks>
        public struct ConstructorParam
        {
            /// <summary>
            /// 弾幕情報のポインタ
            /// </summary>
            public BarrageParam* BarrageParamPtr;

            /// <summary>
            /// 敵情報
            /// </summary>
            public NativeArray<EnemyParam> EnemyParams;
        }

        public struct PrefabEntities
        {
            public Entity PlayerBulletPrefab;
            public Entity EnemyBulletPrefab;
            public Entity[] EnemyPrefabs;
        }

        #endregion // Defines

        // ------------------------------
        #region // Jobs

        /// <summary>
        /// 弾の移動 Job
        /// </summary>
        [BurstCompile]
        public struct BulletUpdateJob : IJobProcessComponentData<Position2D, BulletData, Destroyable>
        {
            // Time.deltaTime
            public float DeltaTime;
            public void Execute(ref Position2D position2D, ref BulletData bulletData, ref Destroyable destroyable)
            {
                // 時間経過破棄
                bulletData.Lifespan -= this.DeltaTime;
                if (bulletData.Lifespan <= 0f)
                {
                    destroyable = Destroyable.Kill;
                    return;
                }

                // 移動
                float2 tmp = new float2(0f, 0f);
                float angle = bulletData.Angle;
                tmp.x = math.cos(angle);
                tmp.y = math.sin(angle);
                position2D.Value += (tmp * bulletData.Speed * this.DeltaTime);
            }
        }

        /// <summary>
        /// 敵の更新(ロジック) Job
        /// </summary>
        unsafe struct EnemyUpdateJob : IJobParallelFor
        {
            // Time.deltaTime
            [ReadOnly] public float DeltaTime;
            // 敵弾のPrefab 
            [ReadOnly] public Entity EnemyBulletPrefab;
            // プレイヤーの位置
            [ReadOnly] public float2 PlayerPosition;
            // 敵の情報
            [ReadOnly] public NativeArray<EnemyParam> EnemyParams;

            // 乱数生成器のポインタ
            [NativeDisableUnsafePtrRestriction] public Random* RandomPtr;
            // 弾幕情報のポインタ
            [NativeDisableUnsafePtrRestriction] public BarrageParam* BarrageParamPtr;

            // 敵の位置
            public ComponentDataArray<Position2D> EnemyPositions;
            // 敵の情報
            public ComponentDataArray<EnemyData> EnemyData;
            // 敵の破棄フラグ
            public ComponentDataArray<Destroyable> Destroyables;

            // CommandBuffer
            public EntityCommandBuffer.Concurrent CommandBuffer;

            public void Execute(int i)
            {
                float deltaTime = this.DeltaTime;
                var enemyPos = this.EnemyPositions[i];
                var enemyData = this.EnemyData[i];
                var enemyParam = this.EnemyParams[(int)enemyData.EnemyID];

                // 敵の移動
                if (this.Movement(ref i, ref enemyPos, ref enemyParam, ref enemyData, ref deltaTime))
                {
                    return;
                }

                // 弾幕の生成
                this.GenerateBarrage(ref i, ref enemyPos, ref enemyParam, ref enemyData, ref deltaTime);

                enemyData.DeltaTimeCounter += deltaTime;
                this.EnemyData[i] = enemyData;
                this.EnemyPositions[i] = enemyPos;
            }

            bool Movement(ref int jobIndex, ref Position2D enemyPosition, ref EnemyParam param, ref EnemyData data, ref float deltaTime)
            {
                // 破棄管理
                if (param.Lifespan <= data.DeltaTimeCounter)
                {
                    var destroyable = this.Destroyables[jobIndex];
                    destroyable = Destroyable.Kill;
                    this.Destroyables[jobIndex] = destroyable;
                    return true;
                }

                // 移動
                float2 dir = new float2(0f);
                switch ((SpawnPoint)data.SpawnPoint)
                {
                    case SpawnPoint.LeftTop:
                    case SpawnPoint.LeftMiddle:
                    case SpawnPoint.LeftBottom:
                        dir = new float2(1f, 0f);
                        break;

                    case SpawnPoint.RightTop:
                    case SpawnPoint.RightMiddle:
                    case SpawnPoint.RightBottom:
                        dir = new float2(-1f, 0f);
                        break;

                    case SpawnPoint.TopLeft:
                    case SpawnPoint.TopMiddle:
                    case SpawnPoint.TopRight:
                        dir = new float2(0f, -1f);
                        break;

                    case SpawnPoint.BottomLeft:
                    case SpawnPoint.BottomMiddle:
                    case SpawnPoint.BottomRight:
                        dir = new float2(0f, 1f);
                        break;
                }

                enemyPosition.Value += (dir * param.Speed * deltaTime);
                return false;
            }

            void GenerateBarrage(ref int jobIndex, ref Position2D enemyPosition, ref EnemyParam param, ref EnemyData data, ref float deltaTime)
            {
                data.CooldownTimeCounter -= deltaTime;
                if (data.CooldownTimeCounter <= 0f)
                {
                    data.CooldownTimeCounter = param.CooldownTime;
                    switch (data.EnemyID)
                    {
                        case EnemyID.Aiming:
                            this.Aiming(ref jobIndex, ref enemyPosition, ref param, ref data);
                            break;
                        case EnemyID.Circle:
                            this.Circle(ref jobIndex, ref enemyPosition, ref param, ref data);
                            break;
                        case EnemyID.Spiral:
                            this.Spiral(ref jobIndex, ref enemyPosition, ref param, ref data);
                            break;
                        case EnemyID.ThreeWay:
                            // 3 way
                            this.NWay(ref jobIndex, ref enemyPosition, ref param, ref data, Constants.ThreeWayBulletCount);
                            break;
                        case EnemyID.FiveWay:
                            // 5 way
                            this.NWay(ref jobIndex, ref enemyPosition, ref param, ref data, Constants.FiveWayBulletCount);
                            break;
                        case EnemyID.SevenWay:
                            // 7 way
                            this.NWay(ref jobIndex, ref enemyPosition, ref param, ref data, Constants.SevenWayBulletCount);
                            break;
                        case EnemyID.SpiralCircle:
                            this.SpiralCircle(ref jobIndex, ref enemyPosition, ref param, ref data);
                            break;
                        case EnemyID.RandomWay:
                            this.RandomWay(ref jobIndex, ref enemyPosition, ref param, ref data);
                            break;
                        case EnemyID.WaveCircle:
                            this.WaveCircle(ref jobIndex, ref enemyPosition, ref param, ref data);
                            break;
                        case EnemyID.WaveWay:
                            this.WaveWay(ref jobIndex, ref enemyPosition, ref param, ref data);
                            break;
                        default:
                            Debug.Log($"Invalid EnemyID : {data.EnemyID}");
                            this.Aiming(ref jobIndex, ref enemyPosition, ref param, ref data);
                            break;
                    }
                }
            }

            // ----------------------------------------------------
            #region // Barrage Methods

            // 自機に狙い撃ち
            void Aiming(ref int jobIndex, ref Position2D enemyPosition, ref EnemyParam param, ref EnemyData data)
            {
                CommandBuffer.Instantiate(jobIndex, this.EnemyBulletPrefab);
                CommandBuffer.SetComponent(jobIndex, enemyPosition);
                CommandBuffer.SetComponent(jobIndex, new BulletData
                {
                    Speed = param.BulletParam.Speed,
                    Angle = MathHelper.Aiming(enemyPosition.Value, this.PlayerPosition),
                    Lifespan = param.BulletParam.Lifespan,
                });
            }

            // 全方位弾
            void Circle(ref int jobIndex, ref Position2D enemyPosition, ref EnemyParam param, ref EnemyData data)
            {
                int bulletCount = this.BarrageParamPtr->Circle.BulletCount;
                for (int i = 0; i < bulletCount; ++i)
                {
                    CommandBuffer.Instantiate(jobIndex, this.EnemyBulletPrefab);
                    CommandBuffer.SetComponent(jobIndex, enemyPosition);
                    CommandBuffer.SetComponent(jobIndex, new BulletData
                    {
                        Speed = param.BulletParam.Speed,
                        Angle = (i / (float)bulletCount) * (Mathf.PI * 2f),
                        Lifespan = param.BulletParam.Lifespan,
                    });
                }
            }

            // 回転撃ち
            void Spiral(ref int jobIndex, ref Position2D enemyPosition, ref EnemyParam param, ref EnemyData data)
            {
                CommandBuffer.Instantiate(jobIndex, this.EnemyBulletPrefab);
                CommandBuffer.SetComponent(jobIndex, enemyPosition);
                CommandBuffer.SetComponent(jobIndex, new BulletData
                {
                    Speed = param.BulletParam.Speed,
                    Angle = data.DeltaTimeCounter * (Mathf.PI * 2f),
                    Lifespan = param.BulletParam.Lifespan,
                });
            }

            // 前方に分岐撃ち
            void NWay(ref int jobIndex, ref Position2D enemyPosition, ref EnemyParam param, ref EnemyData data, int bulletCount)
            {
                float range = math.radians(this.BarrageParamPtr->CommonWay.Range);
                var angle = MathHelper.Aiming(enemyPosition.Value, this.PlayerPosition);
                int halfBulletCount = (int)(bulletCount / 2);
                for (int i = 0; i < bulletCount; ++i)
                {
                    CommandBuffer.Instantiate(jobIndex, this.EnemyBulletPrefab);
                    CommandBuffer.SetComponent(jobIndex, enemyPosition);
                    CommandBuffer.SetComponent(jobIndex, new BulletData
                    {
                        Speed = param.BulletParam.Speed,
                        Angle = angle + ((i - halfBulletCount) * range),
                        Lifespan = param.BulletParam.Lifespan,
                    });
                }
            }

            // 回転撃ち & 全方位弾
            void SpiralCircle(ref int jobIndex, ref Position2D enemyPosition, ref EnemyParam param, ref EnemyData data)
            {
                int bulletCount = this.BarrageParamPtr->SpriralCircle.BulletCount;
                float time = data.DeltaTimeCounter * this.BarrageParamPtr->SpriralCircle.AnimationSpeed;
                for (int i = 0; i < bulletCount; ++i)
                {
                    CommandBuffer.Instantiate(jobIndex, this.EnemyBulletPrefab);
                    CommandBuffer.SetComponent(jobIndex, enemyPosition);
                    CommandBuffer.SetComponent(jobIndex, new BulletData
                    {
                        Speed = param.BulletParam.Speed,
                        Angle = time + ((i / (float)bulletCount) * (Mathf.PI * 2f)),
                        Lifespan = param.BulletParam.Lifespan,
                    });
                }
            }

            // 前方にランダムに分岐撃ち
            void RandomWay(ref int jobIndex, ref Position2D enemyPosition, ref EnemyParam param, ref EnemyData data)
            {
                float range = this.BarrageParamPtr->RandomWay.Range;
                this.RandomPtr->state += 1;
                range = this.RandomPtr->NextFloat(-range, range);
                range = math.radians(range);

                var angle = MathHelper.Aiming(enemyPosition.Value, this.PlayerPosition);
                CommandBuffer.Instantiate(jobIndex, this.EnemyBulletPrefab);
                CommandBuffer.SetComponent(jobIndex, enemyPosition);
                CommandBuffer.SetComponent(jobIndex, new BulletData
                {
                    Speed = param.BulletParam.Speed,
                    Angle = angle + range,
                    Lifespan = param.BulletParam.Lifespan,
                });
            }

            // 全方位に波撃ち
            void WaveCircle(ref int jobIndex, ref Position2D enemyPosition, ref EnemyParam param, ref EnemyData data)
            {
                int bulletCount = this.BarrageParamPtr->WaveCircle.BulletCount;
                float sinTime = math.sin(data.DeltaTimeCounter * this.BarrageParamPtr->WaveCircle.AnimationSpeed);
                for (int i = 0; i < bulletCount; ++i)
                {
                    CommandBuffer.Instantiate(jobIndex, this.EnemyBulletPrefab);
                    CommandBuffer.SetComponent(jobIndex, enemyPosition);
                    CommandBuffer.SetComponent(jobIndex, new BulletData
                    {
                        Speed = param.BulletParam.Speed,
                        Angle = sinTime + ((i / (float)bulletCount) * (Mathf.PI * 2f)),
                        Lifespan = param.BulletParam.Lifespan,
                    });
                }
            }

            // 前方に波撃ち
            void WaveWay(ref int jobIndex, ref Position2D enemyPosition, ref EnemyParam param, ref EnemyData data)
            {
                int bulletCount = this.BarrageParamPtr->WaveWay.BulletCount;
                float range = math.radians(this.BarrageParamPtr->WaveWay.Range);
                float sinTime = math.sin(data.DeltaTimeCounter * this.BarrageParamPtr->WaveWay.AnimationSpeed);
                var angle = MathHelper.Aiming(enemyPosition.Value, this.PlayerPosition) + sinTime;
                int halfBulletCount = (int)(bulletCount / 2);
                for (int i = 0; i < bulletCount; ++i)
                {
                    CommandBuffer.Instantiate(jobIndex, this.EnemyBulletPrefab);
                    CommandBuffer.SetComponent(jobIndex, enemyPosition);
                    CommandBuffer.SetComponent(jobIndex, new BulletData
                    {
                        Speed = param.BulletParam.Speed,
                        Angle = angle + ((i - halfBulletCount) * range),
                        Lifespan = param.BulletParam.Lifespan,
                    });
                }
            }

            #endregion // Barrage Methods
        }

        #endregion // Jobs

        // ------------------------------
        #region // Private Fields

        // BarrierSystem
        // ※依存関係解決のためにInjectを行う
        [Inject] TransformBarrierSystem _transformBarrierSystem = null;

        // ComponentGroup
        ComponentGroup _playerGroup;
        ComponentGroup _enemyGroup;
        ComponentGroup _playerBulletGenerateGroup;
        ComponentGroup _enemyGenerateGroup;

        // Pointer
        Random* _randomPtr;                     // 乱数生成器
        BarrageParam* _barrageParamPtr;         // 弾幕情報
        NativeArray<EnemyParam> _enemyParams;   // 敵情報

        // Entity Prefabs
        PrefabEntities _prefabEntities;

        #endregion // Private Fields

        // ----------------------------------------------------
        #region // Public Methods

        public MainRoutineSystem(ConstructorParam param)
        {
            this._barrageParamPtr = param.BarrageParamPtr;
            this._enemyParams = param.EnemyParams;
        }

        /// <summary>
        /// Entity Prefabの設定
        /// </summary>
        public void SetPrefabEntities(PrefabEntities prefabEntities)
        {
            this._prefabEntities = prefabEntities;
        }

        #endregion // Public Methods

        // ----------------------------------------------------
        #region // Protected Methods

        protected override void OnCreateManager()
        {
            this._playerGroup = base.GetComponentGroup(
                ComponentType.ReadOnly<PlayerTag>(),
                ComponentType.Create<PlayerStatus>(),
                ComponentType.Subtractive<BulletTag>(),
                ComponentType.Create<Destroyable>(),
                ComponentType.ReadOnly<Position2D>());

            this._enemyGroup = base.GetComponentGroup(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.Create<EnemyData>(),
                ComponentType.Create<Destroyable>(),
                ComponentType.Create<Position2D>());

            this._playerBulletGenerateGroup = base.GetComponentGroup(
                ComponentType.ReadOnly<BulletData>(),
                ComponentType.ReadOnly<PlayerBulletGenerate>());

            this._enemyGenerateGroup = base.GetComponentGroup(
                ComponentType.ReadOnly<EnemyData>(),
                ComponentType.ReadOnly<EnemyGenerate>());

            // 乱数生成器の構造体をJobに渡すためにPtrに変換
            // ※Ptrで渡さないと値渡しになってシード値が維持されないため。
            var random = new Random((uint)System.DateTime.Now.Ticks);
            var randomStrSize = UnsafeUtility.SizeOf<Random>();
            this._randomPtr = (Random*)UnsafeUtility.Malloc(randomStrSize, 16, Allocator.Persistent);
            UnsafeUtility.MemClear(this._randomPtr, randomStrSize);
            UnsafeUtility.CopyStructureToPtr<Random>(ref random, this._randomPtr);
        }

        protected override void OnDestroyManager()
        {
            UnsafeUtility.Free(this._randomPtr, Allocator.Persistent);
        }

        protected override unsafe JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (this._playerGroup.CalculateLength() != 1) { return inputDeps; }

            float deltaTime = Time.deltaTime;
            var handle = inputDeps;
            var commandBuffer = this._transformBarrierSystem.CreateCommandBuffer();

            // プレイヤーの更新(バリア回復処理)
            var playerStatus = this._playerGroup.GetComponentDataArray<PlayerStatus>()[0];
            {
                // バリアが尽きていたら負け
                if (playerStatus.BarrierPoint <= 0f)
                {
                    var playerEntities = this._playerGroup.GetEntityArray();
                    commandBuffer.SetComponent(playerEntities[0], Destroyable.Kill);
                    return handle;
                }
                playerStatus.BarrierPoint += playerStatus.PlayerParam.RecoveryBarrierPoint * deltaTime;
                playerStatus.BarrierPoint = math.clamp(playerStatus.BarrierPoint, 0f, playerStatus.PlayerParam.MaxBarrierPoint);
            }

            // 自機の弾の生成
            {
                var length = this._playerBulletGenerateGroup.CalculateLength();
                var entities = this._playerBulletGenerateGroup.GetEntityArray();
                var generateData = this._playerBulletGenerateGroup.GetComponentDataArray<PlayerBulletGenerate>();
                var bulletData = this._playerBulletGenerateGroup.GetComponentDataArray<BulletData>();
                for (int i = 0; i < length; i++)
                {
                    commandBuffer.Instantiate(this._prefabEntities.PlayerBulletPrefab);
                    commandBuffer.SetComponent(new Position2D { Value = generateData[i].CreatePosition });
                    commandBuffer.SetComponent(bulletData[i]);

                    // 通知用Entityの破棄
                    commandBuffer.DestroyEntity(entities[i]);
                    // 弾のエネルギー消費
                    playerStatus.BarrierPoint -= playerStatus.PlayerParam.ShotEnergy;
                }
            }
            // 敵機の生成
            {
                var length = this._enemyGenerateGroup.CalculateLength();
                var entities = this._enemyGenerateGroup.GetEntityArray();
                var enemyData = this._enemyGenerateGroup.GetComponentDataArray<EnemyData>();
                var generateData = this._enemyGenerateGroup.GetComponentDataArray<EnemyGenerate>();
                for (int i = 0; i < length; i++)
                {
                    var data = enemyData[i];
                    commandBuffer.Instantiate(this._prefabEntities.EnemyPrefabs[(int)data.EnemyID]);
                    commandBuffer.SetComponent(new Position2D { Value = generateData[i].CreatePosition });
                    commandBuffer.SetComponent(data);

                    // 通知用Entityの破棄
                    commandBuffer.DestroyEntity(entities[i]);
                }
            }

            // 敵の更新
            var enemyGroupLength = this._enemyGroup.CalculateLength();
            handle = new EnemyUpdateJob
            {
                DeltaTime = deltaTime,
                EnemyBulletPrefab = this._prefabEntities.EnemyBulletPrefab,
                PlayerPosition = this._playerGroup.GetComponentDataArray<Position2D>()[0].Value,
                EnemyParams = this._enemyParams,

                RandomPtr = this._randomPtr,
                BarrageParamPtr = this._barrageParamPtr,

                EnemyPositions = this._enemyGroup.GetComponentDataArray<Position2D>(),
                Destroyables = this._enemyGroup.GetComponentDataArray<Destroyable>(),
                EnemyData = this._enemyGroup.GetComponentDataArray<EnemyData>(),

                CommandBuffer = commandBuffer.ToConcurrent()
            }.Schedule(enemyGroupLength, 32, handle);

            // 弾の移動
            handle = new BulletUpdateJob { DeltaTime = deltaTime }.Schedule(this, handle);

            return handle;
        }

        #endregion // Protected Methods
    }
}
