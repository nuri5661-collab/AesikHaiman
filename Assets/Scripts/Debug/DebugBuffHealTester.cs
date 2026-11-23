using UnityEngine;
using AesikHaiman.Systems;

namespace AesikHaiman.DebugTools
{
    /// <summary>
    /// BuffSystem/HealSystem이 실제로 동작하는지 눈/Console로 확인하기 위한 임시 테스트용 스크립트.
    /// B키: 테스트 버프(공격력 +20%, 8초, Refresh 정책) 부여
    /// N키: 체력 즉시 회복 테스트 (H키는 DebugHitTester가 이미 쓰고 있어서 힐은 N으로 배정)
    /// M키: 마나 지속 회복 테스트
    /// 스파 가디언 스킬이 실제로 구현되면 이 스크립트는 비활성화하거나 삭제해도 됩니다.
    /// </summary>
    public class DebugBuffHealTester : MonoBehaviour
    {
        [SerializeField] private BuffSystem buffSystem;
        [SerializeField] private HealSystem healSystem;

        [Header("테스트 버프 (비워두면 Awake에서 자동 생성)")]
        [Tooltip("실제 스파 가디언 버프 스킬을 만들 때는 BuffData 에셋을 만들어 여기에 연결하는 방식을 참고하세요.")]
        [SerializeField] private BuffData testAttackBuff;

        [Header("테스트 키")]
        [SerializeField] private KeyCode testBuffKey = KeyCode.B;
        [SerializeField] private KeyCode testHealKey = KeyCode.N;
        [SerializeField] private KeyCode testManaKey = KeyCode.M;

        [Header("테스트 수치")]
        [SerializeField] private int testHealAmount = 20;
        [SerializeField] private float testManaAmount = 20f;
        [SerializeField] private float testManaDuration = 3f;

        private void Awake()
        {
            // 인스펙터에 버프 에셋을 연결하지 않았다면 디버그용으로 즉석에서 하나 생성
            if (testAttackBuff == null)
            {
                testAttackBuff = BuffData.CreateRuntime(
                    id: "debug_attack_up",
                    name: "테스트 공격력 버프",
                    statType: StatType.AttackPower,
                    modifierType: BuffModifierType.PercentAdd,
                    value: 0.2f,
                    duration: 8f,
                    stackPolicy: BuffStackPolicy.Refresh);
            }
        }

        private void Update()
        {
            if (buffSystem != null && Input.GetKeyDown(testBuffKey))
                buffSystem.ApplyBuff(testAttackBuff);

            if (healSystem != null && Input.GetKeyDown(testHealKey))
                healSystem.HealInstant(testHealAmount);

            if (healSystem != null && Input.GetKeyDown(testManaKey))
                healSystem.ManaOverTime(testManaAmount, testManaDuration);
        }
    }
}
