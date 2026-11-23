using UnityEngine;

namespace AesikHaiman.Game
{
    /// <summary>
    /// 맵(씬)이 4개로 나뉘면서 생긴 "씬을 넘어가도 유지돼야 하는 값"(누적 점수, 누적 생존 시간)을
    /// 전담하는 전역 싱글톤. GameManager는 각 맵 씬마다 새로 생기고 사라지지만, 이 오브젝트는
    /// DontDestroyOnLoad로 게임 전체에서 단 하나만 유지됩니다.
    /// ===== 맵 4개 구조 리팩터: Stage1~4를 넘어가도 점수/생존시간이 끊기지 않도록 분리 =====
    /// </summary>
    public class GameProgress : MonoBehaviour
    {
        private static GameProgress _instance;

        /// <summary>없으면 새로 만들어서 DontDestroyOnLoad로 등록. GameManager.Awake()에서 호출.</summary>
        public static GameProgress GetOrCreate()
        {
            if (_instance != null) return _instance;

            GameObject go = new GameObject("GameProgress");
            _instance = go.AddComponent<GameProgress>();
            DontDestroyOnLoad(go);
            return _instance;
        }

        public int CumulativeScore { get; private set; }
        public float CumulativeElapsedTime { get; private set; }

        private void Update()
        {
            CumulativeElapsedTime += Time.deltaTime;
        }

        public void AddScore(int amount)
        {
            CumulativeScore += amount;
        }

        /// <summary>게임 클리어(최종 보스 처치) 후 "다시 하기"로 전체 런을 새로 시작할 때 호출.</summary>
        public void ResetRun()
        {
            CumulativeScore = 0;
            CumulativeElapsedTime = 0f;
        }
    }
}
