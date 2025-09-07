using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Data;

namespace Actions
{
    public enum IntentType { None, BasicAttack, Skill, Item, Guard }

    public class ActionExecutor : MonoBehaviour
    {
        [SerializeField] private UI.DamagePopup damagePopup;

        public IEnumerator Execute(CharacterScript actor, UI.CommandDecision decision, List<CharacterScript> targets)
        {
            switch (decision.Type)
            {
                case UI.CommandDecision.DecisionType.Attack:
                    yield return DoBasicAttack(actor, targets);
                    break;
                case UI.CommandDecision.DecisionType.Skill:
                    yield return DoSkill(actor, decision.Skill, targets);
                    break;
                case UI.CommandDecision.DecisionType.Item:
                    yield return DoItem(actor, decision.Item, targets);
                    break;
                case UI.CommandDecision.DecisionType.Guard:
                    yield break;
            }
        }

        public IEnumerator ExecuteIntent(CharacterScript actor, EnemyAI.Intent intent)
        {
            switch (intent.Type)
            {
                case IntentType.BasicAttack:
                    yield return DoBasicAttack(actor, intent.Targets);
                    break;
                case IntentType.Skill:
                    yield return DoSkill(actor, intent.Skill, intent.Targets);
                    break;
                case IntentType.Item:
                    yield return DoItem(actor, intent.Item, intent.Targets);
                    break;
            }
        }

        private IEnumerator DoBasicAttack(CharacterScript actor, List<CharacterScript> targets)
        {
            if (targets == null || targets.Count == 0) yield break;
            var t = targets[0];
            if (!t) yield break;

            // (Optional) face the target
            Vector3 look = t.transform.position - actor.transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f)
                actor.transform.rotation = Quaternion.LookRotation(look);

            // Play attack and wait the windup
            actor.PlayAttack();
            yield return new WaitForSeconds(actor.attackWindup);

            // Apply damage
            int dmg = DamageCalculator.Physical(null, actor, t);
            t.SetHP(t.currentHP - dmg);

            // Popup + hurt (if alive)
            if (damagePopup) damagePopup.Spawn(t.transform.position, dmg, false, false);
            if (t.currentHP > 0) t.PlayHurt();

            // Recover delay
            yield return new WaitForSeconds(actor.attackRecover);
            actor.PlayIdle();

            // SP stage 1: restore 1 SP per basic attack action
            actor.GainSP(1);
        }

        private IEnumerator DoSkill(CharacterScript actor, Data.SkillDefinition skill, List<CharacterScript> targets)
{
    if (!skill) yield break;

    // Enforce targets if needed
    if (!skill.TargetsSelfOnly && (targets == null || targets.Count == 0))
        yield break;

    // SP cost gate
    if (actor.currentSP < skill.spCost) yield break;
    actor.SetSP(actor.currentSP - skill.spCost);

    // Use attack animation as a generic "use skill" for now
    actor.PlayAttack();
    yield return new WaitForSeconds(actor.attackWindup);

    if (skill.effectType == Data.SkillDefinition.EffectType.Damage)
    {
        foreach (var t in targets)
        {
            if (!t) continue;
            int dmg = DamageCalculator.Physical(skill, actor, t, skill.power, skill.overrideWithPower);
            t.SetHP(t.currentHP - dmg);
            if (damagePopup) damagePopup.Spawn(t.transform.position, dmg, false, false);
            if (t.currentHP > 0) t.PlayHurt();
        }
    }
    else if (skill.effectType == Data.SkillDefinition.EffectType.Heal)
    {
        // If self-only heal and no targets passed, heal the caster
        if ((targets == null || targets.Count == 0) && skill.TargetsSelfOnly)
            targets = new System.Collections.Generic.List<CharacterScript> { actor };

        foreach (var t in targets)
        {
            if (!t) continue;
            int heal = DamageCalculator.HealAmount(t.maxHP, skill.power, skill.isPercent);
            t.SetHP(t.currentHP + heal);
            if (damagePopup) damagePopup.Spawn(t.transform.position, heal, false, true);
        }
    }
    else if (skill.effectType == Data.SkillDefinition.EffectType.ApplyStatus && skill.statusToApply)
    {
        if ((targets == null || targets.Count == 0) && skill.TargetsSelfOnly)
            targets = new System.Collections.Generic.List<CharacterScript> { actor };
        foreach (var t in targets) if (t) t.AddStatusEffect(skill.statusToApply);
    }

    yield return new WaitForSeconds(actor.attackRecover);
}

        private IEnumerator DoItem(CharacterScript actor, Data.ItemDefinition item, List<CharacterScript> targets)
        {
            if (!item) yield break;

            // NEW: consume 1 if you have an ItemsInventory, otherwise continue silently
            var inv = actor.GetComponent<ItemsInventory>();
            if (inv && !inv.TryConsume(item, 1)) yield break;

            if (item.effectType == Data.ItemDefinition.EffectType.Heal)
            {
                foreach (var t in targets)
                {
                    if (!t) continue;
                    int heal = DamageCalculator.HealAmount(t.maxHP, item.power, item.isPercent);
                    t.SetHP(t.currentHP + heal);
                    damagePopup?.Spawn(t.transform.position, heal, false, true);
                }
            }
            else if (item.effectType == Data.ItemDefinition.EffectType.ApplyStatus && item.statusToApply)
            {
                foreach (var t in targets) if (t) t.AddStatusEffect(item.statusToApply);
            }

            yield return null;
        }
    }
}
