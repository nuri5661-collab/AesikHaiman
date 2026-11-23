using UnityEngine;
using UnityEngine.UI;
using AesikHaiman.Game;

namespace AesikHaiman.UI
{
    /// <summary>
    /// 게임오버 패널. 평소엔 꺼져있다가 GameManager.onGameOver가 오면 켜지고 최종 점수를 보여줍니다.
    /// 재시작 버튼은 GameManager.RestartGame()(씬 재로드)로 연결됩니다.
    /// ===== 7주차 스프린트: 게임 오버 + 재시작 루프 =====
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private GameObject panel;
        [SerializeField] private Text finalScoreText;
        [SerializeField] private Button retryButton;

        private void Awake()
        {
            if (panel != null)
                panel.SetActive(false);

            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryClicked);
        }

        private void OnEnable()
        {
            if (gameManager != null)
                gameManager.onGameOver.AddListener(ShowGameOver);
        }

        private void OnDisable()
        {
            if (gameManager != null)
                gameManager.onGameOver.RemoveListener(ShowGameOver);
        }

        private void ShowGameOver()
        {
            if (panel != null)
                panel.SetActive(true);

            if (finalScoreText != null && gameManager != null)
                finalScoreText.text = $"최종 점수: {gameManager.Score}";
        }

        private void OnRetryClicked()
        {
            gameManager?.RestartGame();
        }
    }
}
