using System.Collections;
using UnityEngine;

namespace AesikHaiman.Player
{
    /// <summary>
    /// 슈퍼아머 상태를 관리. 슈퍼아머가 켜져있는 동안 PlayerHealth.TakeDamage()는
    /// 히트 경직(애니메이션/넉백)과 무적시간을 생략하고 데미지만 적용합니다.
    /// ===== 스파 가디언 슈퍼아머 계열 스킬(예: 온천의 공주) 구현 시 Activate()만 호출하면 됨 =====
    /// </summary>
    [DisallowMultipleComponent]
    public class SuperArmorState : MonoBehaviour
    {
        [SerializeField] private float defaultDuration = 3f;

        private Coroutine _activeRoutine;

        public bool IsActive { get; private set; }
        public float RemainingDuration { get; private set; }

        /// <summary>슈퍼아머를 켭니다. duration을 생략하면 defaultDuration이 적용됩니다.</summary>
        public void Activate(float duration = -1f)
        {
            float appliedDuration = duration > 0f ? duration : defaultDuration;

            if (_activeRoutine != null)
                StopCoroutine(_activeRoutine);

            _activeRoutine = StartCoroutine(ActivateRoutine(appliedDuration));
        }

        /// <summary>슈퍼아머를 즉시 해제합니다.</summary>
        public void Deactivate()
        {
            if (_activeRoutine != null)
                StopCoroutine(_activeRoutine);

            IsActive = false;
            RemainingDuration = 0f;
        }

        private IEnumerator ActivateRoutine(float duration)
        {
            IsActive = true;
            RemainingDuration = duration;

            while (RemainingDuration > 0f)
            {
                RemainingDuration -= Time.deltaTime;
                yield return null;
            }

            IsActive = false;
            RemainingDuration = 0f;
        }
    }
}
