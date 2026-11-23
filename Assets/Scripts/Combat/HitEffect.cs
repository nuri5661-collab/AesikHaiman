using UnityEngine;

namespace AesikHaiman.Combat
{
    /// <summary>
    /// 적중 이펙트용 플레이스홀더. 파티클을 재생하고 lifetime 뒤 스스로 사라집니다.
    /// ===== 9주차 스프린트: 공격 적중 시 파티클 생성 =====
    /// </summary>
    public class HitEffect : MonoBehaviour
    {
        [SerializeField] private float lifetime = 0.5f;

        private void Start()
        {
            Destroy(gameObject, lifetime);
        }
    }
}
