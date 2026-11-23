using UnityEngine;
using UnityEngine.UI;
using AesikHaiman.Systems;

namespace AesikHaiman.UI
{
    /// <summary>
    /// 디버그용 강수량 게이지 UI. 실제 UI 아트가 붙기 전까지 슬라이더 + 텍스트로 대체합니다.
    /// ===== 아트 붙일 때: 이 스크립트는 그대로 두고 Slider/Text만 실제 UI 프리팹으로 교체하면 됨 =====
    /// </summary>
    public class RainGaugeUI : MonoBehaviour
    {
        [SerializeField] private RainGaugeSystem gaugeSystem;
        [SerializeField] private Slider gaugeSlider;
        [SerializeField] private Text gaugeText;

        private void OnEnable()
        {
            if (gaugeSystem != null)
                gaugeSystem.onGaugeChanged.AddListener(OnGaugeChanged);
        }

        private void OnDisable()
        {
            if (gaugeSystem != null)
                gaugeSystem.onGaugeChanged.RemoveListener(OnGaugeChanged);
        }

        private void Start()
        {
            if (gaugeSystem != null)
                OnGaugeChanged(gaugeSystem.CurrentGauge, gaugeSystem.MaxGauge);
        }

        private void OnGaugeChanged(float current, float max)
        {
            if (gaugeSlider != null)
            {
                gaugeSlider.maxValue = max;
                gaugeSlider.value = current;
            }

            if (gaugeText != null && gaugeSystem != null)
                gaugeText.text = $"강수량 게이지 {current:0} / {max:0}  (x{gaugeSystem.GetGaugeMultiplier():0.00})";
        }
    }
}
