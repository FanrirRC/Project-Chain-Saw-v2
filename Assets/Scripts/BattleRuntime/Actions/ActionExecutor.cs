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

        [Header("Approach Target")]
        [SerializeField] private float approachDistance = 1.75f;
        [SerializeField] private float moveSpeed = 17.5f;

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

        private IEnumerator MoveTo(Transform who, Vector3 targetPos, float speed)
        {
            if (!who) yield break;
            while ((who.position - targetPos).sqrMagnitude > 0.0004f)
            {
                who.position = Vector3.MoveTowards(who.position, targetPos, speed * Time.deltaTime);
                yield return null;
            }
        }

        private IEnumerator DoBasicAttack(CharacterScript actor, List<CharacterScript> targets)
        {
            if (targets == null || targets.Count == 0) yield break;
            var t = targets[0];
            if (!t) yield break;

            Vector3 startPos = actor.transform.position;
            Quaternion startRot = actor.transform.rotation;

            Vector3 toT = t.transform.position - actor.transform.position; toT.y = 0f;
            if (toT.sqrMagnitude > 0.0001f) actor.transform.rotation = Quaternion.LookRotation(toT);

            Vector3 approachPos = t.transform.position - toT.normalized * Mathf.Max(0.05f, approachDistance);
            approachPos.y = startPos.y; // keep on same plane
            yield return MoveTo(actor.transform, approachPos, moveSpeed);

            actor.PlayAttack();
            yield return new WaitForSeconds(actor.attackWindup);

            int dmg = DamageCalculator.Physical(null, actor, t);
            t.SetHP(t.currentHP - dmg);
            if (damagePopup) damagePopup.Spawn(t.transform.position, dmg, false, false);
            if (t.currentHP > 0) t.PlayHurt();

            yield return new WaitForSeconds(actor.attackRecover);
            actor.PlayIdle();

            yield return MoveTo(actor.transform, startPos, moveSpeed);
            actor.transform.rotation = startRot;

            actor.GainSP(1);
        }


        private IEnumerator DoSkill(CharacterScript actor, Data.SkillDefinition skill, List<CharacterScript> targets)
        {
            if (!skill) yield break;
            if (!skill.TargetsSelfOnly && (targets == null || targets.Count == 0)) yield break;
            if (actor.currentSP < skill.spCost) yield break;
            actor.SetSP(actor.currentSP - skill.spCost);

            Vector3 startPos = actor.transform.position;
            Quaternion startRot = actor.transform.rotation;

            CharacterScript firstTarget = null;
            if (targets != null) foreach (var z in targets) { if (z) { firstTarget = z; break; } }

            if (firstTarget)
            {
                Vector3 toT = firstTarget.transform.position - actor.transform.position; toT.y = 0f;
                if (toT.sqrMagnitude > 0.0001f) actor.transform.rotation = Quaternion.LookRotation(toT);

                Vector3 approachPos = firstTarget.transform.position - toT.normalized * Mathf.Max(0.05f, approachDistance);
                approachPos.y = startPos.y;
                yield return MoveTo(actor.transform, approachPos, moveSpeed);
            }

            actor.PlayAttack();
            yield return new WaitForSeconds(actor.attackWindup);

            if (skill.effectType == Data.SkillDefinition.EffectType.Damage)
            {
                foreach (var tt in targets)
                {
                    if (!tt) continue;
                    int dmg = DamageCalculator.Physical(skill, actor, tt, skill.power, skill.overrideWithPower);
                    tt.SetHP(tt.currentHP - dmg);
                    if (damagePopup) damagePopup.Spawn(tt.transform.position, dmg, false, false);
                    if (tt.currentHP > 0) tt.PlayHurt();
                }
            }
            else if (skill.effectType == Data.SkillDefinition.EffectType.Heal)
            {
                if ((targets == null || targets.Count == 0) && skill.TargetsSelfOnly)
                    targets = new System.Collections.Generic.List<CharacterScript> { actor };
                foreach (var tt in targets)
                {
                    if (!tt) continue;
                    int heal = DamageCalculator.HealAmount(tt.maxHP, skill.power, skill.isPercent);
                    tt.SetHP(tt.currentHP + heal);
                    if (damagePopup) damagePopup.Spawn(tt.transform.position, heal, false, true);
                }
            }
            else if (skill.effectType == Data.SkillDefinition.EffectType.ApplyStatus && skill.statusToApply)
            {
                if ((targets == null || targets.Count == 0) && skill.TargetsSelfOnly)
                    targets = new System.Collections.Generic.List<CharacterScript> { actor };
                foreach (var tt in targets) if (tt) tt.AddStatusEffect(skill.statusToApply);
            }

            yield return new WaitForSeconds(actor.attackRecover);
            actor.PlayIdle();

            yield return MoveTo(actor.transform, startPos, moveSpeed);
            actor.transform.rotation = startRot;
        }

        private IEnumerator DoItem(CharacterScript actor, Data.ItemDefinition item, List<CharacterScript> targets)
        {
            if (!item) yield break;

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
