using System.Collections;
using UnityEngine;
using AesikHaiman.Player;
using AesikHaiman.Systems;
using AesikHaiman.Enemy;

namespace AesikHaiman.Skills.SpaGuardian
{
    /// <summary>
    /// 스파 가디언 1차 전직 액티브 스킬 - 첨벙첨벙.
    /// 스킬 설명(기획 원문): 슈퍼아머를 부여받은 뒤 제자리에서 2회 점프하며,
    /// 점프할 때마다 주변에 물을 튀겨 소량의 데미지를 입힌다.
    /// ===== 애니메이션 붙일 때 참고: 점프 1회 = hopInterval초 간격, 착지 타이밍에 물보라 이펙트 =====
    /// 지금은 애니메이션이 없어 실제로 캡슐을 Rigidbody2D로 두 번 점프시켜 y축 이동으로 대체합니다.
    /// </summary>
    public class Skill_SplishSplash : SkillBase
    {
        [Header("첨벙첨벙 수치")]
        [SerializeField] private float superArmorDuration = 1.5f;
        [SerializeField] private float hopForce = 9f;
        [Tooltip("점프 1회부터 다음 점프까지의 간격(초). 착지 타이밍에 맞춰 데미지 판정이 들어갑니다.")]
        [SerializeField] private float hopInterval = 0.4f;
        [SerializeField] private float splashRadius = 2.5f;
        [SerializeField] private float splashDamage = 8f;
        [SerializeField] private LayerMask enemyLayer;
        [Tooltip("물보라 VFX. 비워두면 이펙트 없이 데미지만 들어감.")]
        [SerializeField] private GameObject splashEffectPrefab;

        private SuperArmorState _superArmor;
        private Rigidbody2D _rb;

        public override void Initialize(PlayerController owner, RainGaugeSystem gaugeSystem, BuffSystem buffSystem, HealSystem healSystem)
        {
            base.Initialize(owner, gaugeSystem, buffSystem, healSystem);
            _superArmor = owner.GetComponent<SuperArmorState>();
            _rb = owner.GetComponent<Rigidbody2D>();
        }

        protected override void Execute(float gaugeMultiplier)
        {
            Debug.Log($"[{SkillName}] 발동 - 슈퍼아머 {superArmorDuration:0.0}초 부여, 2회 첨벙 예정");
            StartCoroutine(SplishSplashRoutine(gaugeMultiplier));
        }

        private IEnumerator SplishSplashRoutine(float gaugeMultiplier)
        {
            _superArmor?.Activate(superArmorDuration);

            for (int hop = 0; hop < 2; hop++)
            {
                if (_rb != null)
                    _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, hopForce);

                yield return new WaitForSeconds(hopInterval);

                float damage = splashDamage * gaugeMultiplier;
                int hitCount = SplashDamage(damage);

                if (splashEffectPrefab != null)
                    Instantiate(splashEffectPrefab, Owner.transform.position, Quaternion.identity);

                Debug.Log($"[{SkillName}] {hop + 1}번째 첨벙 - 반경 {splashRadius}m 데미지 {damage:0.0} (적중 {hitCount}개)");
            }
        }

        private int SplashDamage(float damage)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(Owner.transform.position, splashRadius, enemyLayer);
            foreach (Collider2D hit in hits)
            {
                if (hit.TryGetComponent<IDamageable>(out var damageable))
                    damageable.TakeDamage(damage);
            }
            return hits.Length;
        }

        protected override void PlayEffect()
        {
            // 첨벙첨벙은 2회 점프 각각 착지 타이밍에 물보라가 떠야 해서 PlayEffect()가 아니라
            // SplishSplashRoutine() 안에서 매 hop마다 직접 splashEffectPrefab을 Instantiate함.
        }

        private void OnDrawGizmosSelected()
        {
            if (Owner == null) return;
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.4f);
            Gizmos.DrawWireSphere(Owner.transform.position, splashRadius);
        }
    }
}
