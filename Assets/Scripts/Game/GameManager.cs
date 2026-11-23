using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using AesikHaiman.Audio;

namespace AesikHaiman.Game
{
    /// <summary>
    /// 점수/처치 수/생존 시간을 관리하고, 플레이어가 죽으면 게임오버, 보스를 처치하면 게임클리어 이벤트를 쏩니다.
    /// 맵마다(Stage1~4 씬) 하나씩 새로 생기는 씬 로컬 싱글톤이지만, 누적 점수/생존 시간 자체는
    /// GameProgress(DontDestroyOnLoad)에 위임해서 맵을 넘어가도 끊기지 않습니다.
    /// 죽었을 때는 같은 맵을 다시 로드(체크포인트 재시도), 최종 클리어 후 "다시 하기"는 Stage1부터 새로 시작합니다.
    /// ===== 6~11주차 스프린트: 몬스터 처치 점수, 피격/게임오버 루프, 게임 클리어, 맵 4개 구조 =====
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private const string Stage1SceneName = "Stage1";

        /// <summary>씬에 하나만 존재. PlayerHealth/TestDummy 등이 GameManager.Instance로 직접 참조합니다.</summary>
        public static GameManager Instance { get; private set; }

        [Header("클리어 랭크 기준 점수")]
        [SerializeField] private int rankSScore = 1000;
        [SerializeField] private int rankAScore = 700;
        [SerializeField] private int rankBScore = 500;

        [Header("이벤트 (UI 등에서 구독)")]
        public UnityEvent<int> onScoreChanged;
        public UnityEvent onGameOver;
        public UnityEvent onGameClear;

        private GameProgress _progress;

        public int Score => _progress.CumulativeScore;
        public int KillCount { get; private set; }
        public float ElapsedTime => _progress.CumulativeElapsedTime;
        public bool IsGameOver { get; private set; }
        public bool IsGameClear { get; private set; }

        private void Awake()
        {
            // 씬 재시작(LoadScene) 시 이전 GameManager는 씬과 함께 사라지고 새로 하나만 생기므로
            // DontDestroyOnLoad 없이 단순 싱글턴으로 충분함. 누적 점수/시간만 GameProgress가 맡음.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _progress = GameProgress.GetOrCreate();
        }

        /// <summary>몬스터 처치 시 점수 증가. TestDummy/BossMonster.TakeDamage()에서 호출.</summary>
        public void AddScore(int amount)
        {
            if (IsGameOver || IsGameClear || amount <= 0) return;
            _progress.AddScore(amount);
            onScoreChanged?.Invoke(_progress.CumulativeScore);
        }

        /// <summary>몬스터 처치 수 증가. 몬스터 이동속도 난이도 보정에 사용.</summary>
        public void RegisterKill()
        {
            if (IsGameOver || IsGameClear) return;
            KillCount++;
        }

        /// <summary>플레이어 체력이 0이 되면 PlayerHealth가 호출. 게임을 멈추고 게임오버 UI를 띄웁니다.</summary>
        public void TriggerGameOver()
        {
            if (IsGameOver || IsGameClear) return;
            IsGameOver = true;
            Time.timeScale = 0f; // 몬스터 이동(Update)/코루틴이 자동으로 함께 멈춤
            AudioManager.Instance?.PlayGameOverSfx();
            onGameOver?.Invoke();
        }

        /// <summary>최종 보스(Stage4)를 처치하면 BossSpawner가 호출. 게임을 멈추고 게임클리어 UI를 띄웁니다.</summary>
        public void TriggerGameClear()
        {
            if (IsGameOver || IsGameClear) return;
            IsGameClear = true;
            Time.timeScale = 0f;
            onGameClear?.Invoke();
        }

        /// <summary>최종 점수 기준 간단한 클리어 랭크. S/A/B/C 중 하나.</summary>
        public string GetClearRank()
        {
            int score = _progress.CumulativeScore;
            if (score >= rankSScore) return "S";
            if (score >= rankAScore) return "A";
            if (score >= rankBScore) return "B";
            return "C";
        }

        /// <summary>
        /// 재시작 버튼에서 호출.
        /// - 게임오버(죽음) 후: 지금 있던 맵을 그대로 다시 로드 (체크포인트 재도전, 누적 점수/시간 유지)
        /// - 게임클리어(최종 보스 처치) 후: 누적 점수/시간을 초기화하고 Stage1부터 새로 시작
        /// </summary>
        public void RestartGame()
        {
            Time.timeScale = 1f;

            if (IsGameClear)
            {
                _progress.ResetRun();
                SceneManager.LoadScene(Stage1SceneName);
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
}
