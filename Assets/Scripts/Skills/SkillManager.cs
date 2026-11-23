using System;
using System.Collections.Generic;
using UnityEngine;
using AesikHaiman.Systems;
using AesikHaiman.Player;

namespace AesikHaiman.Skills
{
    /// <summary>
    /// 액티브/강인/강렬/초월/패시브 5개 슬롯과 입력 키를 관리하고,
    /// 입력이 들어오면 해당 슬롯에 장착된 스킬을 실행합니다.
    /// ===== 스킬 15종 추가 시: 인스펙터에서 slotBindings의 equippedSkill에 새 스킬 컴포넌트를 연결하거나,
    ///        전직/스킬트리 UI에서 EquipSkill()을 호출하면 됩니다. =====
    /// </summary>
    [RequireComponent(typeof(RainGaugeSystem), typeof(BuffSystem), typeof(HealSystem))]
    public class SkillManager : MonoBehaviour
    {
        [Serializable]
        public class SlotBinding
        {
            public SkillSlot slot;
            public KeyCode inputKey = KeyCode.Q;
            [Tooltip("이 슬롯에 현재 장착된 스킬. 비어 있으면 아직 배우지 않은 스킬입니다.")]
            public SkillBase equippedSkill;
        }

        [Header("슬롯 - 입력 키 바인딩 (QWER + 패시브)")]
        [SerializeField]
        private List<SlotBinding> slotBindings = new List<SlotBinding>
        {
            new SlotBinding { slot = SkillSlot.Active,    inputKey = KeyCode.Q },
            new SlotBinding { slot = SkillSlot.Robust,    inputKey = KeyCode.W },
            new SlotBinding { slot = SkillSlot.Intense,   inputKey = KeyCode.E },
            new SlotBinding { slot = SkillSlot.Transcend, inputKey = KeyCode.R },
            new SlotBinding { slot = SkillSlot.Passive,   inputKey = KeyCode.None },
        };

        [Serializable]
        public class JobLineSkillSet
        {
            public JobLine jobLine;
            [Tooltip("이 전직 라인에서 5개 슬롯에 장착될 스킬들. 각 스킬의 Slot 값을 기준으로 자동으로 해당 슬롯에 들어갑니다.")]
            public List<SkillBase> skills = new List<SkillBase>();
        }

        [Header("전직 라인별 스킬셋")]
        [Tooltip("SetJobLine() 호출 시 여기서 해당 전직 라인의 스킬들을 찾아 slotBindings에 자동으로 장착합니다.")]
        [SerializeField] private List<JobLineSkillSet> jobLineSkillSets = new List<JobLineSkillSet>();
        [SerializeField] private JobLine currentJobLine = JobLine.Base;

        public JobLine CurrentJobLine => currentJobLine;

        private RainGaugeSystem _gaugeSystem;
        private BuffSystem _buffSystem;
        private HealSystem _healSystem;
        private PlayerController _playerController;

        private void Awake()
        {
            _gaugeSystem = GetComponent<RainGaugeSystem>();
            _buffSystem = GetComponent<BuffSystem>();
            _healSystem = GetComponent<HealSystem>();
            _playerController = GetComponent<PlayerController>();

            foreach (var binding in slotBindings)
                binding.equippedSkill?.Initialize(_playerController, _gaugeSystem, _buffSystem, _healSystem);

            // 인스펙터에 지정된 전직 라인을 시작 시점에 한 번 적용 (해당 라인의 스킬셋이 등록돼 있으면 자동 장착됨)
            SetJobLine(currentJobLine);
        }

        private void Update()
        {
            foreach (var binding in slotBindings)
            {
                if (binding.equippedSkill == null) continue;
                if (binding.slot == SkillSlot.Passive) continue; // 패시브는 별도 입력 없이 스킬 내부에서 상시 처리

                if (binding.inputKey != KeyCode.None && Input.GetKeyDown(binding.inputKey))
                    binding.equippedSkill.TryActivate();
            }
        }

        /// <summary>전직/스킬 습득 UI 등에서 특정 슬롯에 스킬을 장착할 때 호출. skill이 null이면 슬롯을 비웁니다.</summary>
        public void EquipSkill(SkillSlot slot, SkillBase skill)
        {
            var binding = slotBindings.Find(b => b.slot == slot);
            if (binding == null) return;

            binding.equippedSkill = skill;
            skill?.Initialize(_playerController, _gaugeSystem, _buffSystem, _healSystem);
        }

        public SkillBase GetEquippedSkill(SkillSlot slot)
        {
            return slotBindings.Find(b => b.slot == slot)?.equippedSkill;
        }

        /// <summary>
        /// 전직 라인을 전환하고, 해당 라인에 등록된 스킬들을 각자의 Slot에 맞춰 자동으로 장착합니다.
        /// 새 라인의 스킬셋에 포함되지 않은 슬롯은 비워서, 이전 전직 라인의 스킬이 남아있지 않도록 합니다.
        /// (예: 스파 가디언 → 본캐로 되돌리면 강인/강렬/초월/패시브에 남아있던 스파 가디언 스킬이 자동으로 해제됨)
        /// ===== 스파 가디언으로 전직시키려면 SetJobLine(JobLine.SpaGuardian) 한 번만 호출하면 됨 =====
        /// </summary>
        public void SetJobLine(JobLine newJobLine)
        {
            currentJobLine = newJobLine;

            JobLineSkillSet set = jobLineSkillSets.Find(s => s.jobLine == newJobLine);

            foreach (SkillSlot slotType in (SkillSlot[])System.Enum.GetValues(typeof(SkillSlot)))
            {
                bool coveredBySet = set != null && set.skills.Exists(s => s != null && s.Slot == slotType);
                if (!coveredBySet)
                    EquipSkill(slotType, null);
            }

            if (set == null) return;

            foreach (SkillBase skill in set.skills)
            {
                if (skill == null) continue;
                EquipSkill(skill.Slot, skill);
            }
        }
    }
}
