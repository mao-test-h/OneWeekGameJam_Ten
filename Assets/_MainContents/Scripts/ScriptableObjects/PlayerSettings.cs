namespace MainContents.ScriptableObjects
{
    using System;
    using UnityEngine;

    /// <summary>
    /// プレイヤーの設定
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerSettings", menuName = "ScriptableObject/PlayerSettings")]
    public sealed class PlayerSettings : ScriptableObject
    {
        /// <summary>
        /// 各種設定値
        /// </summary>
        [Serializable]
        public struct PlayerParam
        {
            /// <summary>
            /// 自機の移動速度
            /// </summary>
            public float MoveSpeed;

            /// <summary>
            /// 最大バリア耐久値
            /// </summary>
            public float MaxBarrierPoint;

            /// <summary>
            /// 現在の耐久値
            /// </summary>
            public float RecoveryBarrierPoint;

            /// <summary>
            /// ダメージを受けた際のバリアの減少値
            /// </summary>
            public float HitDamage;

            /// <summary>
            /// 発砲時の消費エネルギー
            /// </summary>
            public float ShotEnergy;
        }

        /// <summary>
        /// 各種設定値
        /// </summary>
        [Header("Parameter")]
        public PlayerParam Param;

        /// <summary>
        /// 弾速
        /// </summary>
        [Header("Bullet")]
        public float BulletSpeed;

        /// <summary>
        /// 弾の生存時間
        /// </summary>
        public float BulletLifespan;

        /// <summary>
        /// 生成間隔
        /// </summary>
        public float BulletCooldownTime;
    }
}
