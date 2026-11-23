using UnityEngine;
using AesikHaiman.Skills;

namespace AesikHaiman.DebugTools
{
    /// <summary>
    /// "전직 라인이 스파 가디언일 때만 해당 스킬셋이 활성화된다"를 눈으로 확인하기 위한 임시 테스트용 스크립트.
    /// J키를 누르면 본캐 ↔ 스파 가디언 전직 라인을 토글하고, 그때마다 QWER 슬롯에 장착된 스킬이 바뀝니다.
    /// 실제 전직 UI/시스템이 만들어지면 이 스크립트는 비활성화하거나 삭제해도 됩니다.
    /// </summary>
    public class DebugJobLineTester : MonoBehaviour
    {
        [SerializeField] private SkillManager skillManager;
        [SerializeField] private KeyCode toggleJobLineKey = KeyCode.J;

        private void Update()
        {
            if (skillManager == null) return;
            if (!Input.GetKeyDown(toggleJobLineKey)) return;

            JobLine next = skillManager.CurrentJobLine == JobLine.SpaGuardian ? JobLine.Base : JobLine.SpaGuardian;
            skillManager.SetJobLine(next);

            Debug.Log($"[DebugJobLineTester] 전직 라인 전환 → {next}");
        }
    }
}
