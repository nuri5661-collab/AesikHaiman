using UnityEngine;
using AesikHaiman.Player;
using AesikHaiman.Systems;
using AesikHaiman.Enemy;

namespace AesikHaiman.Skills.SpaGuardian
{
    /// <summary>
    /// 스파 가디언 1차 전직 강렬 스킬 - 목 마르지?.
    /// 스킬 설명(기획 원문): 전방 부채꼴 범위로 식혜 파도를 일으켜 적에게 데미지를 주고,
    /// 파도에 맞은 적에게는 "물고기 공격" 틱 데미지(짧은 지속 피해)가 추가로 들어간다.
    /// 아군에게는 마나를 회복시켜주며, "계란 먹어" 버프가 걸려있는 아군은 체력+마나 지속 회복을 추가로 받는다.
    /// ===== 애니메이션 붙일 때 참고: 파도가 뻗어나가는 타이밍에 부채꼴 판정, 착수 후 물고기가 튀는 연출 =====
    /// </summary>
    public class Skill_ThirstyHuh : SkillBase
    {
        [Header("식혜 파도 (부채꼴 범위)")]
        [SerializeField] private float waveRange = 3.5f;
        [Tooltip("정면 기준 좌우 각도. 30이면 전체 부채꼴이 60도.")]
        [SerializeField] private float waveHalfAngle = 30f;
        [SerializeField] private float waveDamage = 18f;
        [SerializeField] private LayerMask enemyLayer;
        [Tooltip("식혜 파도 VFX. 비워두면 이펙트 없이 데미지만 들어감.")]
        [SerializeField] private GameObject waveEffectPrefab;

        [Header("물고기 공격 (짧은 DOT)")]
        [SerializeField] private float fishDotTotalDamage = 9f;
        [SerializeField] private float fishDotDuration = 3f;
        [SerializeField] private float fishDotTickInterval = 1f;

        [Header("아군 마나 회복")]
        [SerializeField] private float allyManaRestore = 15f;

        [Header("계란 먹어 연계 추가 회복 (버프 보유 아군 전용)")]
        [SerializeField] private int eggComboHealAmount = 18;
        [SerializeField] private float eggComboManaAmount = 18f;
        [SerializeField] private float eggComboDuration = 4f;

        private AllyGroup _allyGroup;

        public override void Initialize(PlayerController owner, RainGaugeSystem gaugeSystem, BuffSystem buffSystem, HealSystem healSystem)
        {
            base.Initialize(owner, gaugeSystem, buffSystem, healSystem);
            _allyGroup = owner.GetComponent<AllyGroup>();
        }

        protected override void Execute(float gaugeMultiplier)
        {
            int enemyHitCount = DamageConeEnemies(gaugeMultiplier);
            int eggComboCount = RestoreAllyMana();

            Debug.Log($"[{SkillName}] 발동 - 식혜 파도 데미지 {waveDamage * gaugeMultiplier:0.0} x 적 {enemyHitCount}명 " +
                      $"(물고기 공격 DOT {fishDotDuration:0.0}초 추가), 아군 마나 {allyManaRestore:0} 회복 " +
                      $"(계란 먹어 연계 {eggComboCount}명 추가 회복)");
        }

        private int DamageConeEnemies(float gaugeMultiplier)
        {
            Collider2D[] candidates = Physics2D.OverlapCircleAll(Owner.transform.position, waveRange, enemyLayer);
            Vector2 facing = new Vector2(Mathf.Sign(Owner.transform.localScale.x), 0f);
            int hitCount = 0;

            foreach (Collider2D col in candidates)
            {
                Vector2 toTarget = (Vector2)col.transform.position - (Vector2)Owner.transform.position;
                if (toTarget.sqrMagnitude < 0.0001f) toTarget = facing;

                if (Vector2.Angle(facing, toTarget) > waveHalfAngle) continue;

                float damage = waveDamage * gaugeMultiplier;
                if (col.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(damage);
                    hitCount++;
                }

                if (col.TryGetComponent<TestDummy>(out var dummy))
                    dummy.ApplyDamageOverTime(fishDotTotalDamage * gaugeMultiplier, fishDotDuration, fishDotTickInterval);
            }

            return hitCount;
        }

        private int RestoreAllyMana()
        {
            if (_allyGroup == null) return 0;

            int eggComboCount = 0;
            foreach (AllyGroup.AllyRef ally in _allyGroup.GetAllies())
            {
                ally.Heal?.RestoreManaInstant(allyManaRestore);

                bool hasEggBuff = ally.Buffs != null && ally.Buffs.HasBuff(Skill_EatEgg.DamageReductionBuffId);
                if (!hasEggBuff) continue;

                ally.Heal?.HealOverTime(eggComboHealAmount, eggComboDuration, 1f);
                ally.Heal?.ManaOverTime(eggComboManaAmount, eggComboDuration, 1f);
                eggComboCount++;
            }

            return eggComboCount;
        }

        protected override void PlayEffect()
        {
            if (waveEffectPrefab == null) return;

            Vector2 facing = new Vector2(Mathf.Sign(Owner.transform.localScale.x), 0f);
            Vector3 spawnPos = Owner.transform.position + (Vector3)(facing * (waveRange * 0.5f));
            Instantiate(waveEffectPrefab, spawnPos, Quaternion.identity);
        }

        private void OnDrawGizmosSelected()
        {
            if (Owner == null) return;

            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.4f);
            Vector3 origin = Owner.transform.position;
            float facingSign = Mathf.Sign(Owner.transform.localScale.x);
            float centerAngle = facingSign > 0f ? 0f : 180f;

            const int segments = 12;
            Vector3 prevPoint = origin;
            for (int i = 0; i <= segments; i++)
            {
                float angle = centerAngle - waveHalfAngle + (waveHalfAngle * 2f * i / segments);
                Vector3 dir = Quaternion.Euler(0f, 0f, angle) * Vector3.right;
                Vector3 point = origin + dir * waveRange;

                Gizmos.DrawLine(origin, point);
                if (i > 0) Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
        }
    }
}
