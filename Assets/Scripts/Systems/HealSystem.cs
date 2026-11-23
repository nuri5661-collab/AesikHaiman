using System.Collections;
using UnityEngine;
using AesikHaiman.Player;

namespace AesikHaiman.Systems
{
    /// <summary>
    /// 체력/마나 회복을 담당. 즉시 회복과 지속 회복(HoT/MoT)을 모두 지원합니다.
    /// 실제 증감과 과다회복 방지는 PlayerHealth가 처리하고, 이 시스템은 "얼마나 / 언제" 회복시킬지만 관리합니다.
    /// ===== 스파 가디언 힐 스킬(계란 먹어, 온천수 등) 구현 시: skill의 Execute()에서
    ///        HealInstant/HealOverTime/RestoreManaInstant/ManaOverTime을 호출하면 됨 =====
    /// </summary>
    [RequireComponent(typeof(PlayerHealth))]
    public class HealSystem : MonoBehaviour
    {
        private PlayerHealth _health;

        private void Awake()
        {
            _health = GetComponent<PlayerHealth>();
        }

        /// <summary>체력 즉시 회복 (최대치를 넘지 않도록 PlayerHealth에서 자동 clamp).</summary>
        public void HealInstant(int amount) => _health.Heal(amount);

        /// <summary>마나 즉시 회복 (최대치를 넘지 않도록 PlayerHealth에서 자동 clamp).</summary>
        public void RestoreManaInstant(float amount) => _health.RestoreMana(amount);

        /// <summary>
        /// 지속 회복(HoT). totalAmount를 duration 동안 tickInterval 간격으로 나눠서 회복시킵니다.
        /// 예: HealOverTime(30, 6f, 1f) → 6초 동안 1초마다 5씩 회복(총 30).
        /// </summary>
        public void HealOverTime(int totalAmount, float duration, float tickInterval = 1f)
        {
            StartCoroutine(HealOverTimeRoutine(totalAmount, duration, tickInterval));
        }

        /// <summary>지속 마나 회복(MoT). HealOverTime과 동일한 방식으로 마나에 적용.</summary>
        public void ManaOverTime(float totalAmount, float duration, float tickInterval = 1f)
        {
            StartCoroutine(ManaOverTimeRoutine(totalAmount, duration, tickInterval));
        }

        private IEnumerator HealOverTimeRoutine(int totalAmount, float duration, float tickInterval)
        {
            int tickCount = Mathf.Max(1, Mathf.RoundToInt(duration / tickInterval));
            int perTick = Mathf.Max(1, totalAmount / tickCount);

            for (int i = 0; i < tickCount; i++)
            {
                yield return new WaitForSeconds(tickInterval);
                _health.Heal(perTick);
            }
        }

        private IEnumerator ManaOverTimeRoutine(float totalAmount, float duration, float tickInterval)
        {
            int tickCount = Mathf.Max(1, Mathf.RoundToInt(duration / tickInterval));
            float perTick = totalAmount / tickCount;

            for (int i = 0; i < tickCount; i++)
            {
                yield return new WaitForSeconds(tickInterval);
                _health.RestoreMana(perTick);
            }
        }
    }
}
