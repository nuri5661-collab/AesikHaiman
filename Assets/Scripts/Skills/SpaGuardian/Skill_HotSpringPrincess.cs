using UnityEngine;
using AesikHaiman.Player;
using AesikHaiman.Systems;

namespace AesikHaiman.Skills.SpaGuardian
{
    /// <summary>
    /// 스파 가디언 1차 전직 패시브 스킬 - 온천의 공주.
    /// 스킬 설명(기획 원문): 물 관련 스킬을 사용 중이거나 그 효과가 지속되는 동안 체력과 마나를 틱마다 회복한다.
    ///
    /// "물 관련 스킬 효과가 지속되는 동안"의 판정 기준:
    /// - BuffSystem에 활성 버프가 하나라도 있음 (첨벙첨벙/계란 먹어/온천수가 건 버프 포함)
    /// - 또는 SuperArmorState가 켜져있음 (첨벙첨벙/계란 먹어의 슈퍼아머)
    /// 위 둘 중 하나라도 참이면 물 관련 효과가 지속 중이라고 보고 회복을 틱합니다.
    ///
    /// 패시브라 SkillManager가 입력으로 TryActivate()를 호출하지 않습니다 (SkillSlot.Passive는 입력 스킵).
    /// 대신 SkillBase.Update()를 오버라이드해서 매 프레임 상시 체크하는 방식으로 구현했습니다.
    /// </summary>
    public class Skill_HotSpringPrincess : SkillBase
    {
        [Header("온천의 공주 수치")]
        [SerializeField] private float tickInterval = 1f;
        [SerializeField] private int hpTickAmount = 3;
        [SerializeField] private float manaTickAmount = 3f;
        [Tooltip("틱마다 뜨는 회복 반짝임 VFX. 비워두면 이펙트 없이 회복만 적용됨.")]
        [SerializeField] private GameObject healTickEffectPrefab;

        private SuperArmorState _superArmor;
        private float _tickTimer;

        public override void Initialize(PlayerController owner, RainGaugeSystem gaugeSystem, BuffSystem buffSystem, HealSystem healSystem)
        {
            base.Initialize(owner, gaugeSystem, buffSystem, healSystem);
            _superArmor = owner.GetComponent<SuperArmorState>();
        }

        protected override void Update()
        {
            base.Update(); // 쿨타임 처리 (패시브는 TryActivate()를 거치지 않아 사실상 의미 없지만 베이스 동작 유지)

            bool waterEffectActive = (BuffSystem != null && BuffSystem.ActiveBuffs.Count > 0)
                                      || (_superArmor != null && _superArmor.IsActive);

            if (!waterEffectActive)
            {
                _tickTimer = 0f;
                return;
            }

            _tickTimer += Time.deltaTime;
            if (_tickTimer < tickInterval) return;
            _tickTimer = 0f;

            HealSystem?.HealInstant(hpTickAmount);
            HealSystem?.RestoreManaInstant(manaTickAmount);

            if (healTickEffectPrefab != null)
                Instantiate(healTickEffectPrefab, Owner.transform.position + Vector3.up * 0.5f, Quaternion.identity);

            Debug.Log($"[{SkillName}] 패시브 발동 - 물 관련 효과 지속 중, HP +{hpTickAmount} / Mana +{manaTickAmount:0} 틱 회복");
        }

        /// <summary>패시브는 TryActivate()로 발동되지 않고 Update()에서 상시 체크되므로 실제로 호출되지 않습니다.</summary>
        protected override void Execute(float gaugeMultiplier)
        {
        }
    }
}
