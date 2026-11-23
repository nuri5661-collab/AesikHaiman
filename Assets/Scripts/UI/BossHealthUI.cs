using UnityEngine;
using UnityEngine.UI;

namespace AesikHaiman.UI
{
    /// <summary>
    /// 화면 상단의 전용 대형 보스 체력 바. 보스가 없을 땐 꺼져있다가 BossSpawner가 소환할 때 켭니다.
    /// ===== 10주차 스프린트: 보스 체력 바 연동 =====
    /// </summary>
    public class BossHealthUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Slider hpSlider;
        [SerializeField] private Text hpText;

        private void Awake()
        {
            Hide();
        }

        public void Show()
        {
            if (root != null) root.SetActive(true);
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        public void SetHp(float current, float max)
        {
            if (hpSlider != null)
            {
                hpSlider.maxValue = max;
                hpSlider.value = current;
            }

            if (hpText != null)
                hpText.text = $"BOSS  {current:0} / {max:0}";
        }
    }
}
