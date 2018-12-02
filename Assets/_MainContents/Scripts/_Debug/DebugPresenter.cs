namespace MainContents.DebugUtility
{
    using UnityEngine;
    using UnityEngine.UI;

    using UniRx;

#if ENABLE_DEBUG

    public sealed class DebugPresenter : MonoBehaviour
    {
        // ------------------------------
        #region // Private Fields(Editable)

        [SerializeField] Text _textFps = null;

        #endregion // Private Fields(Editable)

        // ----------------------------------------------------
        #region // Unity Events

        /// <summary>
        /// MonoBehaviour.Start
        /// </summary>
        void Start()
        {
            Observable.EveryUpdate().Subscribe(_ => this.UpdateInternal()).AddTo(this.gameObject);
        }

        #endregion // Unity Events

        // ----------------------------------------------------
        #region // Private Methods

        void UpdateInternal()
        {
            this._textFps.text = FPSCounter.CurrentFps.ToString();
        }

        #endregion // Private Methods
    }

#else

    public sealed class DebugPresenter : MonoBehaviour
    {
        // MonoBehaviour.Start
        void Start() { Destroy(this.gameObject); }
    }

#endif
}
