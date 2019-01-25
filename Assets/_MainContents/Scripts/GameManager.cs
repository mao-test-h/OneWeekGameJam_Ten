#if !UNITY_EDITOR && UNITY_WEBGL
#define WEBGL_ONLY
#endif

namespace MainContents
{
    using System;
    using System.Text;
    using UnityEngine;
    using Unity.Entities;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    using UniRx;
    using UniRx.Async;

    using MainContents.ScriptableObjects;
    using MainContents.UI;
    using MainContents.DebugUtility;

#if ENABLE_FULL_VERSION
    using DG.Tweening;
#endif

    /// <summary>
    /// ゲーム管理クラス
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        // ------------------------------
        #region // Defines

        /// <summary>
        /// ゲームステータス
        /// </summary>
        public unsafe struct GameStatus : IDisposable
        {
            // Jobに渡すことを踏まえてポインタで管理
            int* _scorePtr;
            int _defaultAddScore;

            /// <summary>
            /// スコア
            /// </summary>
            public int Score { get { return *this._scorePtr; } set { *this._scorePtr = value; } }

            public GameStatus(int defaultAddScore)
            {
                this._scorePtr = (int*)UnsafeUtility.Malloc(sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Persistent);
                this._defaultAddScore = defaultAddScore;
                this.Score = 0;
            }

            public void Initialize()
            {
                this.Score = 0;
            }

            public void AddScore()
            {
                this.Score += this._defaultAddScore;
            }

            public void AddScore(int addScore)
            {
                this.Score += addScore;
            }

            public void Dispose()
            {
                UnsafeUtility.Free(this._scorePtr, Allocator.Persistent);
            }
        }

        #endregion // Defines

        // ------------------------------
        #region // Private Fields(Editable)

        [Header("【ScriptableObjects】")]
        [SerializeField] LookSettings _lookSettings = null;
        [SerializeField] Collider2DSettings _collider2DSettings = null;
        [SerializeField] EnemySettings _enemySettings = null;

        [Header("【UI】")]
        [SerializeField] TitleUI _titleUI = null;
        [SerializeField] ResultUI _resultUI = null;

        [Header("【References】")]
        [SerializeField] GameObject _playerPrefab = null;

        [Header("【Score Settings】")]
        [SerializeField] int _addScore = 0;

        [Header("【Spawn Point】"), Tooltip("[左上, 左中, 左下, 右上, 右中, 右下, 上左, 上中, 上右, 下左, 下中, 下右]")]
        [SerializeField] Vector2[] _spawnPoints = null; // 敵の生成位置

        [Header("【Audios】")]
        [SerializeField] AudioSource _audioSource = null;
        [SerializeField] AudioClip[] _seAudioClips = null;

        #endregion // Private Fields(Editable)

        // ------------------------------
        #region // Private Fields

        // Components
        Player _player = null;
#if ENABLE_DEBUG
        FPSCounter _fpsCounter = null;
#endif

        // References
        EnemySpawner _enemySpawner = null;
        ECSManager _ecsManager = null;

        // Instance
        GameStatus _gameStatus;

        // Parameter
        float _survivalTime = 0f;   // 生存時間

        #endregion // Private Fields


        // ----------------------------------------------------
        #region // Unity Events

        /// <summary>
        /// MonoBehaviour.Start
        /// </summary>
        void Start()
        {
            this._gameStatus = new GameStatus(this._addScore);

            // ECSの初期化
            this._ecsManager = new ECSManager(this._lookSettings, this._collider2DSettings, this._enemySettings, this._gameStatus);

            // UniRx.AsyncとUnity.Entitiesの和解
            var playerLoop = ScriptBehaviourUpdateOrder.CurrentPlayerLoop;
            PlayerLoopHelper.Initialize(ref playerLoop);

#if ENABLE_DEBUG
            // FPSCounterの起動
            this._fpsCounter = new FPSCounter();
            Observable.EveryUpdate().Subscribe(_ => this._fpsCounter.UpdateInternal()).AddTo(this.gameObject);
#endif

            // プレイヤーのインスタンス化 & 初期化
            var obj = Instantiate<GameObject>(this._playerPrefab);
            this._player = obj.GetComponent<Player>();
            this._player.Initialize(this._ecsManager);
            this._player.OnDestroy.Subscribe(_ => this.OnGameOver()).AddTo(this.gameObject);

            // 敵生成ロジックのインスタンス化
            this._enemySpawner = new EnemySpawner(this._ecsManager, this._enemySettings, this._spawnPoints);

            // UI Event Settings
            this._titleUI.OnGameStartClick.Subscribe(_ => this.OnGameStart()).AddTo(this.gameObject);
            this._titleUI.OnRankingCkick.Subscribe(_ => this.Ranking()).AddTo(this.gameObject);
            this._resultUI.OnRetryClick.Subscribe(_ => this.OnGameStart()).AddTo(this.gameObject);
            this._resultUI.OnRankingCkick.Subscribe(_ => this.Ranking()).AddTo(this.gameObject);
            this._resultUI.OnTweetCkick.Subscribe(_ => this.Tweet()).AddTo(this.gameObject);

            // Audio & Particle Settings
            this._ecsManager.OnDestroyEnemy.Subscribe(pos =>
            {
                // Audio
                this.PlaySE(SE_ID.EnemyDestroy);
                // TODO: Add Particle
            }).AddTo(this.gameObject);

            this._player.OnDestroy.Subscribe(pos =>
            {
                // Audio
                this.PlaySE(SE_ID.PlayerDestroy);
                // TODO: Add Particle
            }).AddTo(this.gameObject);

            this._player.OnShot.Subscribe(pos =>
            {
                // Audio
                this.PlaySE(SE_ID.PlayerShot);
                // TODO: Add Particle
            }).AddTo(this.gameObject);

            // タイトルの表示
            this._titleUI.Show();
        }

