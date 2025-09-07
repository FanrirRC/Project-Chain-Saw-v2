using Sirenix.OdinInspector;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(menuName = "RPG/Item")]
    public class ItemDefinition : ScriptableObject
    {
        public enum EffectType { Heal, ApplyStatus }

        public string displayName;
        [TextArea] public string description;
        public Sprite icon;

        [EnumToggleButtons]
        public EffectType effectType = EffectType.Heal;

        [Header("Effect")]
        public int power = 30;
        public bool isPercent = false;
        public StatusEffectDefinition statusToApply;
    }
}
