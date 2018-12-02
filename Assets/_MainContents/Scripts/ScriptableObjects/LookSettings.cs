#pragma warning disable 0649

namespace MainContents.ScriptableObjects
{
    using UnityEngine;
    using MainContents.ECS;

    /// <summary>
    /// 表示設定
    /// </summary>
    [CreateAssetMenu(fileName = "LookSettings", menuName = "ScriptableObject/LookSettings")]
    public sealed class LookSettings : ScriptableObject
    {
        [Header("ECSで表示するRendererの情報")]
        public SpriteMeshInstanceRenderer[] EnemyLooks;
        public SpriteMeshInstanceRenderer PlayerBulletLook;
        public SpriteMeshInstanceRenderer EnemyBulletLook;
    }
}

#if UNITY_EDITOR
namespace MainContents.ScriptableObjects.Editor
{
    using UnityEngine;
    using UnityEditor;
    using Unity.Rendering;

    using MainContents.ECS;

    [CustomEditor(typeof(LookSettings))]
    public sealed class LookSettingsEditor : Editor
    {
        const string IsEnemyLooksOpenKey = nameof(EnemySettingsEditor) + ".IsEnemyLooksOpenKey";
        bool _isEnemyLooksOpen = false;

        void OnEnable()
        {
            this._isEnemyLooksOpen = EditorPrefs.GetBool(IsEnemyLooksOpenKey, false);
        }

        public override void OnInspectorGUI()
        {
            base.serializedObject.Update();
            var so = base.serializedObject;
            var target = (LookSettings)base.target;

            // ---------------------------------------------
            var isEnemyLooksOpen = EditorGUILayout.Foldout(this._isEnemyLooksOpen, "EnemyLooks");
            if (isEnemyLooksOpen != this._isEnemyLooksOpen)
            {
                EditorPrefs.SetBool(IsEnemyLooksOpenKey, isEnemyLooksOpen);
                this._isEnemyLooksOpen = isEnemyLooksOpen;
            }

            if (this._isEnemyLooksOpen)
            {
                var property = base.serializedObject.FindProperty("EnemyLooks");
                var enemyLooks = target.EnemyLooks;
                for (int i = 0; i < property.arraySize; i++)
                {
                    var item = property.GetArrayElementAtIndex(i);
                    EditorGUILayout.LabelField(((EnemyID)i).ToString(), EditorStyles.boldLabel);

                    EditorGUI.indentLevel++;
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("Sprite"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("Material"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(target);
                    }
                    EditorGUI.indentLevel--;
                }
            }

            // ---------------------------------------------
            {
                EditorGUI.BeginChangeCheck();
                var playerBulletLookProperty = base.serializedObject.FindProperty("PlayerBulletLook");
                EditorGUILayout.PropertyField(playerBulletLookProperty, includeChildren: true);
                var enemyBulletLookProperty = base.serializedObject.FindProperty("EnemyBulletLook");
                EditorGUILayout.PropertyField(enemyBulletLookProperty, includeChildren: true);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(target);
                }
            }

            base.serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
