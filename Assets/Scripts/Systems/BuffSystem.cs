using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AesikHaiman.Systems
{
    /// <summary>
    /// 스탯 버프/디버프(수치 + 지속시간)를 관리하는 시스템.
    /// 같은 buffId의 버프가 다시 적용되면 BuffData.StackPolicy(갱신/중첩/무시)에 따라 처리됩니다.
    /// ===== 스파 가디언 버프 스킬 구현 시: skill의 Execute()에서 BuffSystem.ApplyBuff(buffData)만 호출하면 됨 =====
    /// </summary>
    public class BuffSystem : MonoBehaviour
    {
        /// <summary>런타임에 활성화되어 있는 버프 1건.</summary>
        public class BuffInstance
        {
            public BuffData Data { get; }
            public float RemainingDuration { get; set; }
            public int StackCount { get; set; }

            public BuffInstance(BuffData data)
            {
                Data = data;
                RemainingDuration = data.Duration;
                StackCount = 1;
            }
        }

        [Header("이벤트 (UI 등에서 구독)")]
        public UnityEvent<IReadOnlyList<BuffInstance>> onBuffsChanged;

        private readonly List<BuffInstance> _activeBuffs = new List<BuffInstance>();

        public IReadOnlyList<BuffInstance> ActiveBuffs => _activeBuffs;

        private void Update()
        {
            if (_activeBuffs.Count == 0) return;

            bool changed = false;
            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                _activeBuffs[i].RemainingDuration -= Time.deltaTime;
                if (_activeBuffs[i].RemainingDuration <= 0f)
                {
                    _activeBuffs.RemoveAt(i);
                    changed = true;
                }
            }

            if (changed)
                onBuffsChanged?.Invoke(_activeBuffs);
        }

        /// <summary>버프를 적용합니다. 이미 같은 buffId가 있으면 BuffData.StackPolicy에 따라 자동으로 갱신/중첩/무시됩니다.</summary>
        public void ApplyBuff(BuffData data)
        {
            if (data == null) return;

            BuffInstance existing = _activeBuffs.Find(b => b.Data.BuffId == data.BuffId);

            if (existing == null)
            {
                _activeBuffs.Add(new BuffInstance(data));
                onBuffsChanged?.Invoke(_activeBuffs);
                return;
            }

            switch (data.StackPolicy)
            {
                case BuffStackPolicy.Refresh:
                    existing.RemainingDuration = data.Duration;
                    break;

                case BuffStackPolicy.Stack:
                    if (data.MaxStacks <= 0 || existing.StackCount < data.MaxStacks)
                        existing.StackCount++;
                    existing.RemainingDuration = data.Duration; // 중첩 시에도 지속시간은 새로 갱신
                    break;

                case BuffStackPolicy.Ignore:
                    return; // 이미 적용 중이면 아무것도 하지 않음
            }

            onBuffsChanged?.Invoke(_activeBuffs);
        }

        /// <summary>buffId로 즉시 해제.</summary>
        public void RemoveBuff(string buffId)
        {
            int removed = _activeBuffs.RemoveAll(b => b.Data.BuffId == buffId);
            if (removed > 0)
                onBuffsChanged?.Invoke(_activeBuffs);
        }

        public void ClearAllBuffs()
        {
            if (_activeBuffs.Count == 0) return;
            _activeBuffs.Clear();
            onBuffsChanged?.Invoke(_activeBuffs);
        }

        public bool HasBuff(string buffId) => _activeBuffs.Exists(b => b.Data.BuffId == buffId);

        /// <summary>
        /// 특정 스탯의 최종 배율을 반환합니다 (PercentAdd 버프를 모두 합산, 중첩 수만큼 반영).
        /// 사용 예: PlayerController에서 moveSpeed * buffSystem.GetStatMultiplier(StatType.MoveSpeed)
        /// </summary>
        public float GetStatMultiplier(StatType statType)
        {
            float percentSum = 0f;
            foreach (BuffInstance buff in _activeBuffs)
            {
                if (buff.Data.StatType != statType) continue;
                if (buff.Data.ModifierType != BuffModifierType.PercentAdd) continue;
                percentSum += buff.Data.Value * buff.StackCount;
            }
            return 1f + percentSum;
        }

        /// <summary>특정 스탯의 고정값 가산 합계를 반환합니다 (FlatAdd 버프 전용).</summary>
        public float GetStatFlatBonus(StatType statType)
        {
            float sum = 0f;
            foreach (BuffInstance buff in _activeBuffs)
            {
                if (buff.Data.StatType != statType) continue;
                if (buff.Data.ModifierType != BuffModifierType.FlatAdd) continue;
                sum += buff.Data.Value * buff.StackCount;
            }
            return sum;
        }
    }
}
