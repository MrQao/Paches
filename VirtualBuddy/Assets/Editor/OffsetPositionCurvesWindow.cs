// File: Assets/Editor/OffsetPositionCurvesWindow.cs
// Menu: Tools/Animation/Offset Position Curves
// 功能：对选中 AnimationClip 的指定 Transform 路径的 m_LocalPosition.x/y/z 曲线整体平移
// 兼容 Humanoid 导入的 RootT.x/y/z（当 path 为空且存在 RootT 时也会尝试偏移）

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class OffsetPositionCurvesWindow : EditorWindow
{
    private AnimationClip clip;
    [Tooltip("相对于 AnimationClip 绑定的根的路径，例如 \"Armature/Hips\"；若要改根物体，留空即可")]
    private string transformPath = "";
    [Tooltip("给所有 position 关键帧添加的偏移量（单位：米）")]
    private Vector3 offset = Vector3.zero;

    [MenuItem("Tools/Animation/Offset Position Curves")]
    public static void ShowWindow()
    {
        GetWindow<OffsetPositionCurvesWindow>("Offset Position Curves");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("批量平移 Position 曲线", EditorStyles.boldLabel);
        clip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", clip, typeof(AnimationClip), false);
        transformPath = EditorGUILayout.TextField(new GUIContent("Transform Path", "例如: Armature/Hips；根节点留空"), transformPath);
        offset = EditorGUILayout.Vector3Field(new GUIContent("Offset To Add (x,y,z)"), offset);

        EditorGUILayout.Space();
        if (GUILayout.Button("Apply Offset"))
        {
            if (clip == null)
            {
                EditorUtility.DisplayDialog("提示", "请先选择一个 AnimationClip。", "OK");
                return;
            }
            if (IsReadOnly(clip))
            {
                EditorUtility.DisplayDialog("只读警告",
                    "这个 Clip 可能来自 FBX，处于只读。\n请在 Project 中复制为 .anim 再修改。",
                    "OK");
                return;
            }

            ApplyOffset(clip, transformPath, offset);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "示例（你的数据）：将 x 减 0.012135，y 加 0.00514，z 减 0.62877\n" +
            "则填写 Offset = (-0.012135, 0.00514, -0.62877)。", MessageType.Info);
    }

    private static bool IsReadOnly(AnimationClip c)
    {
        // FBX 内的 Clip 通常是 read-only（不可写）
        return EditorUtility.IsPersistent(c) && AssetDatabase.IsSubAsset(c) == false && AssetDatabase.IsMainAsset(c) == false;
    }

    private static void ApplyOffset(AnimationClip clip, string path, Vector3 offset)
    {
        Undo.RecordObject(clip, "Offset Position Curves");
        bool modified = false;

        // 1) 常规 Transform 本地位移曲线
        modified |= OffsetLocalPositionAxis(clip, path, "m_LocalPosition.x", offset.x);
        modified |= OffsetLocalPositionAxis(clip, path, "m_LocalPosition.y", offset.y);
        modified |= OffsetLocalPositionAxis(clip, path, "m_LocalPosition.z", offset.z);

        // 2) 若是 Humanoid 并且 path 为空，尝试 RootT（导入的人形根位移）
        if (string.IsNullOrEmpty(path))
        {
            modified |= OffsetRootTAxis(clip, "RootT.x", offset.x);
            modified |= OffsetRootTAxis(clip, "RootT.y", offset.y);
            modified |= OffsetRootTAxis(clip, "RootT.z", offset.z);
        }

        if (modified)
        {
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Offset Position Curves] 已应用偏移到：{clip.name}，path='{path}', offset={offset}");
        }
        else
        {
            Debug.LogWarning($"[Offset Position Curves] 未找到可偏移的 position 曲线。请确认 path 是否正确，或该 Clip 是否包含对应曲线。");
        }
    }

    private static bool OffsetLocalPositionAxis(AnimationClip clip, string path, string propertyName, float add)
    {
        var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName);
        var curve = AnimationUtility.GetEditorCurve(clip, binding);
        if (curve == null || curve.keys == null || curve.keys.Length == 0)
            return false;

        var keys = curve.keys;
        for (int i = 0; i < keys.Length; i++)
        {
            var k = keys[i];
            k.value += add;
            keys[i] = k;
        }
        curve.keys = keys;
        AnimationUtility.SetEditorCurve(clip, binding, curve);
        return true;
    }

    private static bool OffsetRootTAxis(AnimationClip clip, string propertyName, float add)
    {
        // Humanoid RootT 在无类型绑定上（typeof(Animator) 也可能），用 null 类型尝试匹配
        // 尝试几种常见绑定
        var candidates = new List<EditorCurveBinding>
        {
            new EditorCurveBinding { path = "", type = typeof(Animator), propertyName = propertyName },
            new EditorCurveBinding { path = "", type = null, propertyName = propertyName },
        };

        bool changed = false;
        foreach (var binding in candidates)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null || curve.keys == null || curve.keys.Length == 0)
                continue;

            var keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                var k = keys[i];
                k.value += add;
                keys[i] = k;
            }
            curve.keys = keys;
            AnimationUtility.SetEditorCurve(clip, binding, curve);
            changed = true;
        }
        return changed;
    }
}
#endif

