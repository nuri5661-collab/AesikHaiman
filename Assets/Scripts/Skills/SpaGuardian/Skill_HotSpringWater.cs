using System.Collections.Generic;
using UnityEngine;
using AesikHaiman.Player;
using AesikHaiman.Systems;

namespace AesikHaiman.Skills.SpaGuardian
{
    /// <summary>
    /// 스파 가디언 1차 전직 초월 스킬 - 온천수.
    /// 스킬 설명(기획 원문): 일정 범위에 온천을 소환한다. 범위 내 적은 지속 데미지와 방어력 감소 디버프를 받고,
    /// 범위 내 아군은 공격력 증가·극대화/크리티컬 확률 증가·동작속도&이동속도 증가·추가 데미지 버프를
    /// 동시에 얻는다.
    /// ===== 애니메이션 붙일 때 참고: 온천 소환 연출 후 HotSpringZone이 duration초 동안 유지·자동 소멸 =====
    /// 지금은 파티클 없이 반투명 원판(Cylinder)으로 온천 범위를 표시합니다.
    /// </summary>
    public class Skill_HotSpringWater : SkillBase
    {
        [Header("온천 범위/지속시간")]
        [SerializeField] private float radius = 3f;
        [SerializeField] private float duration = 8f;
        [SerializeField] private float tickInterval = 1f;

        [Header("적 효과 (지속 데미지 + 방어력 감소)")]
        [SerializeField] private float enemyDamagePerTick = 6f;
        [Tooltip("1보다 크면 그만큼 받는 데미지가 증가 (예: 1.25 = 피해 +25%)")]
        [SerializeField] private float enemyDefenseDebuffMultiplier = 1.25f;
        [SerializeField] private LayerMask enemyLayer;

        [Header("아군 버프 4종 (비워두면 Awake에서 자동 생성)")]
        [SerializeField] private BuffData attackPowerBuff;
        [SerializeField] private BuffData criticalChanceBuff;
        [SerializeField] private BuffData actionAndMoveSpeedBuff;
        [SerializeField] private BuffData bonusDamageBuff;
        [SerializeField] private float allyBuffDuration = 8f;
        [Tooltip("온천 소환 VFX. 비워두면 이펙트 없이 효과만 적용됨.")]
        [SerializeField] private GameObject hotSpringEffectPrefab;

        private AllyGroup _allyGroup;
        private List<BuffData> _allyBuffs;

        public override void Initialize(PlayerController owner, RainGaugeSystem gaugeSystem, BuffSystem buffSystem, HealSystem healSystem)
        {
            base.Initialize(owner, gaugeSystem, buffSystem, healSystem);
            _allyGroup = owner.GetComponent<AllyGroup>();
        }

        private void Awake()
        {
            if (attackPowerBuff == null)
                attackPowerBuff = BuffData.CreateRuntime("spa_guardian_hotspring_atk", "온천수 (공격력)",
                    StatType.AttackPower, BuffModifierType.PercentAdd, 0.15f, allyBuffDuration, BuffStackPolicy.Refresh);

            if (criticalChanceBuff == null)
                criticalChanceBuff = BuffData.CreateRuntime("spa_guardian_hotspring_crit", "온천수 (극대화 확률)",
                    StatType.CriticalChance, BuffModifierType.PercentAdd, 0.15f, allyBuffDuration, BuffStackPolicy.Refresh);

            if (actionAndMoveSpeedBuff == null)
                actionAndMoveSpeedBuff = BuffData.CreateRuntime("spa_guardian_hotspring_speed", "온천수 (동작/이동속도)",
                    StatType.ActionSpeed, BuffModifierType.PercentAdd, 0.1f, allyBuffDuration, BuffStackPolicy.Refresh);

            if (bonusDamageBuff == null)
                bonusDamageBuff = BuffData.CreateRuntime("spa_guardian_hotspring_bonusdmg", "온천수 (추가 데미지)",
                    StatType.BonusDamage, BuffModifierType.FlatAdd, 5f, allyBuffDuration, BuffStackPolicy.Refresh);

            _allyBuffs = new List<BuffData> { attackPowerBuff, criticalChanceBuff, actionAndMoveSpeedBuff, bonusDamageBuff };
        }

        protected override void Execute(float gaugeMultiplier)
        {
            GameObject zoneGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            zoneGO.name = "HotSpringZone_Placeholder";
            Object.Destroy(zoneGO.GetComponent<Collider>());
            zoneGO.transform.position = new Vector3(Owner.transform.position.x, Owner.transform.position.y - 0.9f, Owner.transform.position.z);
            zoneGO.transform.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);

            Renderer zoneRenderer = zoneGO.GetComponent<Renderer>();
            if (zoneRenderer != null)
                zoneRenderer.material.color = new Color(0.3f, 0.7f, 1f, 0.5f);

            HotSpringZone zone = zoneGO.AddComponent<HotSpringZone>();
            zone.Initialize(radius, duration, tickInterval, enemyDamagePerTick * gaugeMultiplier,
                enemyDefenseDebuffMultiplier, enemyLayer, _allyGroup, _allyBuffs);

            Debug.Log($"[{SkillName}] 발동 - 온천 소환 (반경 {radius}m, {duration:0.0}초 유지), " +
                      $"아군 버프 {_allyBuffs.Count}종(공격력/극대화/동작·이동속도/추가데미지) 동시 적용 시작");
        }

        protected override void PlayEffect()
        {
            if (hotSpringEffectPrefab != null)
                Instantiate(hotSpringEffectPrefab, Owner.transform.position, Quaternion.identity);
        }

        private void OnDrawGizmosSelected()
        {
            if (Owner == null) return;
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.4f);
            Gizmos.DrawWireSphere(Owner.transform.position, radius);
        }
    }
}
