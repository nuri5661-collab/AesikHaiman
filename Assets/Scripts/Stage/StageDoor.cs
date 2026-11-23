using UnityEngine;
using UnityEngine.SceneManagement;
using AesikHaiman.Enemy;
using AesikHaiman.Audio;

namespace AesikHaiman.Stage
{
    /// <summary>
    /// 때려 부수면 다음 맵으로 이동하는 문. IDamageable을 구현해 기존 AttackHitbox/Projectile이
    /// 그대로 데미지를 줄 수 있습니다. startsLocked가 켜져 있으면 StageKillTracker.Unlock()이
    /// 호출되기 전까지는 때려도 반응하지 않습니다 (예: "몹을 다 잡아야 부술 수 있는 문").
    /// ===== 맵 4개 구조: 1맵(무조건 부수기)/3맵(몹 전멸 후 부수기)에서 재사용 =====
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class StageDoor : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHp = 30f;
        [SerializeField] private bool startsLocked;
        [SerializeField] private string nextSceneName;
        [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f);
        [SerializeField] private Color unlockedColor = new Color(0.55f, 0.35f, 0.15f);

        private float _hp;
        private bool _locked;
        private Renderer _renderer;

        private void Awake()
        {
            _hp = maxHp;
            _locked = startsLocked;
            _renderer = GetComponent<Renderer>();
            ApplyColor();
        }

        /// <summary>StageKillTracker가 "이 맵의 몹을 전부 잡았다"는 신호를 받으면 호출.</summary>
        public void Unlock()
        {
            _locked = false;
            ApplyColor();
        }

        public void TakeDamage(float amount)
        {
            if (_locked) return;

            _hp -= amount;
            AudioManager.Instance?.PlayHitSfx();

            if (_hp <= 0f)
                Break();
        }

        private void Break()
        {
            Destroy(gameObject);
            if (!string.IsNullOrEmpty(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
        }

        private void ApplyColor()
        {
            if (_renderer == null) return;
            _renderer.material.color = _locked ? lockedColor : unlockedColor;
        }
    }
}
