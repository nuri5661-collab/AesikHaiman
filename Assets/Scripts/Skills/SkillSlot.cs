namespace AesikHaiman.Skills
{
    /// <summary>
    /// 스킬이 배치되는 슬롯 종류 (엘소드 스타일 5개 슬롯).
    /// 스킬 15종(본캐 5 + 1차 5 + 2차 4 + 3차 1)은 전직 단계에 따라
    /// 이 슬롯들 중 하나에 장착됩니다.
    /// ===== 슬롯 체계 자체를 바꿀 일은 거의 없지만, 필요하면 여기만 수정하면 전체에 반영됨 =====
    /// </summary>
    public enum SkillSlot
    {
        Active,     // 액티브
        Robust,     // 강인
        Intense,    // 강렬
        Transcend,  // 초월
        Passive     // 패시브 (입력 없이 상시 적용)
    }
}
