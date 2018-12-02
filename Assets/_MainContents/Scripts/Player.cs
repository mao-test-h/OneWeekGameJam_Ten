//#define SHOW_INPUT_LOG

namespace MainContents
{
    using System;
    using System.Diagnostics;
    using UnityEngine;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    using UniRx;
    using MainContents.ECS;
    using MainContents.ScriptableObjects;

#if ENABLE_FULL_VERSION
    using DG.Tweening;
#endif

    /// <summary>
    /// プレイヤー制御
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed unsafe class Player : MonoBehaviour, IDisposable
    {
        // ------------------------------
        #region // Private Fields(Editable)

        [Header("【ScriptableObjects】")]
        [SerializeField] PlayerSettings _playerSettings = null;

        [Header("【Components】")]
        [SerializeField] SpriteRenderer _spriteMesh = null;
        [SerializeField] MeshRenderer _barrierMesh = null;
        [SerializeField] SpriteRenderer _arrowMesh = null;

        [Header("【References】")]
        [SerializeField] Material _barrierMaterial = null;

        [Header("【Movable Area】")]
        [SerializeField] Rect _movableArea;

#if ENABLE_FULL_VERSION
        [Header("【DOTween】")]
        [SerializeField] float _shotShakeDuration = 0.1f;
        [SerializeField] float _shotShakeStrength = 0.2f;

        [SerializeField] float _loseShakeDuration = 1f;
        [SerializeField] float _loseShakeStrength = 1f;
#endif

        #endregion // Private Fields(Editable)

        // ------------------------------
        #region // Private Fields

        // Components
        Camera _mainCamera = null;
        Transform _arrowTrs = null;

        // References
        ECSManager _ecsManager = null;
        IDisposable _updateDisposable = null;

        // Instance
        Material _barrierMaterialInstance = null;
        PlayerStatus _playerStatus;

        // ECS
        Entity _playerEntity;

        // Parameter
        float _bulletCooldownTimeCount = 0f;

        // Cache
        float _mainCameraPosZ = 0f;
        int _barrierMaterialAlphaID;

        // Subject
        Subject<float2> _destroySubject = new Subject<float2>();
        Subject<float2> _shotSubject = new Subject<float2>();

        #endregion // Private Fields

        // ------------------------------
        #region // Properties

        /// <summary>
        /// 自身が破壊された時
        /// </summary>
        /// <value>破壊された位置(float2)</value>
        public IObservable<float2> OnDestroy { get { return _destroySubject; } }

        /// <summary>
        /// ショット時
        /// </summary>
        /// <value>ショット位置(float2)</value>
        public IObservable<float2> OnShot { get { return this._shotSubject; } }

        #endregion // Properties


        // ----------------------------------------------------
        #region // Public Methods

        public void Initialize(ECSManager ecsManager)
        {
            // 初回1回のみ呼ばれる想定。
            // インスタンスの生成などを行う。
            this._mainCamera = Camera.main;
            this._arrowTrs = this._arrowMesh.transform;
            this._mainCameraPosZ = this._mainCamera.transform.localPosition.z;
            this._ecsManager = ecsManager;

            this._barrierMaterialAlphaID = Shader.PropertyToID(Constants.BarrierMaterialAlpha);

            this._barrierMaterialInstance = new Material(this._barrierMaterial);
            this._barrierMesh.material = this._barrierMaterialInstance;
            this._playerStatus = new PlayerStatus(this._playerSettings.Param);

            // 終わったら非表示にしておく
            this.Hide();
        }

        public void Activate()
        {
            // ゲーム開始時に呼ばれる想定の物。
            // 対となるEntityを生成して自身の表示も行う。
            this.transform.localPosition = Vector3.zero;
            this.Show();
            this._playerStatus.Initialize();
            this._playerEntity = this._ecsManager.CreatePlayer(this._playerStatus);

            // 毎フレーム更新の開始
            this._updateDisposable = Observable.EveryUpdate().Subscribe(_ => this.UpdateInternal());
        }

        public void Dispose()
        {
            // 生成したインスタンスや確保したアンマネージドメモリなどを破棄
            this._playerStatus.Dispose();
            UnityEngine.Object.Destroy(this._barrierMaterialInstance);
            this._barrierMaterialInstance = null;

            if (this._updateDisposable != null)
            {
                this._updateDisposable.Dispose();
                this._updateDisposable = null;
            }
            this._destroySubject.Dispose();
            this._shotSubject.Dispose();
        }

        #endregion // Public Methods

        // ----------------------------------------------------
        #region // Private Methods

        void UpdateInternal()
        {
            // ゲーム終了
            if (Input.GetButtonDown(Constants.Cancel))
            {
                Application.Quit();
                return;
            }

            var trs = this.transform;
            var localPos = trs.localPosition;

            // 生存確認及び死亡時の停止処理
            if (!this._ecsManager.EntityManager.Exists(this._playerEntity))
            {
#if ENABLE_FULL_VERSION
                this._mainCamera.transform.DOShakePosition(
                    this._loseShakeDuration,
                    strength: this._loseShakeStrength);
#endif
                this.Hide();
                this._updateDisposable.Dispose();
                this._updateDisposable = null;
                this._destroySubject.OnNext(new float2(localPos.x, localPos.y));
                return;
            }

            var deltaTime = Time.deltaTime;

            // Player Controller
            {
                // 移動
                // FIXME: Chromeにてローカルで実行(Build And Run)するとバグるので中尉。
                //      - ローカルならFirefoxにて実行、若しくはunityroomにアップして実行することで解決。
                //      - 後はChromeならシークレットで実行することでも直る。(恐らくは拡張が影響している..?)
                //      - 参考? : https://forum.unity.com/threads/webgl-input-getkey-problem-keyboard-keys-can-get-stuck.582640/
                var moveSpeed = this._playerSettings.Param.MoveSpeed;
                var moveH = Input.GetAxis(Constants.MoveHorizontal);
                var moveV = Input.GetAxis(Constants.MoveVertical);
                this.DebugInputLog($"Move... {moveH} - {moveV}");
                localPos += new Vector3(moveH, moveV) * deltaTime * moveSpeed;

                // 移動範囲の制御
                if (localPos.x <= this._movableArea.xMin)
                {
                    localPos.x = this._movableArea.xMin;
                }
                else if (localPos.x > this._movableArea.xMax)
                {
                    localPos.x = this._movableArea.xMax;
                }
                if (localPos.y <= this._movableArea.yMin)
                {
                    localPos.y = this._movableArea.yMin;
                }
                else if (localPos.y > this._movableArea.yMax)
                {
                    localPos.y = this._movableArea.yMax;
                }
                trs.localPosition = localPos;

                // 向き
                Vector3 mousePos = Input.mousePosition;
                mousePos.z = -this._mainCameraPosZ;
                mousePos = this._mainCamera.ScreenToWorldPoint(mousePos);
                float angle = MathHelper.Aiming(localPos, mousePos);
                this.DebugInputLog($"Angle... {angle}");
                // 矢印出向きを可視化する
                this._arrowTrs.localRotation = Quaternion.FromToRotation(Vector3.up, (mousePos - localPos).normalized);

                // ショット
                if (Input.GetButton(Constants.Shot))
                {
                    this._bulletCooldownTimeCount -= deltaTime;
                    if (this._bulletCooldownTimeCount <= 0f)
                    {
#if ENABLE_FULL_VERSION
                        this._mainCamera.transform.DOShakePosition(
                            this._shotShakeDuration,
                            strength: this._shotShakeStrength);
#endif
                        this._bulletCooldownTimeCount = this._playerSettings.BulletCooldownTime;
                        var bulletData = new BulletData
                        {
                            Speed = this._playerSettings.BulletSpeed,
                            Angle = angle,
                            Lifespan = this._playerSettings.BulletLifespan,
                        };
                        var bulletPos = new float2(localPos.x, localPos.y);
                        this._ecsManager.CreatePlayerBullet(bulletData, bulletPos);
                        this._shotSubject.OnNext(bulletPos);
                    }
                }
                else
                {
                    // この処理故に頑張れば連射可能だが...恐らくはエネルギー切れで直ぐに死ぬのでバランス崩壊には繋がらないかと思われるので放置。
                    this._bulletCooldownTimeCount = 0f;
                }
            }

            {
                // Entityとの位置同機
                // CHECK: ポインタ渡しとどちらが早い?
                this._ecsManager.EntityManager.SetComponentData(
                    this._playerEntity,
                    new Position2D { Value = new float2(trs.localPosition.x, trs.localPosition.y) });
            }

            {
                // バリアにエネルギーを反映
                float currentPoint = this._playerStatus.BarrierPoint / this._playerSettings.Param.MaxBarrierPoint;
                this._barrierMaterialInstance.SetFloat(this._barrierMaterialAlphaID, currentPoint);
            }
        }

        void Show()
        {
            this._spriteMesh.enabled = this._barrierMesh.enabled = this._arrowMesh.enabled = true;
        }

        void Hide()
        {
            this._spriteMesh.enabled = this._barrierMesh.enabled = this._arrowMesh.enabled = false;
        }

        #endregion // Private Methods

        // ----------------------------------------------------
        #region // Debug

        [Conditional("SHOW_INPUT_LOG")]
        void DebugInputLog(string message)
        {
            UnityEngine.Debug.Log($"<color=green>{message}</color>");
        }

        #endregion // Debug
    }
}
