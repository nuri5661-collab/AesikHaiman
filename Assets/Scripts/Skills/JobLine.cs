namespace AesikHaiman.Skills
{
    /// <summary>
    /// 캐릭터의 전직 단계. 스킬 15종(본캐 5 + 1차 5 + 2차 4 + 3차 1)은 전직 라인에 따라
    /// 같은 5개 슬롯(SkillSlot)에 서로 다른 스킬로 교체 장착됩니다.
    /// ===== 2차(테르메 퀸)/3차(셀레스티얼 에덴) 전직 추가 시 여기에 항목만 추가하면 됨 =====
    /// </summary>
    public enum JobLine
    {
        Base,        // 본캐 (전직 전)
        SpaGuardian, // 1차 전직: 스파 가디언
    }
}
