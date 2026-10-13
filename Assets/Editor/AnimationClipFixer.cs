#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class AnimationClipFixer : EditorWindow
{
    private GameObject referenceModel;
    private AnimationClip clipToFix;

    [MenuItem("Tools/VAT Animation Fixer")]
    public static void ShowWindow()
    {
        GetWindow<AnimationClipFixer>("VAT Anim Fixer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Add Missing Transform Keys for VAT", EditorStyles.boldLabel);
        
        referenceModel = (GameObject)EditorGUILayout.ObjectField("Reference Model (Bind Pose)", referenceModel, typeof(GameObject), true);
        clipToFix = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", clipToFix, typeof(AnimationClip), false);

        if (GUILayout.Button("Fix Animation Clip"))
        {
            if (referenceModel == null || clipToFix == null)
            {
                Debug.LogError("모델과 애니메이션 클립을 모두 할당해주세요!");
                return;
            }

            FixClip(referenceModel, clipToFix);
            Debug.Log($"{clipToFix.name} 에 누락된 Position/Rotation 키프레임이 추가되었습니다!");
        }
    }

    private void FixClip(GameObject root, AnimationClip clip)
    {
        Transform[] allTransforms = root.GetComponentsInChildren<Transform>();

        foreach (Transform t in allTransforms)
        {
            string path = AnimationUtility.CalculateTransformPath(t, root.transform);
            
            // 빈 경로(Root 자체)는 건너뜀
            if (string.IsNullOrEmpty(path)) continue;

            // Position 곡선이 있는지 확인하고 없으면 현재 모델의 기본 위치로 추가
            if (AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.x")) == null)
            {
                clip.SetCurve(path, typeof(Transform), "m_LocalPosition.x", AnimationCurve.Constant(0f, 0f, t.localPosition.x));
                clip.SetCurve(path, typeof(Transform), "m_LocalPosition.y", AnimationCurve.Constant(0f, 0f, t.localPosition.y));
                clip.SetCurve(path, typeof(Transform), "m_LocalPosition.z", AnimationCurve.Constant(0f, 0f, t.localPosition.z));
            }

            // Rotation 곡선이 있는지 확인하고 없으면 현재 모델의 기본 회전으로 추가
            if (AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.x")) == null)
            {
                clip.SetCurve(path, typeof(Transform), "m_LocalRotation.x", AnimationCurve.Constant(0f, 0f, t.localRotation.x));
                clip.SetCurve(path, typeof(Transform), "m_LocalRotation.y", AnimationCurve.Constant(0f, 0f, t.localRotation.y));
                clip.SetCurve(path, typeof(Transform), "m_LocalRotation.z", AnimationCurve.Constant(0f, 0f, t.localRotation.z));
                clip.SetCurve(path, typeof(Transform), "m_LocalRotation.w", AnimationCurve.Constant(0f, 0f, t.localRotation.w));
            }
        }
        
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
    }
}
#endif