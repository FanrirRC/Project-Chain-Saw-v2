using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class BattleController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BattleSetup setup;
    [SerializeField] private SpawnPositions spawns;
    [SerializeField] private TurnOrderController turnOrder;
    [SerializeField] private EnemyAI enemyAI;
    [SerializeField] private Actions.ActionExecutor executor;
    [SerializeField] private UI.CommandUI commandUI;
    [SerializeField] private UI.TargetingUI targetingUI;
    [SerializeField] private UI.BattleHUD hud;
    [SerializeField] private UI.TurnOrderBar orderBar;

    [Header("Pacing")]
    [SerializeField] private float enemyPostActionPause = 0.25f;


    private CharacterScript _active;

    private readonly HashSet<CharacterScript> _enemyTargetAvoid = new();

    private void Start()
    {
        if (setup && spawns) spawns.Spawn(setup.playerPrefabs, setup.enemyPrefabs);

        turnOrder.Initialize();

        hud?.Bind(spawns.Players);
        orderBar?.Bind(turnOrder);

        hud?.SetActiveUnit(null);
        StartCoroutine(BattleLoop());
    }

    private IEnumerator BattleLoop()
    {
        bool lastWasEnemy = false;

        while (true)
        {
            _active = turnOrder.PopNext();
            if (_active == null) break;
            if (_active.currentHP <= 0) continue;

            hud?.SetActiveUnit(_active);

            bool isPlayer = spawns.Players.Contains(_active);

            if (isPlayer && lastWasEnemy)
                _enemyTargetAvoid.Clear();

            if (isPlayer)
            {
                yield return PlayerTurn();
                lastWasEnemy = false;
            }
            else
            {
                yield return EnemyTurnUniqueTarget();
                lastWasEnemy = true;
            }

            _active.TickStatusesAtTurnEnd();
            hud?.RefreshAll();

            if (_active.currentHP > 0)
                turnOrder.Requeue(_active);
        }

        hud?.SetActiveUnit(null);
    }

    private IEnumerator PlayerTurn()
    {
        while (true)
        {
            yield return commandUI.OpenFor(_active);
            if (commandUI.WasCancelled) { continue; }

            var decision = commandUI.LastDecision;
            if (decision.Type == UI.CommandDecision.DecisionType.None) { continue; }

            var targets = decision.Targets;
            if (decision.NeedsTarget)
            {
                var pool = decision.TargetsAllies ? spawns.Players : spawns.Enemies;
                yield return targetingUI.SelectTargets(_active, pool, decision.TargetMode);
                if (targetingUI.WasCancelled) { continue; }
                targets = targetingUI.ResultTargets;
            }

            yield return executor.Execute(_active, decision, targets);
            hud?.RefreshAll();
            break;
        }
    }

    private IEnumerator EnemyTurnUniqueTarget()
    {
        var intent = enemyAI.Decide(_active, spawns.Players, spawns.Enemies);

        if (intent.NeedsTarget && (intent.Targets == null || intent.Targets.Count == 0))
            intent.Targets = enemyAI.PickTargetsUnique(intent, spawns.Players, spawns.Enemies, _enemyTargetAvoid);

        if (intent.Targets != null)
            foreach (var t in intent.Targets)
                if (t) _enemyTargetAvoid.Add(t);

        yield return executor.ExecuteIntent(_active, intent);
        hud?.RefreshAll();

        if (enemyPostActionPause > 0f)
            yield return new WaitForSeconds(enemyPostActionPause);
    }
}
