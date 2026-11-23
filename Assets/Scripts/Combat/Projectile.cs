using UnityEngine;
using AesikHaiman.Enemy;
using AesikHaiman.Player;
using AesikHaiman.Audio;

namespace AesikHaiman.Combat
{
    /// <summary>
    /// 플레이어의 물방울(X 우산 콤보 회전)과 보스의 탄막 공격이 함께 쓰는 범용 발사체.
    /// damagesPlayer=false면 IDamageable(몬스터)을 때리고, true면 플레이어를 때립니다
    /// (적 투사체가 적을 맞히거나, 내 투사체가 나를 맞히는 걸 막기 위한 구분).
    /// 일정 거리 이동 또는 수명 시간 도달 시 자동으로 사라져 메모리 누수를 방지합니다.
    /// ===== 9주차 스프린트: 발사체 시스템 =====
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 10f;
        [SerializeField] private float maxDistance = 12f;
        [SerializeField] private float maxLifetime = 3f;
        [SerializeField] private GameObject hitEffectPrefab;

        private float _damage;
        private Vector2 _direction;
        private bool _damagesPlayer;
        private bool _hasHit;
        private Vector3 _spawnPosition;

        /// <summary>발사 시 호출. speedOverride를 주면 이 발사체만 다른 속도로 날아갑니다(보스 탄막 등).</summary>
        public void Launch(Vector2 direction, float damage, bool damagesPlayer = false, float? speedOverride = null)
        {
            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            _damage = damage;
            _damagesPlayer = damagesPlayer;
            if (speedOverride.HasValue)
                speed = speedOverride.Value;

            _spawnPosition = transform.position;
            Destroy(gameObject, maxLifetime);
        }

        private void Update()
        {
            transform.position += (Vector3)(_direction * speed * Time.deltaTime);

            if (Vector3.Distance(_spawnPosition, transform.position) >= maxDistance)
                Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasHit) return;

            if (_damagesPlayer)
            {
                if (!other.CompareTag("Player")) return;
                if (!other.TryGetComponent(out PlayerHealth playerHealth)) return;

                _hasHit = true;
                playerHealth.TakeDamage(Mathf.RoundToInt(_damage), _direction);
            }
            else
            {
                if (!other.TryGetComponent(out IDamageable damageable)) return;

                _hasHit = true;
                damageable.TakeDamage(_damage);
            }

            if (hitEffectPrefab != null)
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

            AudioManager.Instance?.PlayHitSfx();

            Destroy(gameObject);
        }
    }
}
