#pragma warning disable 0649

#if ENABLE_DEBUG

namespace MainContents.DebugUtility
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 生成位置のエリアチェック
    /// </summary>
    public sealed class DebugShowSpawnArea : MonoBehaviour
    {
        [SerializeField] Color _color;
        [SerializeField] Rect _areaRect;

        void OnDrawGizmosSelected()
        {
            Gizmos.color = this._color;
            var center = this._areaRect.center;
            var size = this._areaRect.size;
            Gizmos.DrawCube(new Vector3(center.x, center.y, 0f), new Vector3(size.x, size.y, 0f));
        }
    }
}

#else

namespace MainContents.DebugUtility
{
    using UnityEngine;
    public sealed class DebugShowSpawnArea : MonoBehaviour
    {
        // MonoBehaviour.Start
        void Start() { Destroy(this); }
    }
}

#endif
