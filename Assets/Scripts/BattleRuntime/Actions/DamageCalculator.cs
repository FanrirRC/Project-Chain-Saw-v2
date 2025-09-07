using UnityEngine;

namespace Actions
{
    public static class DamageCalculator
    {
        public static int Physical(Data.SkillDefinition maybeSkill, CharacterScript attacker, CharacterScript defender, int basePower = 0, bool overrideWithBasePower = false)
        {
            int atkStat = overrideWithBasePower
                ? ApplyAtkMods(attacker, basePower)
                : GetEffectiveATK(attacker);
            int defStat = GetEffectiveDEF(defender);
            int raw = Mathf.Max(0, atkStat - defStat);

            float dealtMul = 1f + (SumDamageDealtPercent(attacker) / 100f);
            float takenMul = 1f + (SumDamageTakenPercent(defender) / 100f);

            int final = Mathf.RoundToInt(raw * dealtMul * takenMul);
            return Mathf.Max(0, final);
        }

        public static int HealAmount(int targetMax, int amount, bool isPercent)
            => isPercent ? Mathf.RoundToInt(targetMax * (amount / 100f)) : amount;

        public static int DotPercent(CharacterScript defender, int percentOfMaxHP)
        {
            if (defender == null || percentOfMaxHP <= 0) return 0;
            int raw = Mathf.RoundToInt(defender.maxHP * (percentOfMaxHP / 100f));
            return Mathf.Max(1, raw);
        }

        public static int GetEffectiveATK(CharacterScript c)
        {
            int baseAtk = c.GetBaseATK();
            return ApplyAtkMods(c, baseAtk);
        }

        public static int GetEffectiveDEF(CharacterScript c)
        {
            int baseDef = c.GetBaseDEF();
            return ApplyDefMods(c, baseDef);
        }

        private static int ApplyAtkMods(CharacterScript c, int baseValue)
        {
            int flat = 0; int pct = 0;
            foreach (var s in c.activeStatusEffects)
            {
                if (s.effect == null) continue;
                flat += s.effect.atkFlat;
                pct += s.effect.atkPercent;
            }
            float mul = 1f + (pct / 100f);
            return Mathf.Max(0, Mathf.RoundToInt((baseValue + flat) * mul));
        }

        private static int ApplyDefMods(CharacterScript c, int baseValue)
        {
            int flat = 0; int pct = 0;
            foreach (var s in c.activeStatusEffects)
            {
                if (s.effect == null) continue;
                flat += s.effect.defFlat;
                pct += s.effect.defPercent;
            }
            float mul = 1f + (pct / 100f);
            return Mathf.Max(0, Mathf.RoundToInt((baseValue + flat) * mul));
        }

        private static int SumDamageDealtPercent(CharacterScript c)
        {
            int pct = 0;
            foreach (var s in c.activeStatusEffects)
            {
                if (s.effect == null) continue;
                pct += s.effect.damageDealtPercent;
            }
            return pct;
        }

        private static int SumDamageTakenPercent(CharacterScript c)
        {
            int pct = 0;
            foreach (var s in c.activeStatusEffects)
            {
                if (s.effect == null) continue;
                pct += s.effect.damageTakenPercent;
            }
            return pct;
        }
    }
}
