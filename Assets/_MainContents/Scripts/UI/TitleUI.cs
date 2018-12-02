namespace MainContents.UI
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;

    using UniRx;
    using UniRx.Triggers;

    /// <summary>
    /// タイトルUI
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public sealed class TitleUI : MonoBehaviour
    {
        // ------------------------------
        #region // Private Fields(Editable)

        [Header("【Components】")]
        [SerializeField] Canvas _canvas = null;
        [SerializeField] Button _gameStartButton = null;
        [SerializeField] Button _rankingButton = null;

        #endregion // Private Fields(Editable)

        // ------------------------------
        #region // Properties

        /// <summary>
        /// ゲーム開始ボタン押下
        /// </summary>
        public IObservable<Unit> OnGameStartClick { get { return _gameStartButton.OnClickAsObservable(); } }

        /// <summary>
        /// ランキングボタン押下
        /// </summary>
        public IObservable<Unit> OnRankingCkick { get { return _rankingButton.OnClickAsObservable(); } }

        #endregion // Properties


        // ----------------------------------------------------
        #region // Public Methods

        public void Show()
        {
            this._canvas.enabled = true;
        }

        public void Hide()
        {
            this._canvas.enabled = false;
        }

        #endregion // Public Methods
    }
}
