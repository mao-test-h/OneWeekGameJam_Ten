#pragma warning disable 0649

namespace MainContents.ScriptableObjects
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 弾幕設定
    /// </summary>
    [Serializable]
    public struct BarrageParam
    {
        [Serializable]
        public struct CircleParam
        {
            public int BulletCount;
        }
        public CircleParam Circle;

        [Serializable]
        public struct CommonWayParam
        {
            public float Range;
        }
        public CommonWayParam CommonWay;

        [Serializable]
        public struct SpriralCircleParam
        {
            public int BulletCount;
            public int AnimationSpeed;
        }
        public SpriralCircleParam SpriralCircle;

        [Serializable]
        public struct RandomWayParam
        {
            public float Range;
        }
        public RandomWayParam RandomWay;

        [Serializable]
        public struct WaveCircleParam
        {
            public int BulletCount;
            public int AnimationSpeed;
        }
        public WaveCircleParam WaveCircle;

        [Serializable]
        public struct WaveWayParam
        {
            public int BulletCount;
            public float Range;
            public int AnimationSpeed;
        }
        public WaveWayParam WaveWay;
    }

    /// <summary>
    /// 敵の設定
    /// </summary>
    [Serializable]
    public struct EnemyParam
    {
        /// <summary>
        /// 弾の設定
        /// </summary>
        [Serializable]
        public struct Bullet
        {
            /// <summary>
            /// 弾速
            /// </summary>
            public float Speed;

            /// <summary>
            /// 弾の生存時間
            /// </summary>
            public float Lifespan;
        }

        /// <summary>
        /// 移動速度
        /// </summary>
        public float Speed;

        /// <summary>
        /// 本体の生存時間
        /// </summary>
        public float Lifespan;

        /// <summary>
        /// 生成間隔
        /// </summary>
        public float CooldownTime;

        /// <summary>
        /// 弾の設定
        /// </summary>
        public Bullet BulletParam;
    }

    /// <summary>
    /// 敵の設定情報
    /// </summary>
    [CreateAssetMenu(fileName = "EnemySettings", menuName = "ScriptableObject/EnemySettings")]
    public sealed class EnemySettings : ScriptableObject
    {
        /// <summary>
        /// 生成間隔
        /// </summary>
        public float GenerateInterval = 3f;

        /// <summary>
        /// 敵の設定
        /// </summary>
        public EnemyParam[] EnemyParams = new EnemyParam[Enum.GetNames(typeof(EnemyID)).Length];

        /// <summary>
        /// 弾幕設定
        /// </summary>
        public BarrageParam Barrage;
    }
}

#if UNITY_EDITOR
namespace MainContents.ScriptableObjects.Editor
{
    using UnityEngine;
    using UnityEditor;
    using Unity.Rendering;

    using MainContents.ECS;

    [CustomEditor(typeof(EnemySettings))]
    public sealed class EnemySettingsEditor : Editor
    {
        const string IsEnemyParamOpenKey = nameof(EnemySettingsEditor) + ".IsEnemyParamOpenKey";
        bool _isEnemyParamOpen = false;

        void OnEnable()
        {
            this._isEnemyParamOpen = EditorPrefs.GetBool(IsEnemyParamOpenKey, false);
        }

        public override void OnInspectorGUI()
        {
            base.serializedObject.Update();
            var target = (EnemySettings)base.target;

            EditorGUI.BeginChangeCheck();
            var generateIntervalProperty = base.serializedObject.FindProperty("GenerateInterval");
            EditorGUILayout.PropertyField(generateIntervalProperty);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
            }

            // ---------------------------------------------
            var isEnemyParamOpen = EditorGUILayout.Foldout(this._isEnemyParamOpen, "EnemyParams");
            if (isEnemyParamOpen != this._isEnemyParamOpen)
            {
                EditorPrefs.SetBool(IsEnemyParamOpenKey, isEnemyParamOpen);
                this._isEnemyParamOpen = isEnemyParamOpen;
            }

            if (this._isEnemyParamOpen)
            {
                var property = base.serializedObject.FindProperty("EnemyParams");
                var enemyParams = target.EnemyParams;
                for (int i = 0; i < enemyParams.Length; i++)
                {
                    var item = property.GetArrayElementAtIndex(i);
                    EditorGUILayout.LabelField(((EnemyID)i).ToString(), EditorStyles.boldLabel);

                    EditorGUI.indentLevel++;
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("BulletParam"), includeChildren: true);
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("Speed"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("Lifespan"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("CooldownTime"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(target);
                    }
                    EditorGUI.indentLevel--;
                }
            }

            // ---------------------------------------------
            EditorGUI.BeginChangeCheck();
            var barrageProperty = base.serializedObject.FindProperty("Barrage");
            EditorGUILayout.PropertyField(barrageProperty, includeChildren: true);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
            }

            base.serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
