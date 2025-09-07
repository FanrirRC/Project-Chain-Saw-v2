using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Data;

public class CharacterScript : MonoBehaviour
{
    public enum UnitType { Player, Enemy }

    // ---- SP rules ----
    public const int SP_CAP = 9; // absolute cap for the system (display + logic)

    [Header("Unit Details")]
    [EnumToggleButtons]
    [OnValueChanged(nameof(OnUnitTypeChanged))]
    public UnitType unitType = UnitType.Player;

    public string characterName;
    public GameObject characterModel;
    public Sprite portrait;

    [TitleGroup("Character Stats")]
    [ShowIf("@unitType == CharacterScript.UnitType.Player")]
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    [HideLabel]
    public CharacterBaseStats characterStats;

    [TitleGroup("Enemy Stats")]
    [ShowIf("@unitType == CharacterScript.UnitType.Enemy")]
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    [HideLabel]
    public EnemyBaseStats enemyStats;

    [Header("Runtime Resources")]
    public int maxHP;
    public int currentHP;
    public int maxSP;      // clamped to SP_CAP in InitializeFromData
    public int currentSP;

    [Serializable]
    public struct ActiveStatusEffect
    {
        public StatusEffectDefinition effect;
        public int remainingTurns;
    }
    public List<ActiveStatusEffect> activeStatusEffects = new();

    public event Action<CharacterScript> OnHPChanged;
    public event Action<CharacterScript> OnSPChanged;
    public event Action<CharacterScript> OnDied;

    public bool IsEnemy => unitType == UnitType.Enemy;

    // ------------------------------
    // ANIMATION
    // ------------------------------
    [Header("Animation")]
    public Animator animator;               // assign in prefab
    public string idleState = "Idle";
    public string attackTrigger = "Attack";
    public string hurtTrigger = "Hurt";
    public string dieTrigger = "Die";
    [Tooltip("Optional: direct state name to crossfade to.")]
    public string dieState = "Die";

    [Header("Animation Timings")]
    [Range(0f, 1f)] public float attackWindup = 0.25f;
    [Range(0f, 1f)] public float attackRecover = 0.25f;

    public void PlayIdle()
    {
        if (animator && !string.IsNullOrEmpty(idleState))
            animator.CrossFade(idleState, 0.05f, 0, 0f);
    }
    public void PlayAttack()
    {
        if (!animator) return;
        if (!string.IsNullOrEmpty(hurtTrigger)) animator.ResetTrigger(hurtTrigger);
        animator.SetTrigger(attackTrigger);
    }
    public void PlayHurt()
    {
        if (!animator) return;
        if (!string.IsNullOrEmpty(attackTrigger)) animator.ResetTrigger(attackTrigger);
        animator.SetTrigger(hurtTrigger);
    }
    public void PlayDie()
    {
        if (!animator) return;
        if (!string.IsNullOrEmpty(attackTrigger)) animator.ResetTrigger(attackTrigger);
        if (!string.IsNullOrEmpty(hurtTrigger)) animator.ResetTrigger(hurtTrigger);
        if (!string.IsNullOrEmpty(dieTrigger)) animator.SetTrigger(dieTrigger);
        if (!string.IsNullOrEmpty(dieState)) animator.CrossFade(dieState, 0.05f, 0, 0f); // fallback
    }
    // ------------------------------

    private void Awake() => InitializeFromData();

    public Sprite GetPortrait() => portrait;

    private void OnUnitTypeChanged()
    {
#if UNITY_EDITOR
        Undo.RecordObject(this, "Change Unit Type");
#endif

        if (unitType == UnitType.Player) enemyStats = null;
        else characterStats = null;

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    public void InitializeFromData()
    {
        int baseMaxHP = 100, baseMaxSP = 50;
        if (unitType == UnitType.Player && characterStats)
        {
            baseMaxHP = characterStats.maxHP;
            baseMaxSP = characterStats.maxSP;
            if (string.IsNullOrEmpty(characterName)) characterName = characterStats.displayName;
        }
        else if (unitType == UnitType.Enemy && enemyStats)
        {
            baseMaxHP = enemyStats.maxHP;
            baseMaxSP = enemyStats.maxSP;
            if (string.IsNullOrEmpty(characterName)) characterName = enemyStats.displayName;
        }

        maxHP = baseMaxHP;

        // SP stage 2: clamp design value to the 0..SP_CAP range for gameplay & UI
        maxSP = Mathf.Clamp(baseMaxSP, 0, SP_CAP);

        currentHP = maxHP;
        currentSP = 0; // start empty; tweak if you want to start with some SP
        activeStatusEffects.Clear();
    }

    public int GetBaseATK()
    {
        if (unitType == UnitType.Player && characterStats) return characterStats.atk;
        if (unitType == UnitType.Enemy && enemyStats) return enemyStats.atk;
        return 0;
    }
    public int GetBaseDEF()
    {
        if (unitType == UnitType.Player && characterStats) return characterStats.def;
        if (unitType == UnitType.Enemy && enemyStats) return enemyStats.def;
        return 0;
    }
    public int GetAGI()
    {
        if (unitType == UnitType.Player && characterStats) return characterStats.agi;
        if (unitType == UnitType.Enemy && enemyStats) return enemyStats.agi;
        return 0;
    }

    public void SetHP(int value)
    {
        int v = Mathf.Clamp(value, 0, maxHP);
        if (v == currentHP) return;
        currentHP = v;
        OnHPChanged?.Invoke(this);

        if (currentHP == 0)
        {
            PlayDie();
            OnDied?.Invoke(this);
        }
    }

    public void SetSP(int value)
    {
        int v = Mathf.Clamp(value, 0, maxSP);
        if (v == currentSP) return;
        currentSP = v;
        OnSPChanged?.Invoke(this);
    }

    // ---- SP helpers (used by executor & skills) ----
    public void GainSP(int amount)
    {
        if (amount <= 0) return;
        int v = Mathf.Clamp(currentSP + amount, 0, maxSP);
        if (v == currentSP) return;
        currentSP = v;
        OnSPChanged?.Invoke(this);
    }

    /// <summary> Returns true if the spend succeeded. </summary>
    public bool SpendSP(int amount)
    {
        if (amount <= 0) return true;
        if (currentSP < amount) return false;
        currentSP -= amount;
        OnSPChanged?.Invoke(this);
        return true;
    }

    public void AddStatusEffect(StatusEffectDefinition so)
    {
        if (!so) return;
        activeStatusEffects.Add(new ActiveStatusEffect
        {
            effect = so,
            remainingTurns = Mathf.Max(1, so.durationTurns)
        });
    }

    public void TickStatusesAtTurnEnd()
    {
        for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
        {
            var s = activeStatusEffects[i];

            if (s.effect.isDOT && s.effect.dotPercentOfMaxHP > 0 && currentHP > 0)
            {
                int dot = Actions.DamageCalculator.DotPercent(this, s.effect.dotPercentOfMaxHP);
                SetHP(currentHP - dot);
            }

            s.remainingTurns--;
            if (s.remainingTurns <= 0) activeStatusEffects.RemoveAt(i);
            else activeStatusEffects[i] = s;
        }
    }
}
