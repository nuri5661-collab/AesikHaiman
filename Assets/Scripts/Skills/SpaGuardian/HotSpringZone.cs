using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AesikHaiman.Systems;
using AesikHaiman.Enemy;

namespace AesikHaiman.Skills.SpaGuardian
{
    /// <summary>
    /// "온천수" 스킬이 소환하는 온천 지역. 범위 내 아군에게는 여러 버프를 동시에, 적에게는
    /// 지속 데미지 + 방어력 감소 디버프를 tickInterval마다 다시 적용합니다.
    /// 파티클/모션 없이 반투명 원판(Cylinder, 콜라이더 제거)으로 표시되는 플레이스홀더입니다.
    /// Physics2D.OverlapCircleAll로 주기적으로 스캔하는 방식이라 Collider2D/트리거 이벤트에 의존하지 않습니다.
    /// </summary>
    public class HotSpringZone : MonoBehaviour
    {
        private float _radius;
        private float _duration;
        private float _tickInterval;
        private float _enemyDamagePerTick;
        private float _enemyDefenseDebuffMultiplier;
        private LayerMask _enemyLayer;
        private AllyGroup _allyGroup;
        private List<BuffData> _allyBuffs;

        public void Initialize(float radius, float duration, float tickInterval, float enemyDamagePerTick,
            float enemyDefenseDebuffMultiplier, LayerMask enemyLayer, AllyGroup allyGroup, List<BuffData> allyBuffs)
        {
            _radius = radius;
            _duration = duration;
            _tickInterval = tickInterval;
            _enemyDamagePerTick = enemyDamagePerTick;
            _enemyDefenseDebuffMultiplier = enemyDefenseDebuffMultiplier;
            _enemyLayer = enemyLayer;
            _allyGroup = allyGroup;
            _allyBuffs = allyBuffs;

            StartCoroutine(TickRoutine());
        }

        private IEnumerator TickRoutine()
        {
            float elapsed = 0f;
            while (elapsed < _duration)
            {
                int allyCount = ApplyToAllies();
                int enemyCount = ApplyToEnemies();
                Debug.Log($"[HotSpringZone] 온천 틱 - 아군 {allyCount}명 버프 {_allyBuffs.Count}종 재적용, 적 {enemyCount}명 데미지 {_enemyDamagePerTick:0.0}");

                yield return new WaitForSeconds(_tickInterval);
                elapsed += _tickInterval;
            }

            Destroy(gameObject);
        }

        private int ApplyToAllies()
        {
            if (_allyGroup == null) return 0;

            int count = 0;
            foreach (AllyGroup.AllyRef ally in _allyGroup.GetAllies())
            {
                if (ally.Health == null) continue;

                float dist = Vector2.Distance(ally.Health.transform.position, transform.position);
                if (dist > _radius) continue;

                foreach (BuffData buff in _allyBuffs)
                    ally.Buffs?.ApplyBuff(buff);

                count++;
            }
            return count;
        }

        private int ApplyToEnemies()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _radius, _enemyLayer);
            foreach (Collider2D hit in hits)
            {
                if (hit.TryGetComponent<IDamageable>(out var damageable))
                    damageable.TakeDamage(_enemyDamagePerTick);

                if (hit.TryGetComponent<TestDummy>(out var dummy))
                    dummy.ApplyDefenseDebuff(_enemyDefenseDebuffMultiplier, _tickInterval + 0.1f);
            }
            return hits.Length;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.3f);
            Gizmos.DrawSphere(transform.position, _radius);
        }
    }
}
