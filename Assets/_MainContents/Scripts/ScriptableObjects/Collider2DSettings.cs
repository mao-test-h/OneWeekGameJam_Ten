#pragma warning disable 0649

namespace MainContents.ScriptableObjects
{
    using System;
    using UnityEngine;
    using Unity.Mathematics;

    /// <summary>
    /// コリジョン設定
    /// </summary>
    [CreateAssetMenu(fileName = "Collider2DSettings", menuName = "ScriptableObject/Collider2DSettings")]
    public sealed class Collider2DSettings : ScriptableObject
    {
        /// <summary>
        /// コリジョンとなる球体形状の半径
        /// </summary>
        [Serializable]
        public class RadiusSettings
        {
            public float Player;
            public float Enemy;
            public float PlayerBullet;
            public float EnemyBullet;
        }

        /// <summary>
        /// コリジョン位置のオフセット
        /// </summary>
        [Serializable]
        public class OffsetSettings
        {
            public float2 Player;
            public float2 Enemy;
            public float2 PlayerBullet;
            public float2 EnemyBullet;
        }

        [Header("【Radius】")]
        public RadiusSettings Radius;

        [Header("【Offset】")]
        public OffsetSettings Offset;
    }
}
