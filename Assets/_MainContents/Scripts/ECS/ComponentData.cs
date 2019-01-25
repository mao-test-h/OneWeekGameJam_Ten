namespace MainContents.ECS
{
    using System;
    using UnityEngine;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    using PlayerParam = MainContents.ScriptableObjects.PlayerSettings.PlayerParam;

    // ------------------------------
    #region // Entity Tag

    // Entity検索時の識別子
    public struct PlayerTag : IComponentData { }
    public struct EnemyTag : IComponentData { }
    public struct BulletTag : IComponentData { }

    #endregion // Entity Tag

    // ------------------------------
    #region // Generate Event

    public struct PlayerBulletGenerate : IComponentData
    {
        /// <summary>
        /// 生成位置
        /// </summary>
        public float2 CreatePosition;
    }

    public struct EnemyGenerate : IComponentData
    {
        /// <summary>
        /// 生成位置
        /// </summary>
        public float2 CreatePosition;
    }

    #endregion // Generate Event

    // ------------------------------
    #region // Bullet

    /// <summary>
    /// 弾のデータ
    /// </summary>
    public struct BulletData : IComponentData
    {
        /// <summary>
        /// 生存時間(カウント用)
        /// </summary>
        public float Lifespan;

        /// <summary>
        /// 角度(radian)
        /// </summary>
        public float Angle;

        /// <summary>
        /// 弾速
        /// </summary>
        public float Speed;
    }

    #endregion // Bullet

    // ------------------------------
    #region // Enemy

    /// <summary>
    /// 敵のデータ
    /// </summary>
    public struct EnemyData : IComponentData
    {
        /// <summary>
        /// ID
        /// </summary>
        public EnemyID EnemyID;

        /// <summary>
        /// 生成間隔(カウント用)
        /// </summary>
        public float CooldownTimeCounter;

        /// <summary>
        /// DeltaTime(カウント)
        /// </summary>
        public float DeltaTimeCounter;

        /// <summary>
        /// 生成位置
        /// </summary>
        public SpawnPoint SpawnPoint;
    }

    #endregion // Enemy

    // ------------------------------
    #region // Player

    /// <summary>
    /// プレイヤーの状態
    /// </summary>
    public unsafe struct PlayerStatus : IDisposable, IComponentData
    {
        // Jobに渡すことを踏まえてポインタで管理
        float* _barrierPointPtr;
        PlayerParam* _playerParamPtr;

        /// <summary>
        /// 現在のバリアポイント
        /// </summary>
        /// <value></value>
        public float BarrierPoint { get { return *this._barrierPointPtr; } set { *this._barrierPointPtr = value; } }

        /// <summary>
        /// プレイヤーの設定 -> 各種設定地
        /// </summary>
        /// <value>PlayerSettings.PlayerParamの内容のコピー</value>
        /// <remarks>ScriptableObject自体はBlittableじゃないので、必要な設定値をコピーして保持している</remarks>
        public PlayerParam PlayerParam { get { return *this._playerParamPtr; } }

        public PlayerStatus(PlayerParam playerParam)
        {
            var playerParamSize = UnsafeUtility.SizeOf<PlayerParam>();
            this._playerParamPtr = (PlayerParam*)UnsafeUtility.Malloc(playerParamSize, UnsafeUtility.AlignOf<PlayerParam>(), Allocator.Persistent);
            UnsafeUtility.MemClear(this._playerParamPtr, playerParamSize);
            UnsafeUtility.CopyStructureToPtr<PlayerParam>(ref playerParam, this._playerParamPtr);

            this._barrierPointPtr = (float*)UnsafeUtility.Malloc(sizeof(float), UnsafeUtility.AlignOf<float>(), Allocator.Persistent);
            this.BarrierPoint = playerParam.MaxBarrierPoint;
        }

        public void Initialize()
        {
            this.BarrierPoint = this.PlayerParam.MaxBarrierPoint;
        }

        public void Dispose()
        {
            UnsafeUtility.Free(this._barrierPointPtr, Allocator.Persistent);
            UnsafeUtility.Free(this._playerParamPtr, Allocator.Persistent);
        }
    }

    #endregion // Player

    // ------------------------------
    #region // Collider2D

    // ヒット情報 & 衝突プリミティブ 定義
    // ▽参照
    // https://github.com/Unity-Technologies/AnotherThreadECS
    // AnotherThreadECS/Assets/Scripts/ECSCollider.cs

    /// <summary>
    /// 球体形状の衝突プリミティブ
    /// </summary>
    public struct SphereCollider2D : IComponentData
    {
        public float2 Position;
        public float2 OffsetPosition;
        public float Radius;
        public byte IsUpdated;  // boolean
        public bool Intersect(ref SphereCollider2D another)
        {
            if (this.IsUpdated == 0) { return false; }
            var diff = another.Position - this.Position;
            var dist2 = math.lengthsq(diff);
            var rad = this.Radius + another.Radius;
            var rad2 = rad * rad;
            return (dist2 < rad2);
        }
    }

    #endregion // Collider2D

    // ------------------------------
    #region // Destroy

    // ▽参照
    // https://github.com/Unity-Technologies/AnotherThreadECS
    // AnotherThreadECS/Assets/Scripts/ECSDestroy.cs

    /// <summary>
    /// 破棄管理用データ
    /// </summary>
    public struct Destroyable : IComponentData
    {
        public byte Killed; // boolean
        public static Destroyable Kill { get { return new Destroyable { Killed = 1, }; } }
    }

    #endregion // Destroy

    // ------------------------------
    #region // Transform2D

    public struct Position2D : IComponentData
    {
        public float2 Value;
    }

#if ENABLE_TRANSFORM2D_ROTATION
    public struct RotationZ : IComponentData
    {
        public float Value;
    }
#endif

    #endregion // Transform2D
}
