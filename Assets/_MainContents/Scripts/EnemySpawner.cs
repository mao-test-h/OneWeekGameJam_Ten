namespace MainContents
{
    using System;
    using UnityEngine;

    using UniRx;
    using MainContents.ECS;
    using MainContents.ScriptableObjects;

    using UnityRandom = UnityEngine.Random;

    /// <summary>
    /// 敵の生成ロジック
    /// </summary>
    public class EnemySpawner : IDisposable
    {
        // ------------------------------
        #region // Private Fields

        // ScriptableObjects
        EnemySettings _enemySettings = null;

        // References
        ECSManager _ecsBoostrap = null;
        IDisposable _createDisposable = null;
        Vector2[] _spawnPoints = null;

        // Cache
        int _maxEnemyID;

        #endregion // Private Fields


        // ----------------------------------------------------
        #region // Public Methods

        public EnemySpawner(ECSManager ecsManager, EnemySettings enemySettings, Vector2[] spawnPoints)
        {
            this._ecsBoostrap = ecsManager;
            this._enemySettings = enemySettings;
            this._spawnPoints = spawnPoints;
            this._maxEnemyID = Enum.GetNames(typeof(EnemyID)).Length;
        }

        public void Activate()
        {
            // 一定間隔ごとに生成していくだけ
            var createTime = TimeSpan.FromSeconds(this._enemySettings.GenerateInterval);
            this._createDisposable = Observable.Interval(createTime).Subscribe(_ =>
            {
                var pointIndex = UnityRandom.Range(0, this._spawnPoints.Length);
                Vector2 point = this._spawnPoints[pointIndex];
                var enemyID = UnityRandom.Range(0, this._maxEnemyID);
                var enemyData = new EnemyData { EnemyID = (EnemyID)enemyID, SpawnPoint = (SpawnPoint)pointIndex };
                this._ecsBoostrap.CreateEnemy(point, enemyData);
            });
        }

        public void Deactivate()
        {
            this._createDisposable.Dispose();
        }

        public void Dispose()
        {
            if (this._createDisposable != null) { this._createDisposable.Dispose(); }
        }

        #endregion // Public Methods
    }
}
