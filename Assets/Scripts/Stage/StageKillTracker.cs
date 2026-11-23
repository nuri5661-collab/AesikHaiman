using System.Collections.Generic;
using UnityEngine;

namespace AesikHaiman.Stage
{
    /// <summary>
    /// 이 맵에 스폰된 몹 목록을 들고 있다가, 전부 죽으면(=리스트가 전부 null이 되면) 문을 열거나
    /// 포탈을 오픈합니다. 몹 목록은 ProjectSetupTool이 씬을 생성할 때 미리 채워 넣습니다.
    /// ===== 맵 4개 구조: "몹 다 잡으면 ~" 조건을 공용으로 처리하는 감시자 =====
    /// </summary>
    public class StageKillTracker : MonoBehaviour
    {
        [SerializeField] private List<GameObject> enemies = new List<GameObject>();
        [SerializeField] private StageDoor doorToUnlock;
        [SerializeField] private StagePortal portalToOpen;

        private bool _fired;

        private void Update()
        {
            if (_fired || enemies.Count == 0) return;

            enemies.RemoveAll(e => e == null);
            if (enemies.Count > 0) return;

            _fired = true;
            doorToUnlock?.Unlock();
            portalToOpen?.Open();
        }
    }
}
