namespace MainContents.UI
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;

    using UniRx;
    using UniRx.Triggers;

#if ENABLE_FULL_VERSION
    using DG.Tweening;
#endif

    /// <summary>
    /// リザルトUI
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public sealed class ResultUI : MonoBehaviour
    {
        // ------------------------------
        #region // Constants

        // リザルト表示のフォーマット
        const string ResultTextFormat = "Survival Time : {0}\nScore : {1}";

        #endregion // Constants

        // ------------------------------
        #region // Private Fields(Editable)

        [Header("【Components】")]
        [SerializeField] Canvas _canvas = null;
        [SerializeField] Button _retryButton = null;
        [SerializeField] Button _tweetButton = null;
        [SerializeField] Button _rankingButton = null;
        [SerializeField] Text _resultText = null;
#if ENABLE_FULL_VERSION
        [SerializeField] CanvasGroup _canvasGroup = null;

        [Header("【DOTween】")]
        [SerializeField] float _fadeDuration = 1.5f;
#endif

        #endregion // Private Fields(Editable)

        // ------------------------------
        #region // Properties

        /// <summary>
        /// リトライボタン押下
        /// </summary>
        public IObservable<Unit> OnRetryClick { get { return _retryButton.OnClickAsObservable(); } }

        /// <summary>
        /// ツイートボタン押下
        /// </summary>
        public IObservable<Unit> OnTweetCkick { get { return _tweetButton.OnClickAsObservable(); } }

        /// <summary>
        /// ランキングボタン押下
        /// </summary>
        /// <returns></returns>
        public IObservable<Unit> OnRankingCkick { get { return _rankingButton.OnClickAsObservable(); } }

        #endregion // Properties


        // ----------------------------------------------------
        #region // Public Methods

        public void Show()
        {
#if ENABLE_FULL_VERSION
            this._canvas.enabled = true;
            this._canvasGroup.alpha = 0f;
            this._canvasGroup.DOFade(1, this._fadeDuration);
#else
            this._canvas.enabled = true;
#endif
        }

        public void Hide()
        {
            this._canvas.enabled = false;
        }

        public void SetResult(float survivalTime, int score)
        {
            this._resultText.text = string.Format(ResultTextFormat, survivalTime, score);
        }

        #endregion // Public Methods
    }
}
