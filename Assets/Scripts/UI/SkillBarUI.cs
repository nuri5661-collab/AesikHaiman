using UnityEngine;
using UnityEngine.UI;
using AesikHaiman.Skills;

namespace AesikHaiman.UI
{
    /// <summary>
    /// QWER 4개 슬롯의 스킬 아이콘과 쿨타임을 표시하는 스킬창.
    /// 쿨타임 중에는 Radial360 오버레이가 남은 비율만큼 덮고, 중앙에 남은 초가 숫자로 표시된다.
    /// 슬롯 순서는 SkillSlot.Active/Robust/Intense/Transcend 고정 (QWER과 1:1 대응).
    /// </summary>
    public class SkillBarUI : MonoBehaviour
    {
        private static readonly SkillSlot[] Slots = { SkillSlot.Active, SkillSlot.Robust, SkillSlot.Intense, SkillSlot.Transcend };

        [SerializeField] private SkillManager skillManager;
        [SerializeField] private Image[] cooldownOverlays = new Image[4];
        [SerializeField] private Text[] cooldownTexts = new Text[4];
        [SerializeField] private Text[] skillNameTexts = new Text[4];
        [SerializeField] private Image[] iconImages = new Image[4];

        private void Update()
        {
            if (skillManager == null) return;

            for (int i = 0; i < Slots.Length; i++)
            {
                SkillBase skill = skillManager.GetEquippedSkill(Slots[i]);
                bool hasSkill = skill != null;

                if (iconImages[i] != null)
                    iconImages[i].color = hasSkill ? new Color(0.25f, 0.45f, 0.75f, 1f) : new Color(0.15f, 0.15f, 0.15f, 0.6f);

                if (skillNameTexts[i] != null)
                    skillNameTexts[i].text = hasSkill ? skill.SkillName : "";

                if (!hasSkill)
                {
                    if (cooldownOverlays[i] != null) cooldownOverlays[i].fillAmount = 0f;
                    if (cooldownTexts[i] != null) cooldownTexts[i].text = "";
                    continue;
                }

                bool onCooldown = skill.IsOnCooldown;
                float ratio = skill.Cooldown > 0f ? Mathf.Clamp01(skill.CooldownRemaining / skill.Cooldown) : 0f;

                if (cooldownOverlays[i] != null)
                    cooldownOverlays[i].fillAmount = onCooldown ? ratio : 0f;

                if (cooldownTexts[i] != null)
                    cooldownTexts[i].text = onCooldown ? Mathf.CeilToInt(skill.CooldownRemaining).ToString() : "";
            }
        }
    }
}
