using System.Collections;
using UnityEngine;
using AesikHaiman.Game;

namespace AesikHaiman.Enemy
{
    /// <summary>
    /// 몬스터(슬라임) 프리팹을 일정 주기로 계속 소환합니다. GameManager.KillCount가 늘어날수록
    /// 스폰 간격이 점점 짧아져서 난이도가 서서히 올라갑니다.
    /// ===== 6주차 스프린트: 몬스터 무한 스폰 + 간단한 난이도 조절 =====
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("스폰 설정")]
        [SerializeField] private GameObject enemyPrefab;
        [Tooltip("비워두면 Player 태그를 가진 오브젝트를 자동으로 찾아 그 주변에 스폰합니다.")]
        [SerializeField] private Transform player;
        [SerializeField] private float minSpawnInterval = 3f;
        [SerializeField] private float maxSpawnInterval = 5f;
        [Tooltip("플레이어 좌/우로 이 거리만큼 떨어진 곳에서 랜덤하게 스폰됩니다.")]
        [SerializeField] private float spawnDistanceX = 8f;
        [SerializeField] private float spawnY = 0.5f;

        [Header("난이도 (처치 수에 비례해 스폰 간격이 짧아짐)")]
        [SerializeField] private float intervalReductionPerKill = 0.05f;
        [SerializeField] private float minInterval = 1f;

        private bool _stopped;

        private void Start()
        {
            if (player == null)
            {
                GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null)
                    player = playerGO.transform;
            }

            StartCoroutine(SpawnLoop());
        }

        public void StopSpawning()
        {
            _stopped = true;
        }

        private IEnumerator SpawnLoop()
        {
            while (!_stopped)
            {
                int killCount = GameManager.Instance != null ? GameManager.Instance.KillCount : 0;
                float reduction = killCount * intervalReductionPerKill;
                float interval = Mathf.Max(minInterval, Random.Range(minSpawnInterval, maxSpawnInterval) - reduction);

                yield return new WaitForSeconds(interval);

                if (_stopped) yield break;

                SpawnEnemy();
            }
        }

        private void SpawnEnemy()
        {
            if (enemyPrefab == null || player == null) return;

            float side = Random.value > 0.5f ? 1f : -1f;
            Vector3 position = new Vector3(player.position.x + side * spawnDistanceX, spawnY, 0f);

            Instantiate(enemyPrefab, position, Quaternion.identity);
        }
    }
}
