using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AesikHaiman.Enemy;
using AesikHaiman.Audio;
using AesikHaiman.Combat;

namespace AesikHaiman.Player
{
    /// <summary>
    /// 기본 공격(Z)의 판정용 히트박스. 평소에는 콜라이더가 꺼져있다가 공격이 들어오는 순간에만
    /// 잠깐 켜져서 적을 때립니다. 같은 활성화 구간에서 같은 적을 여러 번 때리지 않도록 처리합니다.
    /// 5주차 스프린트 체크리스트("공격 키를 누를 때만 적이 데미지를 입는가?")에 대응하는 구현.
    /// 9주차 스프린트: 적중 시 파티클 + 타격음도 함께 재생.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class AttackHitbox : MonoBehaviour
    {
        [SerializeField] private GameObject hitEffectPrefab;

        private Collider2D _collider;
        private readonly HashSet<IDamageable> _hitThisActivation = new HashSet<IDamageable>();
        private Coroutine _activeRoutine;
        private float _pendingDamage;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _collider.isTrigger = true;
            _collider.enabled = false;
        }

        /// <summary>duration초 동안만 판정을 켜서, 그 사이에 닿은 적에게 한 번씩만 데미지를 줍니다.</summary>
        public void Activate(float damage, float duration)
        {
            if (_activeRoutine != null)
                StopCoroutine(_activeRoutine);

            _pendingDamage = damage;
            _activeRoutine = StartCoroutine(ActivateRoutine(duration));
        }

        private IEnumerator ActivateRoutine(float duration)
        {
            _hitThisActivation.Clear();
            _collider.enabled = true;

            yield return new WaitForSeconds(duration);

            _collider.enabled = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent<IDamageable>(out var damageable)) return;
            if (!_hitThisActivation.Add(damageable)) return; // 이미 이번 활성화에서 맞은 적

            damageable.TakeDamage(_pendingDamage);

            if (hitEffectPrefab != null)
                Instantiate(hitEffectPrefab, other.transform.position, Quaternion.identity);

            AudioManager.Instance?.PlayHitSfx();
        }
    }
}
