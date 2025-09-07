using System.Collections.Generic;
using UnityEngine;

public class SpawnPositions : MonoBehaviour
{
    // Players positions
    private static readonly Vector3[] P1 = { new Vector3(0f, 0.25f, -5f) };
    private static readonly Vector3[] P2 = {
        new Vector3(-1.25f, 0.25f, -5f),
        new Vector3( 1.25f, 0.25f, -5f)
    };
    private static readonly Vector3[] P3 = {
        new Vector3( 0f,    0.25f, -5f),
        new Vector3( 2.5f,  0.25f, -5f),
        new Vector3(-2.5f,  0.25f, -5f)
    };

    // Enemies positions
    private static readonly Vector3[] E1 = { new Vector3(0f, 0.25f, 2.5f) };
    private static readonly Vector3[] E2 = {
        new Vector3(-1.5625f, 0.25f, 2.5f),
        new Vector3( 1.5625f, 0.25f, 2.5f)
    };
    private static readonly Vector3[] E3 = {
        new Vector3( 0f,     0.25f, 2.5f),
        new Vector3( 3.125f, 0.25f, 3.75f),
        new Vector3(-3.125f, 0.25f, 3.75f)
    };

    private readonly List<CharacterScript> _players = new();
    private readonly List<CharacterScript> _enemies = new();

    public IReadOnlyList<CharacterScript> Players => _players;
    public IReadOnlyList<CharacterScript> Enemies => _enemies;

    public void Spawn(GameObject[] playerPrefabs, GameObject[] enemyPrefabs)
    {
        ClearAll();
        var pList = new List<GameObject>();
        if (playerPrefabs != null) foreach (var p in playerPrefabs) if (p) pList.Add(p);

        var eList = new List<GameObject>();
        if (enemyPrefabs != null) foreach (var e in enemyPrefabs) if (e) eList.Add(e);

        SpawnSide(pList, true);
        SpawnSide(eList, false);
    }

    private void SpawnSide(List<GameObject> prefabs, bool isPlayerSide)
    {
        int count = prefabs.Count;
        if (count == 0) return;

        Vector3[] spots = isPlayerSide ? GetPlayerSpots(count) : GetEnemySpots(count);
        if (spots == null || spots.Length != count)
        {
            Debug.LogWarning($"SpawnPositions: No {(isPlayerSide ? "player" : "enemy")} layout for size {count}.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(prefabs[i], spots[i], Quaternion.identity, transform);
            var unit = go.GetComponent<CharacterScript>() ?? go.GetComponentInChildren<CharacterScript>(true);
            if (!unit)
            {
                Debug.LogWarning($"Spawned prefab '{go.name}' has no CharacterScript. Skipping registration.");
                continue;
            }
            (isPlayerSide ? _players : _enemies).Add(unit);
        }
    }

    private static Vector3[] GetPlayerSpots(int count)
    {
        return count switch
        {
            1 => P1,
            2 => P2,
            3 => P3,
            _ => null
        };
    }

    private static Vector3[] GetEnemySpots(int count)
    {
        return count switch
        {
            1 => E1,
            2 => E2,
            3 => E3,
            _ => null
        };
    }

    public void ClearAll()
    {
        _players.Clear();
        _enemies.Clear();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);
            return;
        }
#endif
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }
}
