using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using AesikHaiman.Player;
using AesikHaiman.Systems;
using AesikHaiman.Skills;
using AesikHaiman.Skills.Examples;
using AesikHaiman.Skills.SpaGuardian;
using AesikHaiman.Enemy;
using AesikHaiman.Game;
using AesikHaiman.UI;
using AesikHaiman.CameraControl;
using AesikHaiman.DebugTools;
using AesikHaiman.Audio;
using AesikHaiman.Combat;
using AesikHaiman.Stage;

namespace AesikHaiman.EditorTools
{
    /// <summary>
    /// 1~2주차 "기반 시스템" 단계용 자동 셋업 도구.
    /// - Animator Controller 골격 생성
    /// - 테스트용 플레이스홀더 씬 구성(플레이어 캡슐, 바닥, 카메라, 디버그 UI)
    /// 메뉴(Tools > Aesik Haiman > Setup Test Scene) 또는
    /// -executeMethod AesikHaiman.EditorTools.ProjectSetupTool.SetupProject 로 실행 가능.
    /// </summary>
    public static class ProjectSetupTool
    {
        private const string AnimatorControllerPath = "Assets/Animations/PlayerAnimatorController.controller";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string TestDummyPrefabPath = "Assets/Prefabs/TestDummy.prefab";
        private const string GuardianGolemPrefabPath = "Assets/Prefabs/GuardianGolem.prefab";
        private const string ProjectilePrefabPath = "Assets/Prefabs/Projectile_Droplet.prefab";
        private const string HitEffectPrefabPath = "Assets/Prefabs/HitEffect.prefab";
        private const string BossPrefabPath = "Assets/Prefabs/BossMonster.prefab";

        private const string SlimeSpritePath = "Assets/Sprites/Monsters/slime.png";
        private const string GuardianGolemSpritePath = "Assets/Sprites/Monsters/guardian_golem.png";
        private const string BossGolemSpritePath = "Assets/Sprites/Monsters/boss_golem.png";
        private const string BackgroundSpritePath = "Assets/Sprites/Backgrounds/onsen_town.png";
        private const string GroundSpritePath = "Assets/Sprites/Ground/stone_path.png";
        private const string SoftDotSpritePath = "Assets/Sprites/VFX/soft_dot.png";

        private const string SplashEffectPrefabPath = "Assets/Prefabs/FX_Splash.prefab";
        private const string EggEffectPrefabPath = "Assets/Prefabs/FX_EggThrow.prefab";
        private const string WaveEffectPrefabPath = "Assets/Prefabs/FX_SikhyeWave.prefab";
        private const string HotSpringEffectPrefabPath = "Assets/Prefabs/FX_HotSpring.prefab";
        private const string HealTickEffectPrefabPath = "Assets/Prefabs/FX_HealTick.prefab";

        // Perchance로 받은 스킬 이펙트 일러스트 (배경 제거 완료본)
        private const string SplashArtPath = "Assets/Sprites/VFX/fx_splash.png";
        private const string EggArtPath = "Assets/Sprites/VFX/fx_egg.png";
        private const string WaveArtPath = "Assets/Sprites/VFX/fx_wave.png";
        private const string HotSpringArtPath = "Assets/Sprites/VFX/fx_hotspring.png";
        private const string HealTickArtPath = "Assets/Sprites/VFX/fx_healtick.png";

        // 스파 가디언 스킬 5종의 VFX 프리팹을 한 번에 들고 다니기 위한 묶음.
        private struct SkillEffectPrefabs
        {
            public GameObject Splash;
            public GameObject Egg;
            public GameObject Wave;
            public GameObject HotSpring;
            public GameObject HealTick;
        }

        // ===== 맵 4개 구조 (스프린트 10 재작업) =====
        // Stage1: 몹 3마리를 전부 잡아야 문을 부술 수 있음 -> Stage2
        // Stage2: 몹 4마리를 전부 잡아야 포탈이 열림 -> Stage3
        // Stage3: 몹 4마리를 전부 잡아야 문을 부술 수 있음 -> Stage4
        // Stage4: 보스 1 + 몹 2. 보스를 처치하면 게임 클리어.
        private const string Stage1ScenePath = "Assets/Scenes/Stage1.unity";
        private const string Stage2ScenePath = "Assets/Scenes/Stage2.unity";
        private const string Stage3ScenePath = "Assets/Scenes/Stage3.unity";
        private const string Stage4ScenePath = "Assets/Scenes/Stage4.unity";
        private const string Stage1SceneName = "Stage1";
        private const string Stage2SceneName = "Stage2";
        private const string Stage3SceneName = "Stage3";
        private const string Stage4SceneName = "Stage4";

        [MenuItem("Tools/Aesik Haiman/Setup Test Scene")]
        public static void SetupProject()
        {
            Debug.Log("[ProjectSetupTool] 셋업 시작");

            AnimatorController controller = BuildAnimatorController();

            int groundLayer = GetOrCreateLayer("Ground");
            int enemyLayer = GetOrCreateLayer("Enemy");

            GameObject hitEffectPrefab = BuildHitEffectPrefab();
            GameObject projectilePrefab = BuildProjectilePrefab(hitEffectPrefab);
            GameObject testDummyPrefab = BuildTestDummyPrefab(enemyLayer);
            GameObject golemPrefab = BuildGuardianGolemPrefab(enemyLayer);
            GameObject bossPrefab = BuildBossPrefab(enemyLayer, projectilePrefab);
            SkillEffectPrefabs skillEffects = BuildSkillEffectPrefabs();

            BuildStage1(controller, groundLayer, enemyLayer, testDummyPrefab, projectilePrefab, hitEffectPrefab, skillEffects);
            BuildStage2(controller, groundLayer, enemyLayer, testDummyPrefab, golemPrefab, projectilePrefab, hitEffectPrefab, skillEffects);
            BuildStage3(controller, groundLayer, enemyLayer, testDummyPrefab, golemPrefab, projectilePrefab, hitEffectPrefab, skillEffects);
            BuildStage4(controller, groundLayer, enemyLayer, testDummyPrefab, golemPrefab, projectilePrefab, hitEffectPrefab, bossPrefab, skillEffects);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(Stage1ScenePath, true),
                new EditorBuildSettingsScene(Stage2ScenePath, true),
                new EditorBuildSettingsScene(Stage3ScenePath, true),
                new EditorBuildSettingsScene(Stage4ScenePath, true),
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ProjectSetupTool] 셋업 완료 (맵 4개: Stage1~4)");
        }

