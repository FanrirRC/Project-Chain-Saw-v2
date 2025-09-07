using UnityEngine;

namespace Data
{
    [CreateAssetMenu(menuName = "RPG/Stats/Enemy Base Stats")]
    public class EnemyBaseStats : ScriptableObject
    {
        [Header("Enemy Info")]
        public string displayName;

        [Header("Stats Info")]
        public int maxHP = 80;
        public int maxSP = 30;
        public int atk = 8;
        public int def = 4;
        public int agi = 8;
    }
}
