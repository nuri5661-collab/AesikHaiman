using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AesikHaiman.EditorTools
{
    /// <summary>
    /// Assets/Sprites/{상태이름}/ 폴더에 {상태이름}_0, _1, _2... 순서로 넣어둔 스프라이트를 모아
    /// AnimationClip을 만들고, PlayerAnimatorController의 같은 이름 State에 자동으로 연결하는 도구.
    /// 폴더가 비어있거나 없으면 그 상태는 그냥 건너뛰므로, 그림이 준비되는 상태부터 하나씩 넣고
    /// 이 메뉴를 다시 실행하면 됩니다.
    /// ===== 새 상태를 애니메이터에 추가했다면 States 배열에 이름/루프 여부만 추가하면 됨 =====
    /// </summary>
    public static class AnimationClipBuilder
    {
        private const string SpritesRoot = "Assets/Sprites";
        private const string ClipsRoot = "Assets/Animations/Clips";
        private const string AnimatorControllerPath = "Assets/Animations/PlayerAnimatorController.controller";
        private const float FrameRate = 8f;

        // 상태 이름 = 폴더 이름 = AnimatorController의 State 이름. loop만 상태별로 다름
        // (Idle/Move처럼 계속 반복되는 상태만 true, 나머지는 한 번 재생하고 끝).
        private static readonly (string name, bool loop)[] States =
        {
            ("Idle", true),
            ("Move", true),
            ("Jump", false),
            ("Attack1", false),
            ("Attack2", false),
            ("Attack3", false),
            ("Hit", false),
            ("SpecialStab", false),
            ("SpecialOpen", false),
            ("SpecialSpin", true),
        };

        [MenuItem("Tools/Aesik Haiman/Build Animation Clips From Sprites")]
        public static void BuildClips()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (controller == null)
            {
                Debug.LogError("[AnimationClipBuilder] PlayerAnimatorController를 찾을 수 없습니다. 먼저 Setup Test Scene을 실행하세요.");
                return;
            }

            if (!Directory.Exists(ClipsRoot))
                Directory.CreateDirectory(ClipsRoot);

            int builtCount = 0;
            int skippedCount = 0;

            foreach ((string stateName, bool loop) in States)
            {
                Sprite[] frames = LoadFrames(stateName);
                if (frames.Length == 0)
                {
                    Debug.Log($"[AnimationClipBuilder] {stateName}: 스프라이트 없음, 건너뜀 " +
                              $"(Assets/Sprites/{stateName}/{stateName}_0.png 형태로 넣어주세요)");
                    skippedCount++;
                    continue;
                }

                AnimationClip clip = BuildClip(stateName, frames, loop);
                AssignClipToState(controller, stateName, clip);
                Debug.Log($"[AnimationClipBuilder] {stateName}: {frames.Length}프레임으로 클립 생성/갱신 완료");
                builtCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[AnimationClipBuilder] 완료 - 생성/갱신 {builtCount}개, 건너뜀 {skippedCount}개");
        }

        private static Sprite[] LoadFrames(string stateName)
        {
            string folder = $"{SpritesRoot}/{stateName}";
            if (!AssetDatabase.IsValidFolder(folder))
                return Array.Empty<Sprite>();

            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });

            return guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
                .Where(sprite => sprite != null)
                .OrderBy(sprite => ExtractFrameIndex(sprite.name, stateName))
                .ToArray();
        }

        // "Attack1_2" -> 2. 규칙에 맞는 숫자를 못 찾으면 0으로 취급(파일 하나뿐인 상태 등).
        private static int ExtractFrameIndex(string spriteName, string stateName)
        {
            string prefix = stateName + "_";
            if (spriteName.StartsWith(prefix) && int.TryParse(spriteName.Substring(prefix.Length), out int index))
                return index;
            return 0;
        }

        private static AnimationClip BuildClip(string stateName, Sprite[] frames, bool loop)
        {
            string path = $"{ClipsRoot}/{stateName}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            bool isNew = clip == null;
            if (isNew)
                clip = new AnimationClip();

            clip.frameRate = FrameRate;

            var binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            var keyframes = new ObjectReferenceKeyframe[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / FrameRate,
                    value = frames[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            if (isNew)
                AssetDatabase.CreateAsset(clip, path);
            else
                EditorUtility.SetDirty(clip);

            return clip;
        }

        private static void AssignClipToState(AnimatorController controller, string stateName, AnimationClip clip)
        {
            AnimatorStateMachine root = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in root.states)
            {
                if (child.state.name == stateName)
                {
                    child.state.motion = clip;
                    return;
                }
            }

            Debug.LogWarning($"[AnimationClipBuilder] AnimatorController에 '{stateName}' 상태가 없어 연결하지 못했습니다.");
        }
    }
}
