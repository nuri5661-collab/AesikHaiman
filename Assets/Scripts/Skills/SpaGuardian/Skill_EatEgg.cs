using UnityEngine;
using AesikHaiman.Player;
using AesikHaiman.Systems;
using AesikHaiman.Enemy;

namespace AesikHaiman.Skills.SpaGuardian
{
    /// <summary>
    /// 스파 가디언 1차 전직 강인 스킬 - 계란 먹어.
    /// 스킬 설명(기획 원문): 머리 위로 계란을 아군 수만큼 투척한다. 계란을 맞은 아군은 슈퍼아머를 부여받고
    /// 피해 감소 버프와 약간의 체력을 회복하며, 계란에 맞은 적에게는 데미지를 준다.
    /// ===== 애니메이션 붙일 때 참고: 투척 모션 끝나는 시점에 계란 낙하 이펙트, 아군 인원수만큼 순차 투척 =====
    /// 지금은 싱글플레이어라 아군이 자기 자신 1명뿐이지만, AllyGroup.GetAllies()를 순회하는 구조라
    /// 파티원이 늘어나면 자동으로 전원에게 적용됩니다.
    /// </summary>
    public class Skill_EatEgg : SkillBase
    {
        /// <summary>목 마르지?가 "계란 먹어 버프 보유 여부"를 확인할 때 쓰는 버프 ID.</summary>
        public const string DamageReductionBuffId = "spa_guardian_egg_dr";

        [Header("계란 먹어 수치")]
        [SerializeField] private float enemyDamage = 15f;
        [SerializeField] private float enemyRadius = 2f;
        [SerializeField] private float allySuperArmorDuration = 2f;
        [SerializeField] private int allyHealAmount = 15;

        [Header("피해 감소 버프 (비워두면 Awake에서 자동 생성)")]
        [SerializeField] private BuffData damageReductionBuff;
        [Tooltip("Defense 스탯 PercentAdd 값. 0.3 = 받는 데미지 약 -23%(1/1.3)")]
        [SerializeField] private float damageReductionValue = 0.3f;
        [SerializeField] private float damageReductionDuration = 8f;

        [SerializeField] private LayerMask enemyLayer;
        [Tooltip("계란 투척 VFX. 비워두면 이펙트 없이 효과만 적용됨.")]
        [SerializeField] private GameObject eggEffectPrefab;

        private AllyGroup _allyGroup;

        public override void Initialize(PlayerController owner, RainGaugeSystem gaugeSystem, BuffSystem buffSystem, HealSystem healSystem)
        {
            base.Initialize(owner, gaugeSystem, buffSystem, healSystem);
            _allyGroup = owner.GetComponent<AllyGroup>();
        }

        private void Awake()
        {
            if (damageReductionBuff == null)
            {
                damageReductionBuff = BuffData.CreateRuntime(
                    id: DamageReductionBuffId,
                    name: "계란 먹어 (피해 감소)",
                    statType: StatType.Defense,
                    modifierType: BuffModifierType.PercentAdd,
                    value: damageReductionValue,
                    duration: damageReductionDuration,
                    stackPolicy: BuffStackPolicy.Refresh);
            }
        }

        protected override void Execute(float gaugeMultiplier)
        {
            int allyCount = ApplyToAllies();
            float damage = enemyDamage * gaugeMultiplier;
            int enemyHitCount = DamageEnemies(damage);

            Debug.Log($"[{SkillName}] 발동 - 계란 {allyCount}개 투척, 아군 {allyCount}명에게 슈퍼아머 {allySuperArmorDuration:0.0}초 + " +
                      $"피해감소 {damageReductionDuration:0.0}초 + 체력 {allyHealAmount} 회복, 적 {enemyHitCount}명에게 데미지 {damage:0.0}");
        }

        private int ApplyToAllies()
        {
            if (_allyGroup == null) return 0;

            int count = 0;
            foreach (AllyGroup.AllyRef ally in _allyGroup.GetAllies())
            {
                ally.SuperArmor?.Activate(allySuperArmorDuration);
                ally.Buffs?.ApplyBuff(damageReductionBuff);
                ally.Heal?.HealInstant(allyHealAmount);
                count++;
            }
            return count;
        }

        private int DamageEnemies(float damage)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(Owner.transform.position, enemyRadius, enemyLayer);
            foreach (Collider2D hit in hits)
            {
                if (hit.TryGetComponent<IDamageable>(out var damageable))
                    damageable.TakeDamage(damage);
            }
            return hits.Length;
        }

        protected override void PlayEffect()
        {
            if (eggEffectPrefab != null)
                Instantiate(eggEffectPrefab, Owner.transform.position + Vector3.up, Quaternion.identity);
        }

        private void OnDrawGizmosSelected()
        {
            if (Owner == null) return;
            Gizmos.color = new Color(1f, 0.9f, 0.5f, 0.4f);
            Gizmos.DrawWireSphere(Owner.transform.position, enemyRadius);
        }
    }
}
