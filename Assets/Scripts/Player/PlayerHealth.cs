using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using AesikHaiman.Systems;
using AesikHaiman.Game;
using AesikHaiman.Audio;

namespace AesikHaiman.Player
{
    /// <summary>
    /// 체력/마나, 피격 처리(데미지/넉백/무적시간)를 담당.
    /// ===== 스파 가디언 힐 스킬은 Heal()/RestoreMana()를, 슈퍼아머 스킬은 SuperArmorState.Activate()를 호출하면 됨 =====
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerHealth : MonoBehaviour
    {
        [Header("체력")]
        [SerializeField] private int maxHp = 100;
        [SerializeField] private int currentHp;

        [Header("마나")]
        [Tooltip("스파 가디언 등 마나를 소모하는 스킬을 위한 자원. 아직 마나를 소모하는 스킬은 없지만 HealSystem의 마나 회복과 연동됩니다.")]
        [SerializeField] private float maxMana = 100f;
        [SerializeField] private float currentMana;

        [Header("피격 처리")]
        [Tooltip("7주차 명세: 약 0.5~1초 권장")]
        [SerializeField] private float invincibleDuration = 0.8f;
        [SerializeField] private float knockbackDuration = 0.2f;
        [SerializeField] private float defaultKnockbackForce = 8f;

        [Header("피격 시 깜빡임 연출 (플레이스홀더)")]
        [Tooltip("MeshRenderer/SpriteRenderer 등 렌더러를 연결하면 무적시간 동안 깜빡임 연출이 적용됩니다. 없어도 동작에는 문제없음.")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private float blinkInterval = 0.1f;

        [Header("이벤트 (UI 등에서 구독)")]
        public UnityEvent<int, int> onHpChanged;       // (currentHp, maxHp)
        public UnityEvent<float, float> onManaChanged; // (currentMana, maxMana)

        private Rigidbody2D _rb;
        private PlayerController _controller;
        private SuperArmorState _superArmor;
        private BuffSystem _buffSystem;
        private bool _isInvincible;
        private bool _isKnockedBack;

        public bool IsInvincible => _isInvincible;
        public bool IsKnockedBack => _isKnockedBack;
        public int CurrentHp => currentHp;
        public int MaxHp => maxHp;
        public float CurrentMana => currentMana;
        public float MaxMana => maxMana;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _controller = GetComponent<PlayerController>();
            _superArmor = GetComponent<SuperArmorState>(); // 없어도 동작(슈퍼아머 없는 캐릭터도 가능)
            _buffSystem = GetComponent<BuffSystem>();
            currentHp = maxHp;
            currentMana = maxMana;

            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<Renderer>();
        }

        /// <summary>
        /// 피격 처리. direction은 넉백 방향(내부에서 정규화), force가 0이면 기본값 사용.
        /// ===== 이후 "피격 무효/경직 감소" 같은 패시브 스킬을 추가할 때 이 함수 진입부에서 분기 처리하면 됨 =====
        /// </summary>
        public void TakeDamage(int damage, Vector2 direction, float force = 0f)
        {
            if (_isInvincible) return;

            // ===== 스파 가디언 방어 버프("계란 먹어"의 피해 감소 등)는 StatType.Defense 버프로 자동 반영됨 =====
            float defenseMultiplier = _buffSystem != null ? _buffSystem.GetStatMultiplier(StatType.Defense) : 1f;
            int mitigatedDamage = Mathf.RoundToInt(damage / defenseMultiplier);

            currentHp = Mathf.Max(0, currentHp - mitigatedDamage);
            onHpChanged?.Invoke(currentHp, maxHp);
            AudioManager.Instance?.PlayPlayerHitSfx();

            bool superArmorActive = _superArmor != null && _superArmor.IsActive;

            // 슈퍼아머가 켜져 있으면 히트 경직(애니메이션/넉백)과 무적시간을 생략하고 데미지만 적용.
            // ===== 스파 가디언 슈퍼아머 계열 스킬(예: 온천의 공주) 구현 시 SuperArmorState.Activate()만 호출하면 이 분기가 자동 적용됨 =====
            if (!superArmorActive)
            {
                _controller?.TriggerHitAnimation();
                StartCoroutine(KnockbackRoutine(direction.normalized, force > 0f ? force : defaultKnockbackForce));
                StartCoroutine(InvincibilityRoutine());
            }

            if (currentHp <= 0)
                Die();
        }

        /// <summary>
        /// 체력 즉시 회복. 최대 체력을 넘지 않도록 자동으로 clamp됩니다 (과다회복 방지).
        /// ===== 스파 가디언 힐 스킬(계란 먹어 등)은 HealSystem을 통해 이 함수를 호출하게 됨 =====
        /// </summary>
        public void Heal(int amount)
        {
            if (amount <= 0) return;
            currentHp = Mathf.Min(maxHp, currentHp + amount);
            onHpChanged?.Invoke(currentHp, maxHp);
        }

        /// <summary>마나 회복. 최대 마나를 넘지 않도록 자동으로 clamp됩니다 (과다회복 방지).</summary>
        public void RestoreMana(float amount)
        {
            if (amount <= 0f) return;
            currentMana = Mathf.Min(maxMana, currentMana + amount);
            onManaChanged?.Invoke(currentMana, maxMana);
        }

        /// <summary>마나 소모. 부족하면 소모하지 않고 false를 반환합니다.</summary>
        public bool ConsumeMana(float amount)
        {
            if (currentMana < amount) return false;
            currentMana -= amount;
            onManaChanged?.Invoke(currentMana, maxMana);
            return true;
        }

        private IEnumerator KnockbackRoutine(Vector2 direction, float force)
        {
            _isKnockedBack = true;
            _rb.linearVelocity = Vector2.zero;
            _rb.AddForce(direction * force, ForceMode2D.Impulse);

            yield return new WaitForSeconds(knockbackDuration);

            _isKnockedBack = false;
        }

        private IEnumerator InvincibilityRoutine()
        {
            _isInvincible = true;

            float elapsed = 0f;
            while (elapsed < invincibleDuration)
            {
                if (targetRenderer != null)
                    targetRenderer.enabled = !targetRenderer.enabled;

                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;
            }

            if (targetRenderer != null)
                targetRenderer.enabled = true;

            _isInvincible = false;
        }

        /// <summary>===== 7주차 스프린트: 체력 0 → GameManager가 게임을 멈추고 게임오버 UI를 띄움 =====</summary>
        private void Die()
        {
            Debug.Log("[PlayerHealth] 플레이어 사망 처리");
            GameManager.Instance?.TriggerGameOver();
        }
    }
}
