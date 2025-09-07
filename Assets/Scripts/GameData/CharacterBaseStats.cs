using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Data
{
    [CreateAssetMenu(menuName = "RPG/Stats/Character Base Stats")]
    public class CharacterBaseStats : ScriptableObject
    {
        [Header("Character Info")]
        public string displayName;

        [Header("Stats Info")]
        public int maxHP = 100;
        public int maxSP = 50;
        public int atk = 10;
        public int def = 5;
        public int agi = 10;
    }
}
