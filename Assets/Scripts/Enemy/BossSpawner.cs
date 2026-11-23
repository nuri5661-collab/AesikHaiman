using System.Collections;
using UnityEngine;
using AesikHaiman.Game;
using AesikHaiman.Audio;
using AesikHaiman.CameraControl;
using AesikHaiman.UI;

namespace AesikHaiman.Enemy
{
    /// <summary>
    /// 맵에 입장하자마자(Start) WARNING 연출(카메라 셰이크 + 경고 텍스트 + BGM 전환)을 재생한 뒤
    /// 보스를 소환합니다. 보스가 죽으면 GameManager.TriggerGameClear()를 호출합니다.
    /// 맵 4개 구조에서는 마지막 맵(Stage4)에만 배치됨 - 더 이상 점수/시간 조건으로 기다리지 않음.
    /// ===== 10~11주차 스프린트: 보스 몬스터 등장 연출 + 게임 클리어 연결 =====
    /// </summary>
    public class BossSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject bossPrefab;
        [Tooltip("비워두면 Player 태그를 가진 오브젝트를 자동으로 찾습니다.")]
        [SerializeField] private Transform player;
        [SerializeField] private float spawnOffsetX = 6f;
        [SerializeField] private float spawnY = 0.5f;
        [SerializeField] private float warningDuration = 2f;
        [SerializeField] private float shakeMagnitude = 0.2f;

        [SerializeField] private CameraFollow2D cameraFollow;
        [SerializeField] private BossWarningUI warningUI;
        [SerializeField] private BossHealthUI bossHealthUI;

        private void Start()
        {
            if (player == null)
            {
                GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null)
                    player = playerGO.transform;
            }

            BeginBossSequence();
        }

        private void BeginBossSequence()
        {
            StartCoroutine(BossIntroRoutine());
        }

        private IEnumerator BossIntroRoutine()
        {
            warningUI?.ShowWarning(warningDuration);
            cameraFollow?.Shake(warningDuration, shakeMagnitude);
            AudioManager.Instance?.PlayBossWarningSfx();
            AudioManager.Instance?.PlayBossBgm();

            yield return new WaitForSeconds(warningDuration);

            SpawnBoss();
        }

        private void SpawnBoss()
        {
            if (bossPrefab == null || player == null) return;

            Vector3 pos = new Vector3(player.position.x + spawnOffsetX, spawnY, 0f);
            GameObject bossGO = Instantiate(bossPrefab, pos, Quaternion.identity);

            if (!bossGO.TryGetComponent(out BossMonster boss)) return;

            if (bossHealthUI != null)
            {
                boss.onHpChanged.AddListener(bossHealthUI.SetHp);
                bossHealthUI.Show();
            }

            boss.onBossDefeated.AddListener(OnBossDefeated);
        }

        private void OnBossDefeated()
        {
            bossHealthUI?.Hide();
            AudioManager.Instance?.PlayVictorySfx();
            GameManager.Instance?.TriggerGameClear();
        }
    }
}
