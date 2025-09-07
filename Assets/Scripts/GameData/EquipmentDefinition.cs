using UnityEngine;

namespace Data
{
    [CreateAssetMenu(menuName = "RPG/Equipment")]
    public class EquipmentDefinition : ScriptableObject
    {
        public string displayName;
        public int hpBonus, spBonus, atkBonus, defBonus, agiBonus;
    }
}