        // ============================================================
        // Animator Controller 골격
        // 이후 스킬 애니메이션(7주차~)을 추가할 때:
        // - 스킬별 State를 추가하고, AttackTrigger 방식처럼 트리거+조건 조합으로 전이를 연결
        // - 실제 AnimationClip은 각 State의 motion 필드에 나중에 연결하면 됨
        // ============================================================
        private static AnimatorController BuildAnimatorController()
        {
            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (existing != null && HasParameter(existing, "SpecialStabTrigger"))
            {
                Debug.Log("[ProjectSetupTool] 기존 AnimatorController 재사용");
                return existing;
            }

            if (existing != null)
            {
                // 이전 버전 AnimatorController에는 우산 콤보(SpecialStab/Open/Spin) 파라미터가 없으므로 새로 생성
                Debug.Log("[ProjectSetupTool] AnimatorController에 새 파라미터가 필요해 다시 생성합니다");
                AssetDatabase.DeleteAsset(AnimatorControllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorControllerPath);
            AnimatorStateMachine root = controller.layers[0].stateMachine;

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("JumpTrigger", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("AttackTrigger", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("ComboIndex", AnimatorControllerParameterType.Int);
            controller.AddParameter("HitTrigger", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("SpecialStabTrigger", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("SpecialOpenTrigger", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("SpecialSpinTrigger", AnimatorControllerParameterType.Trigger);

            AnimatorState idle = root.AddState("Idle");
            AnimatorState move = root.AddState("Move");
            AnimatorState jump = root.AddState("Jump");
            AnimatorState attack1 = root.AddState("Attack1");
            AnimatorState attack2 = root.AddState("Attack2");
            AnimatorState attack3 = root.AddState("Attack3");
            AnimatorState hit = root.AddState("Hit");
            AnimatorState specialStab = root.AddState("SpecialStab");
            AnimatorState specialOpen = root.AddState("SpecialOpen");
            AnimatorState specialSpin = root.AddState("SpecialSpin");

            root.defaultState = idle;

            // Idle <-> Move (Speed 기준)
            AnimatorStateTransition idleToMove = idle.AddTransition(move);
            idleToMove.AddCondition(AnimatorConditionMode.Greater, 0.05f, "Speed");
            idleToMove.hasExitTime = false;
            idleToMove.duration = 0.1f;

            AnimatorStateTransition moveToIdle = move.AddTransition(idle);
            moveToIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, "Speed");
            moveToIdle.hasExitTime = false;
            moveToIdle.duration = 0.1f;

            // Any -> Jump / Jump -> Idle(착지)
            AnimatorStateTransition anyToJump = root.AddAnyStateTransition(jump);
            anyToJump.AddCondition(AnimatorConditionMode.If, 0, "JumpTrigger");
            anyToJump.hasExitTime = false;
            anyToJump.duration = 0.05f;

            AnimatorStateTransition jumpToIdle = jump.AddTransition(idle);
            jumpToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            jumpToIdle.hasExitTime = false;
            jumpToIdle.duration = 0.1f;

            // Any -> Attack1/2/3 (AttackTrigger + ComboIndex 조합으로 콤보 단수 판별)
            AddComboAttackTransition(root, attack1, 0);
            AddComboAttackTransition(root, attack2, 1);
            AddComboAttackTransition(root, attack3, 2);

            AnimatorStateTransition attack1ToIdle = attack1.AddTransition(idle);
            attack1ToIdle.hasExitTime = true;
            attack1ToIdle.exitTime = 1f;
            attack1ToIdle.duration = 0.1f;

            AnimatorStateTransition attack2ToIdle = attack2.AddTransition(idle);
            attack2ToIdle.hasExitTime = true;
            attack2ToIdle.exitTime = 1f;
            attack2ToIdle.duration = 0.1f;

            AnimatorStateTransition attack3ToIdle = attack3.AddTransition(idle);
            attack3ToIdle.hasExitTime = true;
            attack3ToIdle.exitTime = 1f;
            attack3ToIdle.duration = 0.1f;

            // Any -> Hit / Hit -> Idle
            AnimatorStateTransition anyToHit = root.AddAnyStateTransition(hit);
            anyToHit.AddCondition(AnimatorConditionMode.If, 0, "HitTrigger");
            anyToHit.hasExitTime = false;
            anyToHit.duration = 0.05f;

            AnimatorStateTransition hitToIdle = hit.AddTransition(idle);
            hitToIdle.hasExitTime = true;
            hitToIdle.exitTime = 1f;
            hitToIdle.duration = 0.1f;

            // Any -> SpecialStab/Open/Spin (우산 콤보: 찌르기/펼치기/회전), 각 상태 -> Idle
            AddTriggerTransition(root, specialStab, "SpecialStabTrigger", idle);
            AddTriggerTransition(root, specialOpen, "SpecialOpenTrigger", idle);
            AddTriggerTransition(root, specialSpin, "SpecialSpinTrigger", idle);

            Debug.Log("[ProjectSetupTool] AnimatorController 생성 완료: " + AnimatorControllerPath);
            return controller;
        }

        private static bool HasParameter(AnimatorController controller, string paramName)
        {
            foreach (AnimatorControllerParameter p in controller.parameters)
                if (p.name == paramName) return true;
            return false;
        }

        private static void AddComboAttackTransition(AnimatorStateMachine root, AnimatorState target, int comboIndex)
        {
            AnimatorStateTransition transition = root.AddAnyStateTransition(target);
            transition.AddCondition(AnimatorConditionMode.If, 0, "AttackTrigger");
            transition.AddCondition(AnimatorConditionMode.Equals, comboIndex, "ComboIndex");
            transition.hasExitTime = false;
            transition.duration = 0.05f;
            transition.canTransitionToSelf = false;
        }

        // Any -> target(트리거 조건) / target -> returnState(exitTime) 왕복 전이를 한 번에 만드는 헬퍼.
        // Hit, SpecialStab/Open/Spin처럼 "트리거 하나로 들어갔다가 곧바로 Idle로 돌아오는" 상태에 재사용.
        private static void AddTriggerTransition(AnimatorStateMachine root, AnimatorState target, string triggerParam, AnimatorState returnState)
        {
            AnimatorStateTransition anyToTarget = root.AddAnyStateTransition(target);
            anyToTarget.AddCondition(AnimatorConditionMode.If, 0, triggerParam);
            anyToTarget.hasExitTime = false;
            anyToTarget.duration = 0.05f;

            AnimatorStateTransition targetToReturn = target.AddTransition(returnState);
            targetToReturn.hasExitTime = true;
            targetToReturn.exitTime = 1f;
            targetToReturn.duration = 0.1f;
        }

        // ============================================================
        // 테스트 씬 구성
        // 이후 실제 아트/애니메이션이 들어오면:
        // - Player의 MeshRenderer(캡슐)를 SpriteRenderer/스켈레톤 애니메이션으로 교체
        // - Ground도 실제 타일맵/배경으로 교체
        // 컴포넌트 연결 로직(RainGaugeSystem, SkillManager 등)은 그대로 유지하면 됨
        // ============================================================
        // 4개 맵이 공통으로 쓰는 뼈대(카메라/조명/바닥/오디오/플레이어/게임매니저/공용 디버그 UI)를 구성.
        private static void BuildStageCommon(
            AnimatorController controller, int groundLayer, int enemyLayer,
            GameObject projectilePrefab, GameObject hitEffectPrefab, SkillEffectPrefabs skillEffects,
            out GameObject player, out GameManager gameManager, out CameraFollow2D cameraFollow, out GameObject canvasGO)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildCamera(out GameObject cameraGO);
            cameraFollow = cameraGO.AddComponent<CameraFollow2D>();
            BuildLight();
            BuildBackground();
            BuildGround(groundLayer);
            BuildAudioManager();

            player = BuildPlayer(controller, groundLayer, enemyLayer, projectilePrefab, hitEffectPrefab, skillEffects);
            gameManager = BuildGameManager();
            canvasGO = BuildCommonDebugUI(player, gameManager);

            SerializedObject followSO = new SerializedObject(cameraFollow);
            followSO.FindProperty("target").objectReferenceValue = player.transform;
            followSO.ApplyModifiedProperties();
        }

        private static void SaveStageScene(string scenePath)
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);
            Debug.Log("[ProjectSetupTool] 맵 생성 완료: " + scenePath);
        }

        // 몹 count마리를 startX부터 spacing 간격으로 늘어놓고, 킬 트래커가 감시할 수 있도록 리스트로 반환.
        private static List<GameObject> SpawnMobs(GameObject testDummyPrefab, int count, float startX, float spacing)
        {
            var mobs = new List<GameObject>();
            for (int i = 0; i < count; i++)
            {
                GameObject mob = (GameObject)PrefabUtility.InstantiatePrefab(testDummyPrefab);
                mob.transform.position = new Vector3(startX + i * spacing, 0.5f, 0f);
                mobs.Add(mob);
            }
            return mobs;
        }

        // 때려 부수는 문. locked=true면 StageKillTracker가 Unlock()을 호출하기 전까지 데미지가 먹히지 않음.
        private static StageDoor BuildDoor(float x, bool locked, string nextSceneName)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "StageDoor";
            Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            go.transform.position = new Vector3(x, 0.5f, 0f);
            go.transform.localScale = new Vector3(0.6f, 2.5f, 1f);
            go.AddComponent<BoxCollider2D>();

            StageDoor door = go.AddComponent<StageDoor>();
            SerializedObject so = new SerializedObject(door);
            so.FindProperty("startsLocked").boolValue = locked;
            so.FindProperty("nextSceneName").stringValue = nextSceneName;
            so.ApplyModifiedProperties();
            return door;
        }

        // 몹을 전부 잡아야 열리는 포탈. StageKillTracker.Open() 호출 전까지는 콜라이더가 꺼져있음.
        private static StagePortal BuildPortal(float x, string nextSceneName)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "StagePortal";
            Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            go.transform.position = new Vector3(x, 0.5f, 0f);
            go.transform.localScale = new Vector3(1.2f, 2.2f, 0.4f);
            go.AddComponent<BoxCollider2D>();
            Renderer renderer = go.GetComponent<Renderer>();

            StagePortal portal = go.AddComponent<StagePortal>();
            SerializedObject so = new SerializedObject(portal);
            so.FindProperty("nextSceneName").stringValue = nextSceneName;
            so.FindProperty("visualRenderer").objectReferenceValue = renderer;
            so.ApplyModifiedProperties();
            return portal;
        }

        // 수호 골렘 1마리를 지정 위치에 소환 (2맵부터 한 마리씩 추가).
        private static GameObject SpawnGolem(GameObject golemPrefab, float x)
        {
            GameObject golem = (GameObject)PrefabUtility.InstantiatePrefab(golemPrefab);
            golem.transform.position = new Vector3(x, 0.5f, 0f);
            return golem;
        }

        // 지정한 몹 리스트를 감시하다가 전부 죽으면 문을 열거나(doorToUnlock) 포탈을 오픈(portalToOpen)함.
        private static void BuildKillTracker(List<GameObject> mobs, StageDoor doorToUnlock, StagePortal portalToOpen)
        {
            GameObject go = new GameObject("StageKillTracker");
            StageKillTracker tracker = go.AddComponent<StageKillTracker>();

            SerializedObject so = new SerializedObject(tracker);
            SerializedProperty enemiesProp = so.FindProperty("enemies");
            enemiesProp.arraySize = mobs.Count;
            for (int i = 0; i < mobs.Count; i++)
                enemiesProp.GetArrayElementAtIndex(i).objectReferenceValue = mobs[i];

            so.FindProperty("doorToUnlock").objectReferenceValue = doorToUnlock;
            so.FindProperty("portalToOpen").objectReferenceValue = portalToOpen;
            so.ApplyModifiedProperties();
        }

