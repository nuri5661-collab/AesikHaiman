using System.Collections;
using UnityEngine;
using AesikHaiman.Game;
using AesikHaiman.Player;

namespace AesikHaiman.Enemy
{
    /// <summary>
    /// 스킬의 데미지/방어력 감소 디버프/도트가 실제로 들어가는지 Console로 확인하기 위한 테스트용 몬스터(슬라임 역할).
    /// 6~7주차 스프린트부터는 EnemySpawner가 계속 생성하고, 플레이어와 닿으면 데미지를 주고, 죽으면 점수를 올립니다.
    /// 실제 적 AI가 만들어지면 이 스크립트는 IDamageable 구현 참고용으로만 남기고 교체해도 됩니다.
    /// </summary>
    public class TestDummy : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHp = 200f;
        [SerializeField] private float currentHp;
        [Tooltip("이 몬스터를 처치했을 때 GameManager에 더해지는 점수. 6주차 명세서의 exp_drop 예시값(10)과 동일.")]
        [SerializeField] private int scoreValue = 10;

        [Header("추격 이동 (5주차 스프린트)")]
        [Tooltip("비워두면 Player 태그를 가진 오브젝트를 자동으로 찾아 추격합니다.")]
        [SerializeField] private Transform chaseTarget;
        [SerializeField] private float moveSpeed = 1.5f;
        [Tooltip("GameManager.KillCount 1당 이동속도가 이만큼 추가됩니다 (6주차 난이도 조절).")]
        [SerializeField] private float moveSpeedPerKill = 0.05f;

        [Header("플레이어 공격 (7주차 스프린트)")]
        [SerializeField] private int contactDamage = 10;
        [Tooltip("접촉 시 플레이어에게 가하는 넉백 힘. 0이면 PlayerHealth의 기본값 사용.")]
        [SerializeField] private float contactKnockback = 6f;

        [Header("피격 시각 피드백 (9주차 스프린트)")]
        [SerializeField] private float hitFlashDuration = 0.1f;
        [SerializeField] private float hitKnockbackDistance = 0.3f;
        [SerializeField] private float hitKnockbackDuration = 0.1f;

        private float _defenseMultiplier = 1f;
        private Coroutine _defenseDebuffRoutine;
        private bool _isDead;
        private bool _isKnockedBack;
        private Renderer _renderer;
        private Color _originalColor;

        public float CurrentHp => currentHp;
        public float MaxHp => maxHp;

        private void Awake()
        {
            currentHp = maxHp;
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
                _originalColor = _renderer.material.color;
        }

        private void Start()
        {
            if (chaseTarget == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    chaseTarget = player.transform;
            }

            // 처치 수가 쌓일수록 새로 스폰되는 몬스터가 살짝 더 빨라짐 (6주차 난이도 조절)
            int killCount = GameManager.Instance != null ? GameManager.Instance.KillCount : 0;
            moveSpeed += killCount * moveSpeedPerKill;
        }

        private void Update()
        {
            if (_isDead || chaseTarget == null || _isKnockedBack) return;

            // 플레이어가 점프해도 위아래로 따라 뜨지 않도록 x축으로만 이동
            Vector3 targetPos = new Vector3(chaseTarget.position.x, transform.position.y, transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
        }

        public void TakeDamage(float amount)
        {
            if (_isDead) return;

            float finalAmount = amount * _defenseMultiplier;
            currentHp = Mathf.Max(0f, currentHp - finalAmount);
            Debug.Log($"[TestDummy] {name} 피격: -{finalAmount:0.0} (방어배율 x{_defenseMultiplier:0.00}) → HP {currentHp:0}/{maxHp:0}");

            if (currentHp <= 0f)
            {
                _isDead = true;
                Debug.Log($"[TestDummy] {name} 처치됨");

                GameManager.Instance?.AddScore(scoreValue);
                GameManager.Instance?.RegisterKill();

                Destroy(gameObject);
                return;
            }

            // ===== 9주차 스프린트: 몬스터 피격 시 흰색 깜빡임 + 살짝 넉백 =====
            StartCoroutine(HitFlashRoutine());
            StartCoroutine(HitKnockbackRoutine());
        }

        private IEnumerator HitFlashRoutine()
        {
            if (_renderer == null) yield break;

            _renderer.material.color = Color.white;
            yield return new WaitForSeconds(hitFlashDuration);

            if (!_isDead && _renderer != null)
                _renderer.material.color = _originalColor;
        }

        private IEnumerator HitKnockbackRoutine()
        {
            if (chaseTarget == null) yield break;

            _isKnockedBack = true;

            Vector3 dir = transform.position - chaseTarget.position;
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.right;

            Vector3 start = transform.position;
            Vector3 end = start + dir * hitKnockbackDistance;
            float elapsed = 0f;

            while (elapsed < hitKnockbackDuration)
            {
                transform.position = Vector3.Lerp(start, end, elapsed / hitKnockbackDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _isKnockedBack = false;
        }

        /// <summary>
        /// 플레이어와 부딪히면(닿아있는 동안 매 프레임) 데미지를 시도합니다.
        /// 실제 중복 피격 방지는 PlayerHealth의 무적시간이 처리하므로 여기서는 쿨다운을 따로 두지 않습니다.
        /// </summary>
        private void OnCollisionStay2D(Collision2D collision)
        {
            if (_isDead) return;
            if (!collision.collider.CompareTag("Player")) return;
            if (!collision.collider.TryGetComponent(out PlayerHealth playerHealth)) return;

            Vector2 knockbackDir = (Vector2)(collision.transform.position - transform.position);
            playerHealth.TakeDamage(contactDamage, knockbackDir, contactKnockback);
        }

        /// <summary>방어력 감소 디버프. multiplier가 1보다 크면 그만큼 받는 데미지가 증가합니다 (예: 1.3 = 피해 +30%).</summary>
        public void ApplyDefenseDebuff(float multiplier, float duration)
        {
            if (_defenseDebuffRoutine != null)
                StopCoroutine(_defenseDebuffRoutine);
            _defenseDebuffRoutine = StartCoroutine(DefenseDebuffRoutine(multiplier, duration));
        }

        private IEnumerator DefenseDebuffRoutine(float multiplier, float duration)
        {
            _defenseMultiplier = multiplier;
            Debug.Log($"[TestDummy] {name} 방어력 감소 디버프 적용 (받는 데미지 x{multiplier:0.00}, {duration:0.0}초)");

            yield return new WaitForSeconds(duration);

            _defenseMultiplier = 1f;
        }

        /// <summary>지속 데미지(DOT). totalAmount를 duration 동안 tickInterval 간격으로 나눠서 적용합니다.</summary>
        public void ApplyDamageOverTime(float totalAmount, float duration, float tickInterval)
        {
            StartCoroutine(DamageOverTimeRoutine(totalAmount, duration, tickInterval));
        }

        private IEnumerator DamageOverTimeRoutine(float totalAmount, float duration, float tickInterval)
        {
            int tickCount = Mathf.Max(1, Mathf.RoundToInt(duration / tickInterval));
            float perTick = totalAmount / tickCount;

            for (int i = 0; i < tickCount; i++)
            {
                yield return new WaitForSeconds(tickInterval);
                TakeDamage(perTick);
            }
        }
    }
}
