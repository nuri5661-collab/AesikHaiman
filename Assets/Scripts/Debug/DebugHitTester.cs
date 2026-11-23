using UnityEngine;
using AesikHaiman.Player;

namespace AesikHaiman.DebugTools
{
    /// <summary>
    /// 아직 적(Enemy)이 없는 프로토타입 단계에서 피격/넉백/무적시간을 눈으로 확인하기 위한 임시 테스트용 스크립트.
    /// H키를 누르면 플레이어가 데미지를 입고 반대 방향으로 넉백됩니다.
    /// 적 AI가 추가되면 이 스크립트는 비활성화하거나 삭제해도 됩니다.
    /// </summary>
    public class DebugHitTester : MonoBehaviour
    {
        [SerializeField] private PlayerHealth targetHealth;
        [SerializeField] private KeyCode testHitKey = KeyCode.H;
        [SerializeField] private int testDamage = 10;
        [SerializeField] private float testKnockbackForce = 8f;

        private void Update()
        {
            if (targetHealth == null) return;
            if (!Input.GetKeyDown(testHitKey)) return;

            Vector2 knockbackDir = new Vector2(-Mathf.Sign(targetHealth.transform.localScale.x), 0.5f);
            targetHealth.TakeDamage(testDamage, knockbackDir, testKnockbackForce);
        }
    }
}
