using System.Collections.Generic;
using UnityEngine;
using AesikHaiman.Player;

namespace AesikHaiman.Systems
{
    /// <summary>
    /// 아군 목록을 관리. 지금은 싱글플레이어라 자기 자신만 등록되지만,
    /// "계란 먹어"/"목 마르지?"/"온천수"처럼 아군 전체에 효과를 주는 스킬이
    /// 처음부터 리스트를 순회하는 구조로 작성되도록 준비해두는 컴포넌트입니다.
    /// ===== 다인 파티가 추가되면: 파티원 합류/이탈 시 RegisterAlly()/UnregisterAlly()를 호출하면 됨 =====
    /// </summary>
    public class AllyGroup : MonoBehaviour
    {
        /// <summary>아군 한 명이 가진 시스템 묶음(체력/버프/힐/슈퍼아머).</summary>
        public class AllyRef
        {
            public PlayerHealth Health;
            public BuffSystem Buffs;
            public HealSystem Heal;
            public SuperArmorState SuperArmor;
        }

        private readonly List<AllyRef> _allies = new List<AllyRef>();

        public IReadOnlyList<AllyRef> GetAllies() => _allies;

        private void Awake()
        {
            // 지금은 싱글플레이어라 자기 자신만 아군으로 등록.
            RegisterAlly(
                GetComponent<PlayerHealth>(),
                GetComponent<BuffSystem>(),
                GetComponent<HealSystem>(),
                GetComponent<SuperArmorState>());
        }

        public void RegisterAlly(PlayerHealth health, BuffSystem buffs, HealSystem heal, SuperArmorState superArmor)
        {
            if (health == null) return;
            _allies.Add(new AllyRef { Health = health, Buffs = buffs, Heal = heal, SuperArmor = superArmor });
        }

        public void UnregisterAlly(PlayerHealth health)
        {
            _allies.RemoveAll(a => a.Health == health);
        }
    }
}
