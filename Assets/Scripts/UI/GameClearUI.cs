using UnityEngine;
using UnityEngine.UI;
using AesikHaiman.Game;

namespace AesikHaiman.UI
{
    /// <summary>
    /// 보스를 처치하면 뜨는 게임 클리어 패널. 생존 시간/최종 점수/랭크를 보여주고,
    /// 재시작 및 메인 메뉴 버튼을 제공합니다.
    /// 지금은 타이틀/로비 씬이 따로 없는 프로토타입 단계라 "메인 메뉴로" 버튼도 일단 재시작과 동일하게
    /// 현재 씬을 다시 로드합니다 - 타이틀 씬이 생기면 OnMainMenu()만 SceneManager.LoadScene("Title")로 바꾸면 됨.
    /// ===== 10주차 스프린트: 게임 클리어 시퀀스 =====
    /// </summary>
    public class GameClearUI : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private GameObject panel;
        [SerializeField] private Text resultText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;

        private void Awake()
        {
            if (panel != null)
                panel.SetActive(false);

            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryClicked);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        private void OnEnable()
        {
            if (gameManager != null)
                gameManager.onGameClear.AddListener(ShowClear);
        }

        private void OnDisable()
        {
            if (gameManager != null)
                gameManager.onGameClear.RemoveListener(ShowClear);
        }

        private void ShowClear()
        {
            if (panel != null)
                panel.SetActive(true);

            if (resultText != null && gameManager != null)
            {
                resultText.text = $"GAME CLEAR!\n생존 시간: {gameManager.ElapsedTime:0.0}초\n" +
                                   $"최종 점수: {gameManager.Score}\n랭크: {gameManager.GetClearRank()}";
            }
        }

        private void OnRetryClicked()
        {
            gameManager?.RestartGame();
        }

        private void OnMainMenuClicked()
        {
            // TODO: 타이틀/로비 씬이 생기면 여기만 SceneManager.LoadScene("Title") 등으로 교체
            gameManager?.RestartGame();
        }
    }
}