        // ===== 1맵: 몹 3마리를 전부 잡아야 문이 열려서(부술 수 있게 되어) 2맵으로 =====
        private static void BuildStage1(AnimatorController controller, int groundLayer, int enemyLayer,
            GameObject testDummyPrefab, GameObject projectilePrefab, GameObject hitEffectPrefab, SkillEffectPrefabs skillEffects)
        {
            BuildStageCommon(controller, groundLayer, enemyLayer, projectilePrefab, hitEffectPrefab, skillEffects,
                out GameObject player, out _, out _, out _);

            List<GameObject> mobs = SpawnMobs(testDummyPrefab, 3, 4f, 2f);
            StageDoor door = BuildDoor(11f, locked: true, nextSceneName: Stage2SceneName);
            BuildKillTracker(mobs, doorToUnlock: door, portalToOpen: null);

            SaveStageScene(Stage1ScenePath);
            PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
        }

        // ===== 2맵: 몹 4마리 + 수호 골렘 1마리를 전부 잡아야 포탈이 열려서 3맵으로 =====
        private static void BuildStage2(AnimatorController controller, int groundLayer, int enemyLayer,
            GameObject testDummyPrefab, GameObject golemPrefab, GameObject projectilePrefab, GameObject hitEffectPrefab, SkillEffectPrefabs skillEffects)
        {
            BuildStageCommon(controller, groundLayer, enemyLayer, projectilePrefab, hitEffectPrefab, skillEffects,
                out _, out _, out _, out _);

            List<GameObject> mobs = SpawnMobs(testDummyPrefab, 4, 4f, 2f);
            mobs.Add(SpawnGolem(golemPrefab, 9f));
            StagePortal portal = BuildPortal(11f, Stage3SceneName);
            BuildKillTracker(mobs, doorToUnlock: null, portalToOpen: portal);

            SaveStageScene(Stage2ScenePath);
        }

        // ===== 3맵: 몹 4마리 + 수호 골렘 1마리를 전부 잡아야 문이 열려서(부술 수 있게 되어) 4맵으로 =====
        private static void BuildStage3(AnimatorController controller, int groundLayer, int enemyLayer,
            GameObject testDummyPrefab, GameObject golemPrefab, GameObject projectilePrefab, GameObject hitEffectPrefab, SkillEffectPrefabs skillEffects)
        {
            BuildStageCommon(controller, groundLayer, enemyLayer, projectilePrefab, hitEffectPrefab, skillEffects,
                out _, out _, out _, out _);

            List<GameObject> mobs = SpawnMobs(testDummyPrefab, 4, 4f, 2f);
            mobs.Add(SpawnGolem(golemPrefab, 9f));
            StageDoor door = BuildDoor(11f, locked: true, nextSceneName: Stage4SceneName);
            BuildKillTracker(mobs, doorToUnlock: door, portalToOpen: null);

            SaveStageScene(Stage3ScenePath);
        }

        // ===== 4맵(최종): 보스 1 + 몹 2 + 수호 골렘 1. 보스를 처치하면 게임 클리어 =====
        private static void BuildStage4(AnimatorController controller, int groundLayer, int enemyLayer,
            GameObject testDummyPrefab, GameObject golemPrefab, GameObject projectilePrefab, GameObject hitEffectPrefab, GameObject bossPrefab, SkillEffectPrefabs skillEffects)
        {
            BuildStageCommon(controller, groundLayer, enemyLayer, projectilePrefab, hitEffectPrefab, skillEffects,
                out GameObject player, out GameManager gameManager, out CameraFollow2D cameraFollow, out GameObject canvasGO);

            SpawnMobs(testDummyPrefab, 2, 4f, 2f);
            SpawnGolem(golemPrefab, 8f);
            BuildBossFinaleUI(canvasGO, gameManager, player, cameraFollow, bossPrefab);

            SaveStageScene(Stage4ScenePath);
        }

        private static void BuildCamera(out GameObject cameraGO)
        {
            cameraGO = new GameObject("Main Camera");
            cameraGO.tag = "MainCamera";
            Camera cam = cameraGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
            cameraGO.AddComponent<AudioListener>();
            cameraGO.transform.position = new Vector3(0f, 1.5f, -10f);
        }

        private static void BuildLight()
        {
            GameObject lightGO = new GameObject("Directional Light");
            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        // 4개 맵 공통 배경 일러스트. 카메라보다 훨씬 뒤(z=10)에 두고 sortingOrder를 아주 낮게 줘서
        // Ground/Player/몬스터 전부보다 뒤에 깔리게 함. 스테이지 경계(-14~14)를 넉넉히 덮도록 가로 폭 기준으로 스케일.
        private static void BuildBackground()
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundSpritePath);
            if (sprite == null) return;

            GameObject go = new GameObject("Background");
            go.transform.position = new Vector3(0f, 2f, 10f);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -100;

            const float targetWidth = 50f;
            if (sprite.bounds.size.x > 0f)
            {
                float scale = targetWidth / sprite.bounds.size.x;
                go.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        // 흰색 큐브 대신 돌길 텍스처를 Tiled로 반복시켜 바닥처럼 보이게 함.
        // 콜라이더 크기/위치는 기존 큐브(scale 30x1, y=-1)와 동일하게 맞춰서 GroundCheck 등 기존 로직에 영향 없음.
        private static void BuildGround(int groundLayer)
        {
            GameObject ground = new GameObject("Ground_Placeholder");
            ground.layer = groundLayer;
            ground.transform.position = new Vector3(0f, -1f, 0f);

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GroundSpritePath);
            SpriteRenderer renderer = ground.AddComponent<SpriteRenderer>();
            if (sprite != null)
            {
                renderer.sprite = sprite;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.size = new Vector2(30f, 1f);
            }

            BoxCollider2D collider = ground.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(30f, 1f);
        }

