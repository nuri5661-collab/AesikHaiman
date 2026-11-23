using UnityEngine;

namespace AesikHaiman.CameraControl
{
    /// <summary>
    /// 플레이어를 부드럽게 따라가는 간단한 2D 카메라. 테스트 편의를 위한 최소 구현입니다.
    /// Shake()는 추적 위치 위에 흔들림을 덧씌우는 방식이라, 추적 로직과 서로 값을 덮어쓰며 싸우지 않습니다.
    /// ===== 10주차 스프린트: 보스 등장 시 카메라 셰이크 =====
    /// </summary>
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, -10f);
        [SerializeField] private float smoothTime = 0.15f;

        private Vector3 _velocity;
        private float _shakeDuration;
        private float _shakeMagnitude;

        /// <summary>duration초 동안 magnitude 세기로 화면을 흔듭니다.</summary>
        public void Shake(float duration, float magnitude)
        {
            _shakeDuration = duration;
            _shakeMagnitude = magnitude;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, smoothTime);

            if (_shakeDuration > 0f)
            {
                Vector2 shakeOffset = Random.insideUnitCircle * _shakeMagnitude;
                transform.position += (Vector3)shakeOffset;
                _shakeDuration -= Time.deltaTime;
            }
        }
    }
}
