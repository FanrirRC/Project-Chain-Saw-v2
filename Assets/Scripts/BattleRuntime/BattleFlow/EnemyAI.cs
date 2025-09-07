using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public struct Intent
    {
        public Actions.IntentType Type;
        public Data.SkillDefinition Skill;
        public Data.ItemDefinition Item;
        public List<CharacterScript> Targets;

        public bool TargetsAllies;
        public UI.TargetMode TargetMode;

        public bool NeedsTarget =>
            Type != Actions.IntentType.Guard &&
            (Skill == null || !Skill.TargetsSelfOnly);
    }

    public Intent Decide(CharacterScript self, IReadOnlyList<CharacterScript> players, IReadOnlyList<CharacterScript> enemies)
    {
        var skillsInv = self.GetComponent<SkillsInventory>();
        if (skillsInv != null && skillsInv.skills != null)
        {
            foreach (var s in skillsInv.skills)
            {
                if (!s) continue;
                if (s.spCost <= self.currentSP)
                {
                    return new Intent
                    {
                        Type = Actions.IntentType.Skill,
                        Skill = s,
                        TargetsAllies = s.TargetsAllies,
                        TargetMode = s.targetsAll ? UI.TargetMode.All : UI.TargetMode.Single
                    };
                }
            }
        }

        return new Intent
        {
            Type = Actions.IntentType.BasicAttack,
            TargetsAllies = false,
            TargetMode = UI.TargetMode.Single
        };
    }

    public List<CharacterScript> PickTargets(Intent intent, IReadOnlyList<CharacterScript> players, IReadOnlyList<CharacterScript> enemies)
    {
        var result = new List<CharacterScript>();
        var pool = intent.TargetsAllies ? enemies : players;

        var living = pool.Where(u => u && u.currentHP > 0).ToList();
        if (living.Count == 0) return result;

        if (intent.TargetMode == UI.TargetMode.All)
        {
            result.AddRange(living);
            return result;
        }

        int i = Random.Range(0, living.Count);
        result.Add(living[i]);
        return result;
    }

    public List<CharacterScript> PickTargetsUnique(
        Intent intent,
        IReadOnlyList<CharacterScript> players,
        IReadOnlyList<CharacterScript> enemies,
        HashSet<CharacterScript> avoid)
    {
        var result = new List<CharacterScript>();
        var pool = intent.TargetsAllies ? enemies : players;

        var living = pool.Where(u => u && u.currentHP > 0).ToList();
        if (living.Count == 0) return result;

        if (intent.TargetMode == UI.TargetMode.All)
        {
            result.AddRange(living);
            return result;
        }

        var candidates = (avoid != null && avoid.Count > 0)
            ? living.Where(u => !avoid.Contains(u)).ToList()
            : living;

        if (candidates.Count == 0) candidates = living;

        int i = Random.Range(0, candidates.Count);
        result.Add(candidates[i]);
        return result;
    }
}