        private static GameObject BuildPlayer(AnimatorController controller, int groundLayer, int enemyLayer, GameObject projectilePrefab, GameObject hitEffectPrefab, SkillEffectPrefabs skillEffects)
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player_Placeholder";
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 0.5f, 0f);

            // 실제 캐릭터 스프라이트가 들어왔으므로 3D 캡슐 메쉬 대신 SpriteRenderer로 표시.
            // 물리 판정용 CapsuleCollider2D는 그대로 캡슐 모양을 유지함.
            Object.DestroyImmediate(player.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(player.GetComponent<MeshFilter>());
            SpriteRenderer spriteRenderer = player.AddComponent<SpriteRenderer>();
            Sprite idleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Idle/Idle_0.png");
            if (idleSprite != null)
                spriteRenderer.sprite = idleSprite;

            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.gravityScale = 3f;

            CapsuleCollider2D col = player.AddComponent<CapsuleCollider2D>();
            col.direction = CapsuleDirection2D.Vertical;
            col.size = new Vector2(1f, 2f);

            GameObject groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(player.transform);
            groundCheck.transform.localPosition = new Vector3(0f, -1f, 0f);

            GameObject attackHitboxGO = new GameObject("AttackHitbox");
            attackHitboxGO.transform.SetParent(player.transform);
            attackHitboxGO.transform.localPosition = new Vector3(0.6f, 0f, 0f);
            BoxCollider2D attackHitboxCollider = attackHitboxGO.AddComponent<BoxCollider2D>();
            attackHitboxCollider.size = new Vector2(1f, 1.5f);
            attackHitboxCollider.isTrigger = true;
            AttackHitbox attackHitbox = attackHitboxGO.AddComponent<AttackHitbox>();
            SerializedObject attackHitboxSO = new SerializedObject(attackHitbox);
            attackHitboxSO.FindProperty("hitEffectPrefab").objectReferenceValue = hitEffectPrefab;
            attackHitboxSO.ApplyModifiedProperties();

            Animator animator = player.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            PlayerHealth health = player.AddComponent<PlayerHealth>();
            player.AddComponent<SuperArmorState>();
            RainGaugeSystem gauge = player.AddComponent<RainGaugeSystem>();
            BuffSystem buffSystem = player.AddComponent<BuffSystem>();
            HealSystem healSystem = player.AddComponent<HealSystem>();
            player.AddComponent<AllyGroup>();
            SkillManager skillManager = player.AddComponent<SkillManager>();
            Skill_WaterSplash_Example exampleSkill = player.AddComponent<Skill_WaterSplash_Example>();

            // PlayerController는 GroundCheck/LayerMask 참조가 필요하므로 다른 컴포넌트 다음에 추가
            PlayerController controllerComp = player.AddComponent<PlayerController>();

            SerializedObject controllerSO = new SerializedObject(controllerComp);
            controllerSO.FindProperty("groundCheck").objectReferenceValue = groundCheck.transform;
            controllerSO.FindProperty("groundLayer").intValue = 1 << groundLayer;
            controllerSO.FindProperty("attackHitbox").objectReferenceValue = attackHitbox;
            controllerSO.FindProperty("dropletProjectilePrefab").objectReferenceValue = projectilePrefab;
            controllerSO.ApplyModifiedProperties();

            SerializedObject healthSO = new SerializedObject(health);
            healthSO.FindProperty("targetRenderer").objectReferenceValue = player.GetComponent<Renderer>();
            healthSO.ApplyModifiedProperties();

            // SkillManager의 Active 슬롯(QWER 중 Q)에 예시 스킬을 기본 장착
            // ===== 스킬 15종 추가 시 이 부분처럼 slotBindings[n].equippedSkill 을 채우면 됨 =====
            SerializedObject skillManagerSO = new SerializedObject(skillManager);
            SerializedProperty bindings = skillManagerSO.FindProperty("slotBindings");
            for (int i = 0; i < bindings.arraySize; i++)
            {
                SerializedProperty binding = bindings.GetArrayElementAtIndex(i);
                SerializedProperty slotProp = binding.FindPropertyRelative("slot");
                if ((SkillSlot)slotProp.enumValueIndex == SkillSlot.Active)
                {
                    binding.FindPropertyRelative("equippedSkill").objectReferenceValue = exampleSkill;
                    break;
                }
            }
            skillManagerSO.ApplyModifiedProperties();

            DebugHitTester hitTester = player.AddComponent<DebugHitTester>();
            SerializedObject hitTesterSO = new SerializedObject(hitTester);
            hitTesterSO.FindProperty("targetHealth").objectReferenceValue = health;
            hitTesterSO.ApplyModifiedProperties();

            DebugBuffHealTester buffHealTester = player.AddComponent<DebugBuffHealTester>();
            SerializedObject buffHealTesterSO = new SerializedObject(buffHealTester);
            buffHealTesterSO.FindProperty("buffSystem").objectReferenceValue = buffSystem;
            buffHealTesterSO.FindProperty("healSystem").objectReferenceValue = healSystem;
            buffHealTesterSO.ApplyModifiedProperties();

            // ===== 스파 가디언(1차 전직) 스킬 5종 - Assets/Scripts/Skills/SpaGuardian/ 참고 =====
            Skill_SplishSplash splishSplash = player.AddComponent<Skill_SplishSplash>();
            ConfigureSkillBase(splishSplash, "첨벙첨벙", SkillSlot.Active, JobLine.SpaGuardian, 6f);
            SetSkillEnemyLayer(splishSplash, enemyLayer);
            SetSkillEffectPrefab(splishSplash, "splashEffectPrefab", skillEffects.Splash);

            Skill_EatEgg eatEgg = player.AddComponent<Skill_EatEgg>();
            ConfigureSkillBase(eatEgg, "계란 먹어", SkillSlot.Robust, JobLine.SpaGuardian, 10f);
            SetSkillEnemyLayer(eatEgg, enemyLayer);
            SetSkillEffectPrefab(eatEgg, "eggEffectPrefab", skillEffects.Egg);

            Skill_ThirstyHuh thirstyHuh = player.AddComponent<Skill_ThirstyHuh>();
            ConfigureSkillBase(thirstyHuh, "목 마르지?", SkillSlot.Intense, JobLine.SpaGuardian, 8f);
            SetSkillEnemyLayer(thirstyHuh, enemyLayer);
            SetSkillEffectPrefab(thirstyHuh, "waveEffectPrefab", skillEffects.Wave);

            Skill_HotSpringWater hotSpringWater = player.AddComponent<Skill_HotSpringWater>();
            ConfigureSkillBase(hotSpringWater, "온천수", SkillSlot.Transcend, JobLine.SpaGuardian, 20f);
            SetSkillEnemyLayer(hotSpringWater, enemyLayer);
            SetSkillEffectPrefab(hotSpringWater, "hotSpringEffectPrefab", skillEffects.HotSpring);

            Skill_HotSpringPrincess hotSpringPrincess = player.AddComponent<Skill_HotSpringPrincess>();
            ConfigureSkillBase(hotSpringPrincess, "온천의 공주", SkillSlot.Passive, JobLine.SpaGuardian, 0f);
            SetSkillEffectPrefab(hotSpringPrincess, "healTickEffectPrefab", skillEffects.HealTick);

            // 전직 라인 "스파 가디언" 스킬셋을 SkillManager에 등록하고, 기본 전직 라인을 스파 가디언으로 설정
            // (본캐 스킬 5종이 아직 첨벙첨벙 1개뿐이라, 테스트 씬은 바로 스파 가디언 스킬을 확인할 수 있게 기본값을 잡아둠.
            //  J키(DebugJobLineTester)로 본캐 ↔ 스파 가디언을 토글하면 QWER 슬롯이 실제로 바뀌는 것도 확인 가능)
            SerializedObject jobLineSO = new SerializedObject(skillManager);
            SerializedProperty jobLineSets = jobLineSO.FindProperty("jobLineSkillSets");
            jobLineSets.InsertArrayElementAtIndex(0);
            SerializedProperty spaGuardianSet = jobLineSets.GetArrayElementAtIndex(0);
            spaGuardianSet.FindPropertyRelative("jobLine").enumValueIndex = (int)JobLine.SpaGuardian;

            SerializedProperty skillsProp = spaGuardianSet.FindPropertyRelative("skills");
            skillsProp.arraySize = 5;
            skillsProp.GetArrayElementAtIndex(0).objectReferenceValue = splishSplash;
            skillsProp.GetArrayElementAtIndex(1).objectReferenceValue = eatEgg;
            skillsProp.GetArrayElementAtIndex(2).objectReferenceValue = thirstyHuh;
            skillsProp.GetArrayElementAtIndex(3).objectReferenceValue = hotSpringWater;
            skillsProp.GetArrayElementAtIndex(4).objectReferenceValue = hotSpringPrincess;

            jobLineSO.FindProperty("currentJobLine").enumValueIndex = (int)JobLine.SpaGuardian;
            jobLineSO.ApplyModifiedProperties();

            DebugJobLineTester jobLineTester = player.AddComponent<DebugJobLineTester>();
            SerializedObject jobLineTesterSO = new SerializedObject(jobLineTester);
            jobLineTesterSO.FindProperty("skillManager").objectReferenceValue = skillManager;
            jobLineTesterSO.ApplyModifiedProperties();

            return player;
        }

        // SkillBase의 private 필드(skillName/slot/jobLine/cooldown)는 하위 클래스에서 직접 설정할 수 없어
        // SerializedObject로 값을 채우는 공용 헬퍼. 새 스킬을 자동 배치에 추가할 때 재사용하면 됨.
        private static void ConfigureSkillBase(SkillBase skill, string name, SkillSlot slot, JobLine jobLine, float cooldown)
        {
            SerializedObject so = new SerializedObject(skill);
            so.FindProperty("skillName").stringValue = name;
            so.FindProperty("slot").enumValueIndex = (int)slot;
            so.FindProperty("jobLine").enumValueIndex = (int)jobLine;
            so.FindProperty("cooldown").floatValue = cooldown;
            so.ApplyModifiedProperties();
        }

        // 스킬에 enemyLayer 필드가 있으면(적 판정을 하는 스킬) Enemy 레이어로 채워주는 헬퍼.
        private static void SetSkillEnemyLayer(SkillBase skill, int enemyLayer)
        {
            SerializedObject so = new SerializedObject(skill);
            SerializedProperty prop = so.FindProperty("enemyLayer");
            if (prop == null) return;

            prop.intValue = 1 << enemyLayer;
            so.ApplyModifiedProperties();
        }

        // ===== 6주차 스프린트: GameManager(점수/게임오버) + EnemySpawner(무한 스폰) =====
        private static GameManager BuildGameManager()
        {
            GameObject gameManagerGO = new GameObject("GameManager");
            return gameManagerGO.AddComponent<GameManager>();
        }

        private static Sprite LoadMonsterSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // 스프라이트를 붙이고, 원하는 월드 높이(worldHeight)에 맞춰 localScale을 역산해서 적용.
        // 같은 GameObject의 BoxCollider2D가 있으면 스프라이트 실제 크기에 맞게 size도 같이 맞춰줌
        // (CreatePrimitive 큐브와 달리 SpriteRenderer는 원본 픽셀 크기가 제각각이라 그대로 두면 크기가 안 맞음).
        private static void ApplySpriteWithWorldHeight(GameObject go, Sprite sprite, float worldHeight)
        {
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;

            if (sprite == null || sprite.bounds.size.y <= 0f) return;

            float scale = worldHeight / sprite.bounds.size.y;
            go.transform.localScale = new Vector3(scale, scale, 1f);

            BoxCollider2D collider = go.GetComponent<BoxCollider2D>();
            if (collider != null)
                collider.size = sprite.bounds.size;
        }

        // TestDummy(슬라임)를 프리팹으로 한 번 저장해두고, 각 맵이 필요한 수만큼 SpawnMobs()로 Instantiate해서 씁니다.
        // (맵 4개 구조 이전에는 EnemySpawner가 무한 스폰했지만, 이제는 맵마다 정해진 수의 몹만 고정 배치함)
        private static GameObject BuildTestDummyPrefab(int enemyLayer)
        {
            GameObject dummyTemplate = new GameObject("TestDummy");
            dummyTemplate.layer = enemyLayer;

            dummyTemplate.AddComponent<SpriteRenderer>();
            dummyTemplate.AddComponent<BoxCollider2D>();
            ApplySpriteWithWorldHeight(dummyTemplate, LoadMonsterSprite(SlimeSpritePath), 0.8f);

            dummyTemplate.AddComponent<TestDummy>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(dummyTemplate, TestDummyPrefabPath);
            Object.DestroyImmediate(dummyTemplate);
            return prefab;
        }

        // ===== 수호 골렘: 2맵부터 한 마리씩 추가되는 중간 보스급 몬스터 (TestDummy 재사용, 능력치만 강화) =====
        private static GameObject BuildGuardianGolemPrefab(int enemyLayer)
        {
            GameObject go = new GameObject("GuardianGolem");
            go.layer = enemyLayer;

            go.AddComponent<SpriteRenderer>();
            go.AddComponent<BoxCollider2D>();
            ApplySpriteWithWorldHeight(go, LoadMonsterSprite(GuardianGolemSpritePath), 2.2f);

            TestDummy dummy = go.AddComponent<TestDummy>();
            SerializedObject so = new SerializedObject(dummy);
            so.FindProperty("maxHp").floatValue = 500f;
            so.FindProperty("scoreValue").intValue = 50;
            so.FindProperty("contactDamage").intValue = 20;
            so.FindProperty("contactKnockback").floatValue = 10f;
            so.FindProperty("moveSpeed").floatValue = 1f;
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, GuardianGolemPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ===== 9주차 스프린트: 오디오 매니저 (BGM/SFX 풀링) =====
        // Assets/Audio에 플레이스홀더 톤 클립(gen_audio.py로 생성)이 있으면 자동으로 연결함.
        // 실제 음원으로 교체하려면 같은 파일명으로 덮어쓰거나, 인스펙터에서 클립 슬롯만 바꿔 끼우면 됨.
        private static void BuildAudioManager()
        {
            GameObject go = new GameObject("AudioManager");
            AudioSource bgmSource = go.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            AudioManager manager = go.AddComponent<AudioManager>();

            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("bgmSource").objectReferenceValue = bgmSource;
            so.FindProperty("defaultBgm").objectReferenceValue = LoadAudioClip("bgm_default.wav");
            so.FindProperty("bossBgm").objectReferenceValue = LoadAudioClip("bgm_boss.wav");
            so.FindProperty("fireSfx").objectReferenceValue = LoadAudioClip("fire_sfx.wav");
            so.FindProperty("hitSfx").objectReferenceValue = LoadAudioClip("hit_sfx.wav");
            so.FindProperty("playerHitSfx").objectReferenceValue = LoadAudioClip("player_hit_sfx.wav");
            so.FindProperty("gameOverSfx").objectReferenceValue = LoadAudioClip("game_over_sfx.wav");
            so.FindProperty("bossWarningSfx").objectReferenceValue = LoadAudioClip("boss_warning_sfx.wav");
            so.FindProperty("victorySfx").objectReferenceValue = LoadAudioClip("victory_sfx.wav");
            so.ApplyModifiedProperties();
        }

        private static AudioClip LoadAudioClip(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + fileName);
        }

        // ===== 9주차 스프린트: 피격 이펙트 플레이스홀더 (파티클 1종, 공격 히트박스/투사체 공용) =====
        // ParticleSystem을 AddComponent로 만들면 렌더러에 머티리얼이 제대로 안 잡혀서(배치모드 환경 특성)
        // 보라색/깨진 셰이더 네모로 보이는 경우가 있음. Sprites/Default 셰이더 + 지정한 텍스처를
        // 직접 할당해서 항상 제대로 된 모양으로 나오게 고정함.
        private static void ApplyParticleMaterial(ParticleSystem ps, string spritePath)
        {
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer == null) return;

            renderer.sharedMaterial = GetOrCreateSpriteMaterial(spritePath);
        }

        // 순수 런타임 Material(new Material(shader))은 AssetDatabase에 등록된 "진짜 에셋"이 아니라서
        // SaveAsPrefabAsset을 해도 참조가 fileID 0(null)으로 저장되어버림. .mat 파일로 실제 저장해야
        // 프리팹이 그 머티리얼을 제대로 참조/보존할 수 있음.
        private static Material GetOrCreateSpriteMaterial(string spritePath)
        {
            string fileName = spritePath.Substring(spritePath.LastIndexOf('/') + 1);
            fileName = fileName.Substring(0, fileName.LastIndexOf('.'));
            string materialPath = $"Assets/Materials/VFX/{fileName}_Mat.mat";

            Material existing = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (existing != null) return existing;

            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder("Assets/Materials/VFX"))
                AssetDatabase.CreateFolder("Assets/Materials", "VFX");

            Shader shader = Shader.Find("Sprites/Default");
            Material material = new Material(shader);

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite != null)
                material.mainTexture = sprite.texture;

            AssetDatabase.CreateAsset(material, materialPath);
            return material;
        }

        private static GameObject BuildHitEffectPrefab()
        {
            GameObject go = new GameObject("HitEffect_Placeholder");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.3f;
            main.startLifetime = 0.3f;
            main.startSpeed = 2f;
            main.startSize = 0.15f;
            main.startColor = new Color(0.6f, 0.85f, 1f, 1f);
            main.loop = false;
            main.playOnAwake = true;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            ApplyParticleMaterial(ps, SoftDotSpritePath);
            go.AddComponent<HitEffect>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, HitEffectPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ===== 스파 가디언 스킬 5종 VFX: Perchance로 받은 일러스트 한 장을 그 자리에 "펑" 띄웠다 사라지게 함 =====
        // 작은 파티클을 여러 개 뿌리는 대신, burst 1개로 일러스트 전체가 한 번에 보이게 하는 방식.
        // color는 항상 흰색으로 둬서 일러스트 원본 색이 그대로 나오게 함(틴트 곱하면 색이 탁해짐).
        private static GameObject BuildSkillFxPrefab(string name, string assetPath, string artPath,
            float startSize, float lifetime)
        {
            GameObject go = new GameObject(name);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.duration = lifetime;
            main.startLifetime = lifetime;
            main.startSpeed = 0f;
            main.startSize = startSize;
            main.startColor = Color.white;
            main.loop = false;
            main.playOnAwake = true;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            ApplyParticleMaterial(ps, artPath);

            HitEffect hitEffect = go.AddComponent<HitEffect>();
            SerializedObject hitEffectSO = new SerializedObject(hitEffect);
            hitEffectSO.FindProperty("lifetime").floatValue = lifetime;
            hitEffectSO.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, assetPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static SkillEffectPrefabs BuildSkillEffectPrefabs()
        {
            return new SkillEffectPrefabs
            {
                // 첨벙첨벙 - splashRadius(2.5)에 맞춘 크기
                Splash = BuildSkillFxPrefab("FX_Splash", SplashEffectPrefabPath, SplashArtPath, 3f, 0.5f),
                // 계란 먹어 - 머리 위에 작게
                Egg = BuildSkillFxPrefab("FX_EggThrow", EggEffectPrefabPath, EggArtPath, 1.4f, 0.5f),
                // 목 마르지? - waveRange(3.5)에 맞춘 전방 파도
                Wave = BuildSkillFxPrefab("FX_SikhyeWave", WaveEffectPrefabPath, WaveArtPath, 3f, 0.5f),
                // 온천수 - radius(3)에 맞춘 큰 소환 플래시
                HotSpring = BuildSkillFxPrefab("FX_HotSpring", HotSpringEffectPrefabPath, HotSpringArtPath, 4.5f, 0.6f),
                // 온천의 공주 - 패시브 틱마다 뜨는 작은 반짝임
                HealTick = BuildSkillFxPrefab("FX_HealTick", HealTickEffectPrefabPath, HealTickArtPath, 1.2f, 0.4f),
            };
        }

        // 스킬의 VFX 프리팹 필드(이름이 제각각이라 fieldName으로 찾음)에 값을 채워주는 헬퍼.
        private static void SetSkillEffectPrefab(SkillBase skill, string fieldName, GameObject prefab)
        {
            SerializedObject so = new SerializedObject(skill);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) return;

            prop.objectReferenceValue = prefab;
            so.ApplyModifiedProperties();
        }

        // ===== 9주차 스프린트: 물방울 투사체 프리팹 (플레이어 원거리 공격 / 보스 탄막 공용) =====
        private static GameObject BuildProjectilePrefab(GameObject hitEffectPrefab)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Projectile_Droplet";
            Object.DestroyImmediate(go.GetComponent<SphereCollider>());
            go.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.3f, 0.6f, 1f, 1f);

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;

            // 발사체/타겟 양쪽 모두 Rigidbody2D가 없으면 2D 트리거 이벤트 자체가 발생하지 않으므로
            // 발사체 쪽에 Kinematic Rigidbody2D를 달아 TestDummy/StageDoor처럼 RB 없는 대상도 확실히 맞도록 보장.
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            Projectile projectile = go.AddComponent<Projectile>();
            SerializedObject so = new SerializedObject(projectile);
            so.FindProperty("hitEffectPrefab").objectReferenceValue = hitEffectPrefab;
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, ProjectilePrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ===== 10주차 스프린트: 보스 몬스터 프리팹 =====
        private static GameObject BuildBossPrefab(int enemyLayer, GameObject projectilePrefab)
        {
            GameObject go = new GameObject("BossMonster");
            go.layer = enemyLayer;

            go.AddComponent<SpriteRenderer>();
            go.AddComponent<BoxCollider2D>();
            ApplySpriteWithWorldHeight(go, LoadMonsterSprite(BossGolemSpritePath), 4.5f);

            BossMonster boss = go.AddComponent<BossMonster>();

            SerializedObject so = new SerializedObject(boss);
            so.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, BossPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static Font ResolveFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }

        // 4개 맵이 공통으로 쓰는 디버그 UI: 강수량 게이지, HP/마나, 점수, 게임오버 패널, 일시정지 패널.
        // 보스 전용 UI(WARNING/보스 체력바/게임 클리어 패널)는 BuildBossFinaleUI에서 Stage4에만 따로 추가함.
        private static GameObject BuildCommonDebugUI(GameObject player, GameManager gameManager)
        {
            GameObject canvasGO = new GameObject("DebugCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();

            Font font = ResolveFont();

            // 게이지 슬라이더 (배경 + 채움만 있는 표시 전용 슬라이더)
            GameObject sliderGO = new GameObject("RainGaugeSlider", typeof(RectTransform));
            sliderGO.transform.SetParent(canvasGO.transform, false);
            RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0f, 1f);
            sliderRect.anchorMax = new Vector2(0f, 1f);
            sliderRect.pivot = new Vector2(0f, 1f);
            sliderRect.anchoredPosition = new Vector2(20f, -20f);
            sliderRect.sizeDelta = new Vector2(300f, 24f);

            GameObject bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(sliderGO.transform, false);
            RectTransform bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bgGO.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.8f);

            GameObject fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGO.transform.SetParent(sliderGO.transform, false);
            RectTransform fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(4f, 4f);
            fillAreaRect.offsetMax = new Vector2(-4f, -4f);

            GameObject fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            RectTransform fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillGO.GetComponent<Image>().color = new Color(0.3f, 0.6f, 1f, 1f);

            Slider slider = sliderGO.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.targetGraphic = null;
            slider.handleRect = null;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = 0f;

            // 게이지 텍스트
            GameObject textGO = new GameObject("RainGaugeText", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(canvasGO.transform, false);
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(0f, 1f);
            textRect.pivot = new Vector2(0f, 1f);
            textRect.anchoredPosition = new Vector2(20f, -52f);
            textRect.sizeDelta = new Vector2(400f, 24f);

            Text text = textGO.GetComponent<Text>();
            text.font = font;
            text.fontSize = 16;
            text.color = Color.white;
            text.text = "강수량 게이지 0 / 100 (x1.00)";

            RainGaugeSystem gaugeSystem = player.GetComponent<RainGaugeSystem>();
            RainGaugeUI gaugeUI = canvasGO.AddComponent<RainGaugeUI>();
            SerializedObject gaugeUISO = new SerializedObject(gaugeUI);
            gaugeUISO.FindProperty("gaugeSystem").objectReferenceValue = gaugeSystem;
            gaugeUISO.FindProperty("gaugeSlider").objectReferenceValue = slider;
            gaugeUISO.FindProperty("gaugeText").objectReferenceValue = text;
            gaugeUISO.ApplyModifiedProperties();

            // HP / 마나 슬라이더 (강수량 게이지 UI 바로 아래에 배치)
            Slider hpSlider = CreateDebugSlider(canvasGO.transform, "HpSlider", new Vector2(20f, -84f), new Color(0.9f, 0.3f, 0.3f, 1f));
            Text hpText = CreateDebugText(canvasGO.transform, "HpText", new Vector2(20f, -116f), font, "HP 100 / 100");

            Slider manaSlider = CreateDebugSlider(canvasGO.transform, "ManaSlider", new Vector2(20f, -148f), new Color(0.4f, 0.5f, 0.9f, 1f));
            Text manaText = CreateDebugText(canvasGO.transform, "ManaText", new Vector2(20f, -180f), font, "Mana 100 / 100");

            Text buffListText = CreateDebugText(canvasGO.transform, "BuffListText", new Vector2(20f, -212f), font, "활성 버프 없음");
            buffListText.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 140f);

            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            BuffSystem buffSystem = player.GetComponent<BuffSystem>();
            BuffHealDebugUI buffHealUI = canvasGO.AddComponent<BuffHealDebugUI>();
            SerializedObject buffHealUISO = new SerializedObject(buffHealUI);
            buffHealUISO.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            buffHealUISO.FindProperty("buffSystem").objectReferenceValue = buffSystem;
            buffHealUISO.FindProperty("hpSlider").objectReferenceValue = hpSlider;
            buffHealUISO.FindProperty("hpText").objectReferenceValue = hpText;
            buffHealUISO.FindProperty("manaSlider").objectReferenceValue = manaSlider;
            buffHealUISO.FindProperty("manaText").objectReferenceValue = manaText;
            buffHealUISO.FindProperty("buffListText").objectReferenceValue = buffListText;
            buffHealUISO.ApplyModifiedProperties();

            // ===== 6주차 스프린트: 점수 UI (화면 오른쪽 위) =====
            GameObject scoreTextGO = new GameObject("ScoreText", typeof(RectTransform), typeof(Text));
            scoreTextGO.transform.SetParent(canvasGO.transform, false);
            RectTransform scoreRect = scoreTextGO.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(1f, 1f);
            scoreRect.anchorMax = new Vector2(1f, 1f);
            scoreRect.pivot = new Vector2(1f, 1f);
            scoreRect.anchoredPosition = new Vector2(-20f, -20f);
            scoreRect.sizeDelta = new Vector2(240f, 32f);

            Text scoreText = scoreTextGO.GetComponent<Text>();
            scoreText.font = font;
            scoreText.fontSize = 22;
            scoreText.color = Color.white;
            scoreText.alignment = TextAnchor.UpperRight;
            scoreText.text = "Score: 0";

            ScoreUI scoreUI = canvasGO.AddComponent<ScoreUI>();
            SerializedObject scoreUISO = new SerializedObject(scoreUI);
            scoreUISO.FindProperty("gameManager").objectReferenceValue = gameManager;
            scoreUISO.FindProperty("scoreText").objectReferenceValue = scoreText;
            scoreUISO.ApplyModifiedProperties();

            // ===== 7주차 스프린트: 게임오버 패널 (평소엔 꺼져있음) =====
            GameObject gameOverPanel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
            gameOverPanel.transform.SetParent(canvasGO.transform, false);
            RectTransform panelRect = gameOverPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            gameOverPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            Text gameOverTitle = CreateDebugText(gameOverPanel.transform, "GameOverTitle", new Vector2(0f, 0f), font, "GAME OVER");
            RectTransform titleRect = gameOverTitle.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.6f);
            titleRect.anchorMax = new Vector2(0.5f, 0.6f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(400f, 60f);
            gameOverTitle.fontSize = 36;
            gameOverTitle.alignment = TextAnchor.MiddleCenter;

            Text finalScoreText = CreateDebugText(gameOverPanel.transform, "FinalScoreText", new Vector2(0f, 0f), font, "최종 점수: 0");
            RectTransform finalScoreRect = finalScoreText.GetComponent<RectTransform>();
            finalScoreRect.anchorMin = new Vector2(0.5f, 0.48f);
            finalScoreRect.anchorMax = new Vector2(0.5f, 0.48f);
            finalScoreRect.pivot = new Vector2(0.5f, 0.5f);
            finalScoreRect.sizeDelta = new Vector2(400f, 40f);
            finalScoreText.alignment = TextAnchor.MiddleCenter;

            GameObject retryButtonGO = new GameObject("RetryButton", typeof(RectTransform), typeof(Image), typeof(Button));
            retryButtonGO.transform.SetParent(gameOverPanel.transform, false);
            RectTransform retryRect = retryButtonGO.GetComponent<RectTransform>();
            retryRect.anchorMin = new Vector2(0.5f, 0.35f);
            retryRect.anchorMax = new Vector2(0.5f, 0.35f);
            retryRect.pivot = new Vector2(0.5f, 0.5f);
            retryRect.sizeDelta = new Vector2(160f, 48f);
            retryButtonGO.GetComponent<Image>().color = new Color(0.3f, 0.6f, 1f, 1f);
            Button retryButton = retryButtonGO.GetComponent<Button>();

            Text retryLabel = CreateDebugText(retryButtonGO.transform, "Label", Vector2.zero, font, "다시 시작");
            RectTransform retryLabelRect = retryLabel.GetComponent<RectTransform>();
            retryLabelRect.anchorMin = Vector2.zero;
            retryLabelRect.anchorMax = Vector2.one;
            retryLabelRect.offsetMin = Vector2.zero;
            retryLabelRect.offsetMax = Vector2.zero;
            retryLabel.alignment = TextAnchor.MiddleCenter;
            retryLabel.color = Color.black;

            // 평소엔 꺼둠 (GameOverUI.Awake()도 동일하게 꺼주지만, 씬 저장 시점에도 미리 꺼서 Hierarchy를 깔끔하게 유지)
            gameOverPanel.SetActive(false);

            GameOverUI gameOverUI = canvasGO.AddComponent<GameOverUI>();
            SerializedObject gameOverUISO = new SerializedObject(gameOverUI);
            gameOverUISO.FindProperty("gameManager").objectReferenceValue = gameManager;
            gameOverUISO.FindProperty("panel").objectReferenceValue = gameOverPanel;
            gameOverUISO.FindProperty("finalScoreText").objectReferenceValue = finalScoreText;
            gameOverUISO.FindProperty("retryButton").objectReferenceValue = retryButton;
            gameOverUISO.ApplyModifiedProperties();

            BuildPausePanel(canvasGO, font);

            return canvasGO;
        }

        // ===== 11주차 스프린트: ESC 일시정지 패널 (평소엔 꺼져있음, 4개 맵 공통) =====
        private static void BuildPausePanel(GameObject canvasGO, Font font)
        {
            GameObject pausePanel = new GameObject("PausePanel", typeof(RectTransform), typeof(Image));
            pausePanel.transform.SetParent(canvasGO.transform, false);
            RectTransform pausePanelRect = pausePanel.GetComponent<RectTransform>();
            pausePanelRect.anchorMin = Vector2.zero;
            pausePanelRect.anchorMax = Vector2.one;
            pausePanelRect.offsetMin = Vector2.zero;
            pausePanelRect.offsetMax = Vector2.zero;
            pausePanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            Text pauseTitle = CreateDebugText(pausePanel.transform, "PauseTitle", Vector2.zero, font, "일시정지");
            RectTransform pauseTitleRect = pauseTitle.GetComponent<RectTransform>();
            pauseTitleRect.anchorMin = new Vector2(0.5f, 0.6f);
            pauseTitleRect.anchorMax = new Vector2(0.5f, 0.6f);
            pauseTitleRect.pivot = new Vector2(0.5f, 0.5f);
            pauseTitleRect.sizeDelta = new Vector2(400f, 60f);
            pauseTitle.fontSize = 32;
            pauseTitle.alignment = TextAnchor.MiddleCenter;

            GameObject resumeButtonGO = new GameObject("ResumeButton", typeof(RectTransform), typeof(Image), typeof(Button));
            resumeButtonGO.transform.SetParent(pausePanel.transform, false);
            RectTransform resumeRect = resumeButtonGO.GetComponent<RectTransform>();
            resumeRect.anchorMin = new Vector2(0.5f, 0.45f);
            resumeRect.anchorMax = new Vector2(0.5f, 0.45f);
            resumeRect.pivot = new Vector2(0.5f, 0.5f);
            resumeRect.sizeDelta = new Vector2(160f, 48f);
            resumeButtonGO.GetComponent<Image>().color = new Color(0.3f, 0.6f, 1f, 1f);
            Button resumeButton = resumeButtonGO.GetComponent<Button>();

            Text resumeLabel = CreateDebugText(resumeButtonGO.transform, "Label", Vector2.zero, font, "계속하기 (ESC)");
            RectTransform resumeLabelRect = resumeLabel.GetComponent<RectTransform>();
            resumeLabelRect.anchorMin = Vector2.zero;
            resumeLabelRect.anchorMax = Vector2.one;
            resumeLabelRect.offsetMin = Vector2.zero;
            resumeLabelRect.offsetMax = Vector2.zero;
            resumeLabel.alignment = TextAnchor.MiddleCenter;
            resumeLabel.color = Color.black;

            pausePanel.SetActive(false);

            GameObject pauseManagerGO = new GameObject("PauseManager");
            PauseManager pauseManager = pauseManagerGO.AddComponent<PauseManager>();
            SerializedObject pauseManagerSO = new SerializedObject(pauseManager);
            pauseManagerSO.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            pauseManagerSO.FindProperty("resumeButton").objectReferenceValue = resumeButton;
            pauseManagerSO.ApplyModifiedProperties();
        }

        // ===== 10주차 스프린트: 보스 전용 UI + 보스 스포너 (4맵 중 마지막 맵에만 추가) =====
        private static void BuildBossFinaleUI(GameObject canvasGO, GameManager gameManager, GameObject player, CameraFollow2D cameraFollow, GameObject bossPrefab)
        {
            Font font = ResolveFont();

            // ===== 10주차 스프린트: 보스 WARNING 깜빡임 텍스트 (화면 중앙, 평소엔 꺼짐) =====
            GameObject warningTextGO = new GameObject("BossWarningText", typeof(RectTransform), typeof(Text));
            warningTextGO.transform.SetParent(canvasGO.transform, false);
            RectTransform warningRect = warningTextGO.GetComponent<RectTransform>();
            warningRect.anchorMin = new Vector2(0.5f, 0.8f);
            warningRect.anchorMax = new Vector2(0.5f, 0.8f);
            warningRect.pivot = new Vector2(0.5f, 0.5f);
            warningRect.sizeDelta = new Vector2(500f, 60f);
            Text warningText = warningTextGO.GetComponent<Text>();
            warningText.font = font;
            warningText.fontSize = 40;
            warningText.color = Color.red;
            warningText.alignment = TextAnchor.MiddleCenter;
            warningText.text = "WARNING!";
            warningTextGO.SetActive(false);

            BossWarningUI warningUI = canvasGO.AddComponent<BossWarningUI>();
            SerializedObject warningUISO = new SerializedObject(warningUI);
            warningUISO.FindProperty("warningText").objectReferenceValue = warningTextGO;
            warningUISO.ApplyModifiedProperties();

            // ===== 10주차 스프린트: 보스 전용 체력바 (화면 상단 중앙, 평소엔 꺼짐) =====
            GameObject bossHealthRoot = new GameObject("BossHealthRoot", typeof(RectTransform));
            bossHealthRoot.transform.SetParent(canvasGO.transform, false);
            RectTransform bossHealthRootRect = bossHealthRoot.GetComponent<RectTransform>();
            bossHealthRootRect.anchorMin = new Vector2(0.5f, 1f);
            bossHealthRootRect.anchorMax = new Vector2(0.5f, 1f);
            bossHealthRootRect.pivot = new Vector2(0.5f, 1f);
            bossHealthRootRect.anchoredPosition = new Vector2(0f, -20f);
            bossHealthRootRect.sizeDelta = new Vector2(600f, 36f);

            Slider bossHpSlider = CreateDebugSlider(bossHealthRoot.transform, "BossHpSlider", Vector2.zero, new Color(0.7f, 0.1f, 0.1f, 1f));
            RectTransform bossHpSliderRect = bossHpSlider.GetComponent<RectTransform>();
            bossHpSliderRect.anchorMin = Vector2.zero;
            bossHpSliderRect.anchorMax = Vector2.one;
            bossHpSliderRect.offsetMin = Vector2.zero;
            bossHpSliderRect.offsetMax = Vector2.zero;

            Text bossHpText = CreateDebugText(bossHealthRoot.transform, "BossHpText", Vector2.zero, font, "BOSS 1000 / 1000");
            RectTransform bossHpTextRect = bossHpText.GetComponent<RectTransform>();
            bossHpTextRect.anchorMin = Vector2.zero;
            bossHpTextRect.anchorMax = Vector2.one;
            bossHpTextRect.offsetMin = Vector2.zero;
            bossHpTextRect.offsetMax = Vector2.zero;
            bossHpText.alignment = TextAnchor.MiddleCenter;
            bossHpText.color = Color.white;

            bossHealthRoot.SetActive(false);

            BossHealthUI bossHealthUI = canvasGO.AddComponent<BossHealthUI>();
            SerializedObject bossHealthUISO = new SerializedObject(bossHealthUI);
            bossHealthUISO.FindProperty("root").objectReferenceValue = bossHealthRoot;
            bossHealthUISO.FindProperty("hpSlider").objectReferenceValue = bossHpSlider;
            bossHealthUISO.FindProperty("hpText").objectReferenceValue = bossHpText;
            bossHealthUISO.ApplyModifiedProperties();

            // ===== 10주차 스프린트: 게임 클리어 패널 (평소엔 꺼져있음, GameOverPanel과 동일 구조 + 버튼 2개) =====
            GameObject gameClearPanel = new GameObject("GameClearPanel", typeof(RectTransform), typeof(Image));
            gameClearPanel.transform.SetParent(canvasGO.transform, false);
            RectTransform clearPanelRect = gameClearPanel.GetComponent<RectTransform>();
            clearPanelRect.anchorMin = Vector2.zero;
            clearPanelRect.anchorMax = Vector2.one;
            clearPanelRect.offsetMin = Vector2.zero;
            clearPanelRect.offsetMax = Vector2.zero;
            gameClearPanel.GetComponent<Image>().color = new Color(0f, 0.05f, 0.1f, 0.85f);

            Text clearResultText = CreateDebugText(gameClearPanel.transform, "ClearResultText", Vector2.zero, font, "GAME CLEAR!");
            RectTransform clearResultRect = clearResultText.GetComponent<RectTransform>();
            clearResultRect.anchorMin = new Vector2(0.5f, 0.55f);
            clearResultRect.anchorMax = new Vector2(0.5f, 0.55f);
            clearResultRect.pivot = new Vector2(0.5f, 0.5f);
            clearResultRect.sizeDelta = new Vector2(500f, 160f);
            clearResultText.alignment = TextAnchor.MiddleCenter;
            clearResultText.fontSize = 24;

            GameObject clearRetryButtonGO = new GameObject("ClearRetryButton", typeof(RectTransform), typeof(Image), typeof(Button));
            clearRetryButtonGO.transform.SetParent(gameClearPanel.transform, false);
            RectTransform clearRetryRect = clearRetryButtonGO.GetComponent<RectTransform>();
            clearRetryRect.anchorMin = new Vector2(0.5f, 0.3f);
            clearRetryRect.anchorMax = new Vector2(0.5f, 0.3f);
            clearRetryRect.pivot = new Vector2(0.5f, 0.5f);
            clearRetryRect.anchoredPosition = new Vector2(-90f, 0f);
            clearRetryRect.sizeDelta = new Vector2(150f, 48f);
            clearRetryButtonGO.GetComponent<Image>().color = new Color(0.3f, 0.6f, 1f, 1f);
            Button clearRetryButton = clearRetryButtonGO.GetComponent<Button>();

            Text clearRetryLabel = CreateDebugText(clearRetryButtonGO.transform, "Label", Vector2.zero, font, "다시 시작");
            RectTransform clearRetryLabelRect = clearRetryLabel.GetComponent<RectTransform>();
            clearRetryLabelRect.anchorMin = Vector2.zero;
            clearRetryLabelRect.anchorMax = Vector2.one;
            clearRetryLabelRect.offsetMin = Vector2.zero;
            clearRetryLabelRect.offsetMax = Vector2.zero;
            clearRetryLabel.alignment = TextAnchor.MiddleCenter;
            clearRetryLabel.color = Color.black;

            GameObject clearMenuButtonGO = new GameObject("ClearMainMenuButton", typeof(RectTransform), typeof(Image), typeof(Button));
            clearMenuButtonGO.transform.SetParent(gameClearPanel.transform, false);
            RectTransform clearMenuRect = clearMenuButtonGO.GetComponent<RectTransform>();
            clearMenuRect.anchorMin = new Vector2(0.5f, 0.3f);
            clearMenuRect.anchorMax = new Vector2(0.5f, 0.3f);
            clearMenuRect.pivot = new Vector2(0.5f, 0.5f);
            clearMenuRect.anchoredPosition = new Vector2(90f, 0f);
            clearMenuRect.sizeDelta = new Vector2(150f, 48f);
            clearMenuButtonGO.GetComponent<Image>().color = new Color(0.6f, 0.6f, 0.6f, 1f);
            Button clearMenuButton = clearMenuButtonGO.GetComponent<Button>();

            Text clearMenuLabel = CreateDebugText(clearMenuButtonGO.transform, "Label", Vector2.zero, font, "메인 메뉴로");
            RectTransform clearMenuLabelRect = clearMenuLabel.GetComponent<RectTransform>();
            clearMenuLabelRect.anchorMin = Vector2.zero;
            clearMenuLabelRect.anchorMax = Vector2.one;
            clearMenuLabelRect.offsetMin = Vector2.zero;
            clearMenuLabelRect.offsetMax = Vector2.zero;
            clearMenuLabel.alignment = TextAnchor.MiddleCenter;
            clearMenuLabel.color = Color.black;

            gameClearPanel.SetActive(false);

            GameClearUI gameClearUI = canvasGO.AddComponent<GameClearUI>();
            SerializedObject gameClearUISO = new SerializedObject(gameClearUI);
            gameClearUISO.FindProperty("gameManager").objectReferenceValue = gameManager;
            gameClearUISO.FindProperty("panel").objectReferenceValue = gameClearPanel;
            gameClearUISO.FindProperty("resultText").objectReferenceValue = clearResultText;
            gameClearUISO.FindProperty("retryButton").objectReferenceValue = clearRetryButton;
            gameClearUISO.FindProperty("mainMenuButton").objectReferenceValue = clearMenuButton;
            gameClearUISO.ApplyModifiedProperties();

            // ===== 10주차 스프린트: 보스 스포너 (경고 연출 -> 소환 -> 클리어 연결까지 총괄) =====
            GameObject bossSpawnerGO = new GameObject("BossSpawner");
            BossSpawner bossSpawner = bossSpawnerGO.AddComponent<BossSpawner>();
            SerializedObject bossSpawnerSO = new SerializedObject(bossSpawner);
            bossSpawnerSO.FindProperty("bossPrefab").objectReferenceValue = bossPrefab;
            bossSpawnerSO.FindProperty("player").objectReferenceValue = player.transform;
            bossSpawnerSO.FindProperty("cameraFollow").objectReferenceValue = cameraFollow;
            bossSpawnerSO.FindProperty("warningUI").objectReferenceValue = warningUI;
            bossSpawnerSO.FindProperty("bossHealthUI").objectReferenceValue = bossHealthUI;
            bossSpawnerSO.ApplyModifiedProperties();
        }

        // RainGaugeSlider와 동일한 배경+채움 구조의 표시 전용 슬라이더를 만드는 헬퍼 (HP/마나 슬라이더용).
        private static Slider CreateDebugSlider(Transform parent, string name, Vector2 anchoredPosition, Color fillColor)
        {
            GameObject sliderGO = new GameObject(name, typeof(RectTransform));
            sliderGO.transform.SetParent(parent, false);
            RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0f, 1f);
            sliderRect.anchorMax = new Vector2(0f, 1f);
            sliderRect.pivot = new Vector2(0f, 1f);
            sliderRect.anchoredPosition = anchoredPosition;
            sliderRect.sizeDelta = new Vector2(300f, 24f);

            GameObject bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(sliderGO.transform, false);
            RectTransform bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bgGO.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.8f);

            GameObject fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGO.transform.SetParent(sliderGO.transform, false);
            RectTransform fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(4f, 4f);
            fillAreaRect.offsetMax = new Vector2(-4f, -4f);

            GameObject fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            RectTransform fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillGO.GetComponent<Image>().color = fillColor;

            Slider slider = sliderGO.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.targetGraphic = null;
            slider.handleRect = null;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = 0f;

            return slider;
        }

        // RainGaugeText와 동일한 스타일의 디버그용 Text를 만드는 헬퍼.
        private static Text CreateDebugText(Transform parent, string name, Vector2 anchoredPosition, Font font, string initialText)
        {
            GameObject textGO = new GameObject(name, typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(parent, false);
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(0f, 1f);
            textRect.pivot = new Vector2(0f, 1f);
            textRect.anchoredPosition = anchoredPosition;
            textRect.sizeDelta = new Vector2(400f, 24f);

            Text text = textGO.GetComponent<Text>();
            text.font = font;
            text.fontSize = 16;
            text.color = Color.white;
            text.text = initialText;

            return text;
        }

        private static int GetOrCreateLayer(string layerName)
        {
            Object[] tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets.Length == 0)
                return LayerMask.NameToLayer("Default");

            SerializedObject tagManager = new SerializedObject(tagManagerAssets[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");

            for (int i = 8; i < layersProp.arraySize; i++)
            {
                if (layersProp.GetArrayElementAtIndex(i).stringValue == layerName)
                    return i;
            }

            for (int i = 8; i < layersProp.arraySize; i++)
            {
                SerializedProperty sp = layersProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(sp.stringValue))
                {
                    sp.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    return i;
                }
            }

            return LayerMask.NameToLayer("Default");
        }
    }
}
