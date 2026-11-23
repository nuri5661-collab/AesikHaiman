using UnityEngine;

namespace AesikHaiman.Systems
{
    /// <summary>
    /// 버프/디버프가 영향을 주는 스탯 종류.
    /// ===== 새로운 종류의 버프가 필요하면(예: 방어력, 쿨타임 감소) 여기에 추가 =====
    /// </summary>
    public enum StatType
    {
        AttackPower,
        MoveSpeed,
        Defense,
        CriticalChance, // 극대화/크리티컬 확률 (온천수). 실제 치명타 판정 로직은 아직 없고, 배율 조회만 가능
        ActionSpeed,    // 동작속도 (온천수). 실제 애니메이션/쿨타임 반영 로직은 아직 없고, 배율 조회만 가능
        BonusDamage,    // 추가 데미지 (온천수)
    }

    /// <summary>버프 수치를 스탯에 적용하는 방식.</summary>
    public enum BuffModifierType
    {
        PercentAdd, // 퍼센트 가산 (예: value 0.2 = +20%, -0.3 = -30%)
        FlatAdd,    // 고정값 가산
    }

    /// <summary>같은 buffId를 가진 버프가 중복 적용될 때의 정책.</summary>
    public enum BuffStackPolicy
    {
        Refresh, // 지속시간만 새로 갱신 (기본값)
        Stack,   // 효과가 중첩되고 지속시간도 갱신
        Ignore,  // 이미 적용 중이면 무시
    }

    /// <summary>
    /// 버프/디버프 하나의 정의. ScriptableObject라 에셋으로 만들어두면 인스펙터에서 수치를 바로 조절할 수 있습니다.
    /// ===== 스파 가디언 버프 스킬(첨벙첨벙의 방어력 버프 등) 구현 시: 이 에셋을 Project 창에서
    ///        우클릭 > Create > AesikHaiman > Buff Data 로 새로 만들어 스킬에서 참조하면 됨 =====
    /// </summary>
    [CreateAssetMenu(menuName = "AesikHaiman/Buff Data", fileName = "NewBuffData")]
    public class BuffData : ScriptableObject
    {
        [Tooltip("버프 종류를 구분하는 고유 ID. 같은 ID끼리 StackPolicy가 적용됩니다.")]
        [SerializeField] private string buffId = "new_buff";
        [SerializeField] private string displayName = "New Buff";

        [Header("효과")]
        [SerializeField] private StatType statType = StatType.AttackPower;
        [SerializeField] private BuffModifierType modifierType = BuffModifierType.PercentAdd;
        [Tooltip("PercentAdd면 0.2 = +20%, -0.3 = -30%. FlatAdd면 그대로 더해지는 값.")]
        [SerializeField] private float value = 0.2f;

        [Header("지속시간 / 중첩 정책")]
        [SerializeField] private float duration = 8f;
        [SerializeField] private BuffStackPolicy stackPolicy = BuffStackPolicy.Refresh;
        [Tooltip("StackPolicy가 Stack일 때 최대 중첩 수 (0이면 무제한)")]
        [SerializeField] private int maxStacks = 0;

        public string BuffId => buffId;
        public string DisplayName => displayName;
        public StatType StatType => statType;
        public BuffModifierType ModifierType => modifierType;
        public float Value => value;
        public float Duration => duration;
        public BuffStackPolicy StackPolicy => stackPolicy;
        public int MaxStacks => maxStacks;

        /// <summary>
        /// 에셋 없이 코드에서 즉석으로 버프를 만들 때 사용 (디버그 테스트 등).
        /// 실제 스킬용 버프는 되도록 에셋(.asset)으로 만들어 인스펙터에서 조절하는 걸 권장합니다.
        /// </summary>
        public static BuffData CreateRuntime(string id, string name, StatType statType, BuffModifierType modifierType,
            float value, float duration, BuffStackPolicy stackPolicy, int maxStacks = 0)
        {
            BuffData data = CreateInstance<BuffData>();
            data.buffId = id;
            data.displayName = name;
            data.statType = statType;
            data.modifierType = modifierType;
            data.value = value;
            data.duration = duration;
            data.stackPolicy = stackPolicy;
            data.maxStacks = maxStacks;
            return data;
        }
    }
}