        /// <summary>
        /// MonoBehaviour.OnDestroy
        /// </summary>
        void OnDestroy()
        {
            this._ecsManager.Dispose();
            this._enemySpawner.Dispose();
            this._player.Dispose();
            this._gameStatus.Dispose();
        }

        #endregion // Unity Events

        // ----------------------------------------------------
        #region // Private Methods

        #region // Events

        /// <summary>
        /// ゲーム開始イベント
        /// </summary>
        /// <remarks>リトライと併用</remarks>
        void OnGameStart()
        {
            this._ecsManager.Clear();
            this._player.Activate();
            this._enemySpawner.Activate();
            this._survivalTime = Time.realtimeSinceStartup;
            this._gameStatus.Initialize();

            this._titleUI.Hide();
            this._resultUI.Hide();
        }

        /// <summary>
        /// ゲームオーバーイベント
        /// </summary>
        void OnGameOver()
        {
            this._enemySpawner.Deactivate();
            this._survivalTime = (float)Math.Round(Time.realtimeSinceStartup - this._survivalTime, 2);
#if ENABLE_DEBUG
            var builder = new StringBuilder();
            builder.Append("Survival Time : ").Append(this._survivalTime).Append("  Score : ").Append(this._gameStatus.Score);
            Debug.Log(builder.ToString());
#endif
            this._resultUI.SetResult(this._survivalTime, this._gameStatus.Score);
            this._resultUI.Show();
        }

        /// <summary>
        /// Tweetボタン
        /// <summary>
        public void Tweet()
        {
            var builder = new StringBuilder();
            builder.Append("弾幕STG 10").Append("\n");
            builder.AppendFormat("Survival Time : {0}, Score : {1}", this._survivalTime, this._gameStatus.Score).Append("\n");
            Debug.Log(builder.ToString());
#if WEBGL_ONLY && ENABLE_FULL_VERSION
            naichilab.UnityRoomTweet.Tweet("barrage_10", builder.ToString(), "unityroom", "unity1week");
#endif
        }

        /// <summary>
        /// ランキングボタン
        /// </summary>
        public void Ranking()
        {
#if ENABLE_FULL_VERSION
            naichilab.RankingLoader.Instance.SendScoreAndShowRanking(this._survivalTime);
#endif
        }

        #endregion // Events

        public void PlaySE(SE_ID id)
        {
            this._audioSource.PlayOneShot(this._seAudioClips[(int)id]);
        }

        #endregion // Private Methods

#if UNITY_EDITOR
        // ----------------------------------------------------
        #region // Editor Only

        // [左上, 左中, 左下, 右上, 右中, 右下, 上左, 上中, 上右, 下左, 下中, 下右]
        [Header("【Editor Only】")]
        public Transform[] SpawnPointTransforms = null; // 敵の生成位置

        public void SetSpawnPoints(Vector2[] spawnPoints)
        {
            this._spawnPoints = spawnPoints;
        }

        #endregion // Editor Only
#endif
    }
}

#if UNITY_EDITOR
namespace MainContents.Editor
{
    using System.Linq;
    using UnityEngine;
    using UnityEditor;

    [CustomEditor(typeof(GameManager))]
    public sealed class GameManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Set Spawn Points"))
            {
                var target = (GameManager)base.target;
                // 敵の生成位置の座標データはGameObjectでは無く、Vector2[]として持つのでその設定。
                target.SetSpawnPoints(
                    target.SpawnPointTransforms
                        .Select(_ => new Vector2(_.localPosition.x, _.localPosition.y))
                        .ToArray());
                EditorUtility.SetDirty(target);
            }
        }
    }
}
#endif
