#if UNITY_EDITOR && UNITY_WEBGL
namespace MyContents.Editor
{
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// 「Graphics Emulation -> WebGL 2.0」の自動設定
    /// </summary>
    /// <remarks>
    /// こちらの設定は何故かビルドやSwitch Platformと行ったタイミングで
    /// いちいちWebGL 1.0に戻ってしまうので、設定を自動化するようにした。
    /// ※GPU Instancingを使うためにWebGL 2.0が必須となっているので。
    /// </remarks>
    [InitializeOnLoad]
    public class AutoGraphicsEmulatorSetting
    {
        static AutoGraphicsEmulatorSetting()
        {
            EditorApplication.update += Update;
        }

        static void Update()
        {
            bool isSuccess = EditorApplication.ExecuteMenuItem("Edit/Graphics Emulation/WebGL 2.0");
            if (isSuccess)
            {
                EditorApplication.update -= Update;
            }
        }
    }
}
#endif
