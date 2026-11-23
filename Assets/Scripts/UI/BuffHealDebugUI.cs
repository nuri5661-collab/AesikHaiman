using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using AesikHaiman.Player;
using AesikHaiman.Systems;

namespace AesikHaiman.UI
{
    /// <summary>
    /// 디버그용 HP/마나/활성 버프 목록 UI. RainGaugeUI와 동일하게 슬라이더 + 텍스트로 대체합니다.
    /// ===== 아트 붙일 때: 이 스크립트는 그대로 두고 Slider/Text만 실제 UI 프리팹으로 교체하면 됨 =====
    /// </summary>
    public class BuffHealDebugUI : MonoBehaviour
    {
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private BuffSystem buffSystem;

        [SerializeField] private Slider hpSlider;
        [SerializeField] private Text hpText;
        [SerializeField] private Slider manaSlider;
        [SerializeField] private Text manaText;
        [SerializeField] private Text buffListText;

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.onHpChanged.AddListener(OnHpChanged);
                playerHealth.onManaChanged.AddListener(OnManaChanged);
            }

            if (buffSystem != null)
                buffSystem.onBuffsChanged.AddListener(OnBuffsChanged);
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.onHpChanged.RemoveListener(OnHpChanged);
                playerHealth.onManaChanged.RemoveListener(OnManaChanged);
            }

            if (buffSystem != null)
                buffSystem.onBuffsChanged.RemoveListener(OnBuffsChanged);
        }

        private void Start()
        {
            if (playerHealth != null)
            {
                OnHpChanged(playerHealth.CurrentHp, playerHealth.MaxHp);
                OnManaChanged(playerHealth.CurrentMana, playerHealth.MaxMana);
            }

            if (buffSystem != null)
                OnBuffsChanged(buffSystem.ActiveBuffs);
            else
                OnBuffsChanged(new List<BuffSystem.BuffInstance>());
        }

        [Header("체력 낮을 때 시각 피드백 (7주차 스프린트)")]
        [Tooltip("체력 비율이 이 값 이하로 떨어지면 체력바 색이 경고색으로 바뀝니다.")]
        [SerializeField] private float lowHpThreshold = 0.3f;
        [SerializeField] private Color normalHpColor = new Color(0.9f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color lowHpColor = new Color(1f, 0.1f, 0.1f, 1f);

        private void OnHpChanged(int current, int max)
        {
            if (hpSlider != null)
            {
                hpSlider.maxValue = max;
                hpSlider.value = current;

                if (hpSlider.fillRect != null && hpSlider.fillRect.TryGetComponent(out Image fillImage))
                {
                    bool isLow = max > 0 && (float)current / max <= lowHpThreshold;
                    fillImage.color = isLow ? lowHpColor : normalHpColor;
                }
            }

            if (hpText != null)
                hpText.text = $"HP {current} / {max}";
        }

        private void OnManaChanged(float current, float max)
        {
            if (manaSlider != null)
            {
                manaSlider.maxValue = max;
                manaSlider.value = current;
            }

            if (manaText != null)
                manaText.text = $"Mana {current:0} / {max:0}";
        }

        private void OnBuffsChanged(IReadOnlyList<BuffSystem.BuffInstance> buffs)
        {
            if (buffListText == null) return;

            if (buffs.Count == 0)
            {
                buffListText.text = "활성 버프 없음";
                return;
            }

            StringBuilder sb = new StringBuilder("활성 버프:\n");
            foreach (BuffSystem.BuffInstance buff in buffs)
            {
                string stackSuffix = buff.StackCount > 1 ? $" x{buff.StackCount}" : "";
                sb.AppendLine($"- {buff.Data.DisplayName}{stackSuffix} ({buff.RemainingDuration:0.0}s)");
            }

            buffListText.text = sb.ToString();
        }
    }
}
