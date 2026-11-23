using System.Collections;
using UnityEngine;

namespace AesikHaiman.UI
{
    /// <summary>
    /// 보스 등장 직전 "WARNING!" 문구를 깜빡이는 연출. 평소엔 꺼져있습니다.
    /// ===== 10주차 스프린트: 보스전 전용 연출 =====
    /// </summary>
    public class BossWarningUI : MonoBehaviour
    {
        [SerializeField] private GameObject warningText;
        [SerializeField] private float blinkInterval = 0.2f;

        public void ShowWarning(float duration)
        {
            StartCoroutine(BlinkRoutine(duration));
        }

        private IEnumerator BlinkRoutine(float duration)
        {
            if (warningText == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                warningText.SetActive(!warningText.activeSelf);
                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;
            }

            warningText.SetActive(false);
        }
    }
}
