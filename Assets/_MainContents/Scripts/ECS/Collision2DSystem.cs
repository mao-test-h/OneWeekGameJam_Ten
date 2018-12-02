namespace MainContents.ECS
{
    using System;
    using Unity.Entities;
    using Unity.Mathematics;

    using UniRx;

    using GameStatus = GameManager.GameStatus;

    /// <summary>
    /// 衝突判定(総当たり)
    /// </summary>
    [UpdateAfter(typeof(Collider2DUpdate))]
    public sealed class Collision2DSystem : ComponentSystem
    {
        // ------------------------------
        #region // Private Fields

        // ComponentGroup
        ComponentGroup _playerGroup;
        ComponentGroup _enemyGroup;
        ComponentGroup _playerBulletGroup;
        ComponentGroup _enemyBulletGroup;

        // Reference
        GameStatus _gameStatus;
        Subject<float2> _destroyEnemySubject = new Subject<float2>();

        #endregion // Private Fields

        // ------------------------------
        #region // Properties

        /// <summary>
        /// 敵が破壊された時
        /// </summary>
        /// <value>破壊された位置(float2)</value>
        public IObservable<float2> OnDestroyEnemy { get { return this._destroyEnemySubject; } }

        #endregion // Properties


        // ----------------------------------------------------
        #region // Public Methods

        public Collision2DSystem(GameStatus gameStatus)
        {
            this._gameStatus = gameStatus;
        }

        #endregion // Public Methods

        // ----------------------------------------------------
        #region // Protected Methods

        protected override void OnCreateManager()
        {
            this._playerGroup = base.GetComponentGroup(
                ComponentType.ReadOnly<PlayerTag>(),
                ComponentType.Subtractive<BulletTag>(),
                ComponentType.Create<PlayerStatus>(),
                ComponentType.Create<Destroyable>(),
                ComponentType.ReadOnly<SphereCollider2D>());
            this._enemyGroup = base.GetComponentGroup(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.Subtractive<BulletTag>(),
                ComponentType.Create<Destroyable>(),
                ComponentType.ReadOnly<SphereCollider2D>());

            this._playerBulletGroup = base.GetComponentGroup(
                ComponentType.ReadOnly<PlayerTag>(),
                ComponentType.ReadOnly<BulletTag>(),
                ComponentType.Create<Destroyable>(),
                ComponentType.ReadOnly<SphereCollider2D>());
            this._enemyBulletGroup = base.GetComponentGroup(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadOnly<BulletTag>(),
                ComponentType.Create<Destroyable>(),
                ComponentType.ReadOnly<SphereCollider2D>());
        }

        protected override void OnDestroyManager()
        {
            this._destroyEnemySubject.Dispose();
        }

        protected override void OnUpdate()
        {
            if (this._playerGroup.CalculateLength() != 1) { return; }

            // プレイヤーの状態を取得
            var playerStatus = this._playerGroup.GetComponentDataArray<PlayerStatus>()[0];

            // PlayerBullet → Enemy
            {
                var playerBulletGroupLength = this._playerBulletGroup.CalculateLength();
                var enemyGroupLength = this._enemyGroup.CalculateLength();

                var playerBulletColliders = this._playerBulletGroup.GetComponentDataArray<SphereCollider2D>();
                var playerBulletDestroyables = this._playerBulletGroup.GetComponentDataArray<Destroyable>();
                var enemyColliders = this._enemyGroup.GetComponentDataArray<SphereCollider2D>();
                var enemyDestroyables = this._enemyGroup.GetComponentDataArray<Destroyable>();
                for (int i = 0; i < playerBulletGroupLength; i++)
                {
                    var bulletColl = playerBulletColliders[i];
                    for (int j = 0; j < enemyGroupLength; ++j)
                    {
                        var enemyColl = enemyColliders[j];
                        if (enemyColl.Intersect(ref bulletColl))
                        {
                            playerBulletDestroyables[i] = Destroyable.Kill;
                            enemyDestroyables[j] = Destroyable.Kill;

                            // ※今回の実装はOnUpdate内でチェックしているが、データ的にはJobの方でも加算可能な設計。
                            this._gameStatus.AddScore();

                            // こちらはComponentSystem.OnUpdateを前提とした処理。
                            // 仮に破棄周りをJobに回す実装にするならば、managedのobjectは渡すことが出来ないので何かしら別の手段を検討する必要がある。
                            // → 逆に今回の例のようにComponentSystem内ならComponentSystem自体がクラスなので問題はない。
                            this._destroyEnemySubject.OnNext(bulletColl.Position);
                        }
                    }
                }
            }

            // EnemyBullet → Player
            {
                var playerGroupLength = this._playerGroup.CalculateLength();
                var enemyBulletGroupLength = this._enemyBulletGroup.CalculateLength();

                var enemyBulletColliders = this._enemyBulletGroup.GetComponentDataArray<SphereCollider2D>();
                var enemyBulletDestroyables = this._enemyBulletGroup.GetComponentDataArray<Destroyable>();
                var playerColliders = this._playerGroup.GetComponentDataArray<SphereCollider2D>();
                for (int i = 0; i < enemyBulletGroupLength; i++)
                {
                    var bulletColl = enemyBulletColliders[i];
                    for (int j = 0; j < playerGroupLength; ++j)
                    {
                        var playerColl = playerColliders[j];
                        if (playerColl.Intersect(ref bulletColl))
                        {
                            enemyBulletDestroyables[i] = Destroyable.Kill;
                            playerStatus.BarrierPoint -= playerStatus.PlayerParam.HitDamage;
                            playerStatus.BarrierPoint = math.clamp(playerStatus.BarrierPoint, 0f, playerStatus.PlayerParam.MaxBarrierPoint);
                        }
                    }
                }
            }
        }

        #endregion // Protected Methods
    }
}
