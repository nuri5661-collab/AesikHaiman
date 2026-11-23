using UnityEngine;
using UnityEngine.Events;

namespace AesikHaiman.Systems
{
    /// <summary>
    /// 애식 하이먼의 고유 메카닉인 "강수량 게이지" 시스템.
    /// V키를 누르고 있거나 물 관련 스킬을 사용하면 게이지가 쌓이고,
    /// 게이지 수치 구간에 따라 스킬 위력 배율이 달라집니다.
    /// </summary>
    public class RainGaugeSystem : MonoBehaviour
    {
        [Header("게이지 기본 설정 (임시 수치, 인스펙터에서 조절 가능)")]
        [SerializeField] private float maxGauge = 100f;
        [SerializeField] private float currentGauge = 0f;

        [Header("V키 입력 축적")]
        [Tooltip("V키를 누르고 있는 동안 초당 축적되는 게이지 양")]
        [SerializeField] private float gaugeGainPerSecondHeld = 15f;

        [Header("자연 감소 (선택 사항)")]
        [Tooltip("체크하면 시간이 지날수록 게이지가 서서히 줄어듭니다")]
        [SerializeField] private bool enablePassiveDecay = false;
        [SerializeField] private float passiveDecayPerSecond = 2f;

        [Header("게이지 구간별 스킬 위력 배율")]
        [Tooltip("게이지 퍼센트(0~100) 구간별 배율. 스킬 15종을 추가하며 밸런스에 맞게 조정하세요.")]
        [SerializeField]
        private GaugeTier[] gaugeTiers =
        {
            new GaugeTier { thresholdPercent = 0f,   multiplier = 1.0f },
            new GaugeTier { thresholdPercent = 33f,  multiplier = 1.2f },
            new GaugeTier { thresholdPercent = 66f,  multiplier = 1.5f },
            new GaugeTier { thresholdPercent = 100f, multiplier = 2.0f },
        };

        [Header("이벤트 (UI 등에서 구독)")]
        public UnityEvent<float, float> onGaugeChanged; // (currentGauge, maxGauge)

        public float CurrentGauge => currentGauge;
        public float MaxGauge => maxGauge;
        public float GaugePercent => maxGauge <= 0f ? 0f : currentGauge / maxGauge * 100f;

        [System.Serializable]
        public struct GaugeTier
        {
            [Tooltip("이 배율이 적용되기 시작하는 게이지 퍼센트 (0~100)")]
            public float thresholdPercent;
            public float multiplier;
        }

        private void Update()
        {
            HandleGaugeInput();

            if (enablePassiveDecay && currentGauge > 0f)
            {
                AddGauge(-passiveDecayPerSecond * Time.deltaTime);
            }
        }

        private void HandleGaugeInput()
        {
            // V키를 누르고 있는 동안 게이지 축적.
            // ===== 이후 "물 관련 스킬" 전용 입력(예: 차징 스킬)이 추가되면 동일한 방식으로 AddGauge() 호출 =====
            if (Input.GetKey(KeyCode.V))
            {
                AddGauge(gaugeGainPerSecondHeld * Time.deltaTime);
            }
        }

        /// <summary>
        /// 게이지를 증가(amount가 음수면 감소)시킵니다.
        /// 스킬 사용 시 게이지를 쌓고 싶다면 SkillBase.TryActivate()가 자동으로 호출합니다.
        /// ===== 스킬 15종 중 "게이지를 직접 채우는" 효과가 있다면 이 함수를 그대로 사용하면 됨 =====
        /// </summary>
        public void AddGauge(float amount)
        {
            currentGauge = Mathf.Clamp(currentGauge + amount, 0f, maxGauge);
            onGaugeChanged?.Invoke(currentGauge, maxGauge);
        }

        public bool HasEnoughGauge(float amount) => currentGauge >= amount;

        public bool ConsumeGauge(float amount)
        {
            if (!HasEnoughGauge(amount)) return false;
            AddGauge(-amount);
            return true;
        }

        /// <summary>
        /// 현재 게이지 수치에 따른 스킬 위력 배율을 반환합니다.
        /// 스킬 구현 시: finalDamage = baseDamage * gaugeSystem.GetGaugeMultiplier();
        /// </summary>
        public float GetGaugeMultiplier()
        {
            float percent = GaugePercent;
            float result = gaugeTiers.Length > 0 ? gaugeTiers[0].multiplier : 1f;

            foreach (var tier in gaugeTiers)
            {
                if (percent >= tier.thresholdPercent)
                    result = tier.multiplier;
            }

            return result;
        }
    }
}
