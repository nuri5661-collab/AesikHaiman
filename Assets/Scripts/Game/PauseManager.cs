using UnityEngine;
using UnityEngine.UI;

namespace AesikHaiman.Game
{
    /// <summary>
    /// ESC로 일시정지/재개를 토글. 일시정지 중에는 Time.timeScale = 0으로 멈추고,
    /// PlayerController.Update()에서 IsGameOver/IsGameClear와 같은 방식으로 이동/공격 입력을 막습니다.
    /// (Time.timeScale만으로는 Input 폴링 자체가 멈추지 않으므로 명시적으로 막아야 함)
    /// ===== 11주차 스프린트: 일시정지 중 조작/버튼 중복 입력 방지 =====
    /// </summary>
    public class PauseManager : MonoBehaviour
    {
        public static PauseManager Instance { get; private set; }

        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;

        public bool IsPaused { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (resumeButton != null)
                resumeButton.onClick.AddListener(Resume);
        }

        private void Update()
        {
            if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.IsGameClear))
                return;

            if (Input.GetKeyDown(pauseKey))
                TogglePause();
        }

        public void TogglePause()
        {
            if (IsPaused) Resume();
            else Pause();
        }

        public void Pause()
        {
            if (IsPaused) return;
            IsPaused = true;
            Time.timeScale = 0f;
            pausePanel?.SetActive(true);
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = 1f;
            pausePanel?.SetActive(false);
        }
    }
}
