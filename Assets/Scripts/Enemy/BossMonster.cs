using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using AesikHaiman.Player;
using AesikHaiman.Combat;

namespace AesikHaiman.Enemy
{
    /// <summary>
    /// 보스 몬스터. 평소엔 플레이어를 천천히 쫓아오다가, 일정 주기마다 돌진 또는 부채꼴 탄막 패턴을
    /// 번갈아 사용합니다. 탄막은 TestDummy/플레이어가 쓰는 것과 같은 Projectile을 재사용하되,
    /// damagesPlayer=true로 발사해서 플레이어만 맞도록 구분합니다.
    /// ===== 10주차 스프린트: 보스 몬스터 등장 패턴 =====
    /// </summary>
    public class BossMonster : MonoBehaviour, IDamageable
    {
        [Header("체력")]
        [SerializeField] private float maxHp = 1000f;
        [SerializeField] private float currentHp;

        [Header("추격 이동")]
        [Tooltip("비워두면 Player 태그를 가진 오브젝트를 자동으로 찾습니다.")]
        [SerializeField] private Transform target;
        [SerializeField] private float chaseSpeed = 1f;

        [Header("돌진 패턴")]
        [SerializeField] private float dashSpeed = 12f;
        [SerializeField] private float dashDuration = 0.4f;

        [Header("탄막(부채꼴 발사체) 패턴")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private int barrageCount = 7;
        [SerializeField] private float barrageSpreadAngle = 90f;
        [SerializeField] private float barrageDamage = 10f;
        [SerializeField] private float barrageProjectileSpeed = 6f;

        [SerializeField] private float patternInterval = 3f;

        [Header("플레이어 접촉 데미지")]
        [SerializeField] private int contactDamage = 15;
        [SerializeField] private float contactKnockback = 8f;

        [Header("이벤트 (UI 등에서 구독)")]
        public UnityEvent<float, float> onHpChanged; // (currentHp, maxHp)
        public UnityEvent onBossDefeated;

        private bool _isDead;
        private bool _isActing;

        public float CurrentHp => currentHp;
        public float MaxHp => maxHp;

        private void Awake()
        {
            currentHp = maxHp;
        }

        private void Start()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    target = player.transform;
            }

            StartCoroutine(PatternLoop());
        }

        private void Update()
        {
            if (_isDead || _isActing || target == null) return;

            Vector3 targetPos = new Vector3(target.position.x, transform.position.y, transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, targetPos, chaseSpeed * Time.deltaTime);
        }

        private IEnumerator PatternLoop()
        {
            while (!_isDead)
            {
                yield return new WaitForSeconds(patternInterval);
                if (_isDead) yield break;

                if (Random.value > 0.5f)
                    yield return StartCoroutine(DashAttack());
                else
                    BarrageAttack();
            }
        }

        private IEnumerator DashAttack()
        {
            if (target == null) yield break;
            _isActing = true;

            Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
            float elapsed = 0f;
            while (elapsed < dashDuration)
            {
                transform.position += (Vector3)(dir * dashSpeed * Time.deltaTime);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _isActing = false;
        }

        private void BarrageAttack()
        {
            if (projectilePrefab == null || target == null) return;

            Vector2 baseDir = ((Vector2)target.position - (Vector2)transform.position).normalized;
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
            float startAngle = baseAngle - barrageSpreadAngle / 2f;
            float step = barrageCount > 1 ? barrageSpreadAngle / (barrageCount - 1) : 0f;

            for (int i = 0; i < barrageCount; i++)
            {
                float angle = startAngle + step * i;
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

                GameObject projGO = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
                if (projGO.TryGetComponent(out Projectile projectile))
                    projectile.Launch(dir, barrageDamage, damagesPlayer: true, speedOverride: barrageProjectileSpeed);
            }
        }

        public void TakeDamage(float amount)
        {
            if (_isDead) return;

            currentHp = Mathf.Max(0f, currentHp - amount);
            onHpChanged?.Invoke(currentHp, maxHp);
            Debug.Log($"[BossMonster] {name} 피격: -{amount:0.0} → HP {currentHp:0}/{maxHp:0}");

            if (currentHp <= 0f)
            {
                _isDead = true;
                Debug.Log($"[BossMonster] {name} 처치됨");
                onBossDefeated?.Invoke();
                Destroy(gameObject);
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (_isDead) return;
            if (!collision.collider.CompareTag("Player")) return;
            if (!collision.collider.TryGetComponent(out PlayerHealth playerHealth)) return;

            Vector2 dir = collision.transform.position - transform.position;
            playerHealth.TakeDamage(contactDamage, dir, contactKnockback);
        }
    }
}
