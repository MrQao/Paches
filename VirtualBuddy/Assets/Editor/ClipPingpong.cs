#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

public class ClipPingpong : MonoBehaviour
{
    [MenuItem("Tools/Animation/Append Full Reversed Copy (Fixed Tangents)")]
    private static void AppendFullReversedCopy()
    {
        var clip = Selection.activeObject as AnimationClip;
        if (!clip)
        {
            EditorUtility.DisplayDialog("提示", "请选择一个 AnimationClip", "OK");
            return;
        }

        var newClip = new AnimationClip { frameRate = clip.frameRate, legacy = clip.legacy };
        float L = clip.length;

        // ------- float 曲线 -------
        var bindings = AnimationUtility.GetCurveBindings(clip);
        foreach (var b in bindings)
        {
            var src = AnimationUtility.GetEditorCurve(clip, b);
            var merged = src.keys.ToList();

            // 反转复制（注意：交换+取反切线；跳过 time==L 的那一帧，避免接缝重复）
            foreach (var k in src.keys)
            {
                float t = L + (L - k.time);
                if (Mathf.Approximately(t, L)) continue; // 跳过接缝重复点

                var k2 = new Keyframe
                {
                    time = t,
                    value = k.value,

                    // 时间反转：slope 需要取反；且左右切线要交换
                    inTangent = -k.outTangent,
                    outTangent = -k.inTangent,

#if UNITY_2018_1_OR_NEWER
                    inWeight = k.outWeight,
                    outWeight = k.inWeight,
                    weightedMode = k.weightedMode
#endif
                };

                merged.Add(k2);
            }

            merged.Sort((a, b2) => a.time.CompareTo(b2.time));
            var dst = new AnimationCurve(merged.ToArray());
            AnimationUtility.SetEditorCurve(newClip, b, dst);
        }

        // ------- 对象引用曲线（Sprite/启用禁用等） -------
        var objBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        foreach (var b in objBindings)
        {
            var src = AnimationUtility.GetObjectReferenceCurve(clip, b);
            var list = src.ToList();

            foreach (var k in src)
            {
                float t = L + (L - k.time);
                if (Mathf.Approximately(t, L)) continue; // 接缝去重
                list.Add(new ObjectReferenceKeyframe { time = t, value = k.value });
            }

            list = list.OrderBy(k => k.time).ToList();
            AnimationUtility.SetObjectReferenceCurve(newClip, b, list.ToArray());
        }

        // ------- 动画事件（避免接缝重复触发） -------
        var evs = AnimationUtility.GetAnimationEvents(clip).ToList();
        var rev = evs
            .Select(e => new AnimationEvent
            {
                time = L + (L - e.time),
                functionName = e.functionName,
                stringParameter = e.stringParameter,
                floatParameter = e.floatParameter,
                intParameter = e.intParameter,
                objectReferenceParameter = e.objectReferenceParameter
            })
            .Where(e => !Mathf.Approximately(e.time, L)) // 接缝去重
            .ToList();

        evs.AddRange(rev);
        evs = evs.OrderBy(e => e.time).ToList();
        AnimationUtility.SetAnimationEvents(newClip, evs.ToArray());

        // 四元数旋转连续性（避免旋转抽搐）
        //AnimationUtility.EnsureQuaternionContinuity(newClip);

        // 保存
        var path = AssetDatabase.GetAssetPath(clip);
        var newPath = path.EndsWith(".anim")
            ? path.Replace(".anim", "_DoublePingPongFixed.anim")
            : path + "_DoublePingPongFixed.anim";
        AssetDatabase.CreateAsset(newClip, newPath);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("完成", $"已生成：\n{newPath}", "OK");
    }
}
#endif