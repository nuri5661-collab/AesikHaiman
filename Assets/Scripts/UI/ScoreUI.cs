using UnityEngine;
using UnityEngine.UI;
using AesikHaiman.Game;

namespace AesikHaiman.UI
{
    /// <summary>
    /// 점수 텍스트를 표시. GameManager.onScoreChanged 이벤트가 올 때만 갱신하고, Update에서 매 프레임
    /// 값을 다시 쓰지 않습니다 (6주차 체크리스트: "UI가 점수가 바뀔 때만 갱신되는가?").
    /// </summary>
    public class ScoreUI : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Text scoreText;

        private void OnEnable()
        {
            if (gameManager != null)
                gameManager.onScoreChanged.AddListener(OnScoreChanged);
        }

        private void OnDisable()
        {
            if (gameManager != null)
                gameManager.onScoreChanged.RemoveListener(OnScoreChanged);
        }

        private void Start()
        {
            if (gameManager != null)
                OnScoreChanged(gameManager.Score);
        }

        private void OnScoreChanged(int score)
        {
            if (scoreText != null)
                scoreText.text = $"Score: {score}";
        }
    }
}
