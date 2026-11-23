using UnityEngine;
using UnityEngine.SceneManagement;

namespace AesikHaiman.Stage
{
    /// <summary>
    /// 몹을 전부 잡아야 열리는 포탈. 평소엔 꺼져있다가 StageKillTracker가 Open()을 호출하면
    /// 콜라이더/비주얼이 켜지고, 그 상태에서 플레이어가 닿으면 다음 맵으로 이동합니다.
    /// ===== 맵 4개 구조: 2맵("몹 다 잡으면 포탈 오픈")에서 사용 =====
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class StagePortal : MonoBehaviour
    {
        [SerializeField] private string nextSceneName;
        [SerializeField] private Renderer visualRenderer;
        [SerializeField] private Color closedColor = new Color(0.25f, 0.25f, 0.25f, 0.4f);
        [SerializeField] private Color openColor = new Color(0.4f, 0.9f, 1f, 0.9f);

        private Collider2D _collider;
        private bool _open;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _collider.isTrigger = true;
            _collider.enabled = false;

            ApplyColor();
        }

        /// <summary>StageKillTracker가 "이 맵의 몹을 전부 잡았다"는 신호를 받으면 호출.</summary>
        public void Open()
        {
            _open = true;
            _collider.enabled = true;
            ApplyColor();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_open) return;
            if (!other.CompareTag("Player")) return;

            if (!string.IsNullOrEmpty(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
        }

        private void ApplyColor()
        {
            if (visualRenderer == null) return;
            visualRenderer.material.color = _open ? openColor : closedColor;
        }
    }
}
