namespace AesikHaiman.Enemy
{
    /// <summary>
    /// 데미지를 받을 수 있는 대상이 구현하는 인터페이스. 스킬 쪽에서는 이 인터페이스로만 적을 알면 됩니다.
    /// ===== 실제 적 AI를 붙일 때 이 인터페이스를 구현하면 기존 스킬들이 별도 수정 없이 그대로 동작함 =====
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float amount);
    }
}
