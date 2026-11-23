using UnityEngine;

namespace AesikHaiman.Skills.Examples
{
    /// <summary>
    /// 스킬 15종을 어떻게 구현하면 되는지 보여주는 예시 스킬입니다.
    ///
    /// ===== 새 스킬 추가 절차 =====
    /// 1. 이 파일을 복사해서 Assets/Scripts/Skills/ 아래에 새 이름(예: Skill_HailStorm.cs)으로 저장
    /// 2. 클래스명을 바꾸고 SkillBase를 상속
    /// 3. 인스펙터에서 스킬 이름/슬롯/쿨타임/게이지 소모량·축적량을 설정
    /// 4. Execute()에 실제 로직(데미지, 이동, 버프 등) 작성
    /// 5. PlayEffect()에 VFX/SFX 재생 코드 작성 (지금은 비워둠)
    /// 6. 플레이어 오브젝트에 컴포넌트로 추가하고, SkillManager의 slotBindings에서 원하는 슬롯에 연결
    /// </summary>
    public class Skill_WaterSplash_Example : SkillBase
    {
        [Header("워터 스플래시 전용 수치 (예시)")]
        [SerializeField] private float baseDamage = 20f;
        [SerializeField] private float radius = 2f;

        protected override void Execute(float gaugeMultiplier)
        {
            float finalDamage = baseDamage * gaugeMultiplier;

            // TODO: 실제 판정(Physics2D.OverlapCircle 등)과 데미지 적용은 히트박스 시스템 완성 후 연결
            Debug.Log($"[{SkillName}] 발동! 데미지 {finalDamage:0.0} (게이지 배율 x{gaugeMultiplier:0.00}), 범위 {radius}");
        }

        protected override void PlayEffect()
        {
            // TODO: 물보라 VFX 프리팹 인스턴스화, 사운드 재생 등을 여기에 작성
        }
    }
}
