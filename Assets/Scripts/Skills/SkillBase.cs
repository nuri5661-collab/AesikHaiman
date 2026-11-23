using UnityEngine;
using AesikHaiman.Systems;
using AesikHaiman.Player;

namespace AesikHaiman.Skills
{
    /// <summary>
    /// 모든 스킬(15종)이 상속받는 공통 베이스 클래스.
    /// 쿨타임 / 게이지 소모·축적 / 이펙트 훅을 공통으로 처리하고,
    /// 실제 스킬 로직은 하위 클래스에서 Execute()만 구현하면 됩니다.
    /// ===== 새 스킬 추가 방법: Assets/Scripts/Skills/Examples/Skill_WaterSplash_Example.cs 참고 =====
    /// </summary>
    // 주의: [DisallowMultipleComponent]를 여기 붙이면 안 됨 - 이 속성은 하위 클래스까지 상속되기 때문에
    // 플레이어 하나에 스킬 컴포넌트를 여러 개(슬롯당 1개씩, 최대 15종) 붙이는 구조 자체가 막혀버림.
    public abstract class SkillBase : MonoBehaviour
    {
        [Header("스킬 기본 정보")]
        [SerializeField] private string skillName = "New Skill";
        [SerializeField] private SkillSlot slot = SkillSlot.Active;
        [Tooltip("이 스킬이 속한 전직 라인. SkillManager.SetJobLine()이 전직 라인을 바꿀 때 이 값을 기준으로 스킬셋을 구분합니다.")]
        [SerializeField] private JobLine jobLine = JobLine.Base;

        [Header("쿨타임 / 게이지")]
        [SerializeField] private float cooldown = 1f;
        [Tooltip("스킬 사용에 필요한 강수량 게이지 소모량 (0이면 소모 없음)")]
        [SerializeField] private float gaugeCost = 0f;
        [Tooltip("스킬 사용 시 강수량 게이지가 쌓이는 양 (물 속성 스킬 등)")]
        [SerializeField] private float gaugeGainOnUse = 0f;

        private float _cooldownTimer;

        protected RainGaugeSystem GaugeSystem { get; private set; }
        protected BuffSystem BuffSystem { get; private set; }
        protected HealSystem HealSystem { get; private set; }
        protected PlayerController Owner { get; private set; }

        public string SkillName => skillName;
        public SkillSlot Slot => slot;
        public JobLine JobLine => jobLine;
        public float Cooldown => cooldown;
        public float CooldownRemaining => Mathf.Max(0f, _cooldownTimer);
        public bool IsOnCooldown => _cooldownTimer > 0f;

        /// <summary>
        /// SkillManager가 스킬을 장착할 때 호출합니다.
        /// ===== 스파 가디언 힐/버프 스킬은 Execute()에서 BuffSystem.ApplyBuff(...) / HealSystem.HealInstant(...) 등을
        ///        바로 호출하면 됨 (별도 훅 필요 없음) =====
        /// </summary>
        public virtual void Initialize(PlayerController owner, RainGaugeSystem gaugeSystem, BuffSystem buffSystem, HealSystem healSystem)
        {
            Owner = owner;
            GaugeSystem = gaugeSystem;
            BuffSystem = buffSystem;
            HealSystem = healSystem;
        }

        protected virtual void Update()
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;
        }

        /// <summary>쿨타임/게이지 조건 확인. 스킬별 추가 조건이 있다면 override해서 확장.</summary>
        public virtual bool CanUse()
        {
            if (IsOnCooldown) return false;
            if (gaugeCost > 0f && GaugeSystem != null && !GaugeSystem.HasEnoughGauge(gaugeCost))
                return false;
            return true;
        }

        /// <summary>외부(SkillManager, 콤보 시스템 등)에서 호출하는 진입점.</summary>
        public bool TryActivate()
        {
            if (!CanUse()) return false;

            if (gaugeCost > 0f && GaugeSystem != null)
                GaugeSystem.ConsumeGauge(gaugeCost);

            float multiplier = GaugeSystem != null ? GaugeSystem.GetGaugeMultiplier() : 1f;

            Execute(multiplier);

            if (gaugeGainOnUse > 0f && GaugeSystem != null)
                GaugeSystem.AddGauge(gaugeGainOnUse);

            PlayEffect();
            _cooldownTimer = cooldown;
            return true;
        }

        // ============================================================
        // 스킬 15종을 구현할 때는 아래 두 함수만 override 하면 됩니다.
        // ============================================================

        /// <summary>
        /// 실제 스킬 로직. gaugeMultiplier는 RainGaugeSystem.GetGaugeMultiplier() 값이므로,
        /// 데미지/효과에 곱해서 사용하면 게이지 연동이 자동으로 적용됩니다.
        /// </summary>
        protected abstract void Execute(float gaugeMultiplier);

        /// <summary>
        /// 이펙트/사운드/애니메이션 트리거 훅. 지금은 비워둔 상태이며,
        /// 이후 VFX 프리팹 인스턴스화, Animator.SetTrigger, AudioSource.PlayOneShot 등을 여기에 작성.
        /// </summary>
        protected virtual void PlayEffect()
        {
            // TODO: 스킬별 VFX/SFX 재생 코드는 각 스킬 클래스에서 override
        }
    }
}
