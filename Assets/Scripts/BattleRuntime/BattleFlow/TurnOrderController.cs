using System.Collections.Generic;
using UnityEngine;
using System;

public class TurnOrderController : MonoBehaviour
{
    [SerializeField] private SpawnPositions spawns;
    private readonly Queue<CharacterScript> _queue = new();
    private readonly List<CharacterScript> _forecast = new();

    public IReadOnlyList<CharacterScript> Forecast => _forecast;

    public event Action ForecastChanged;

    public void Initialize()
    {
        _queue.Clear();
        _forecast.Clear();

        var all = new List<CharacterScript>();
        all.AddRange(spawns.Players);
        all.AddRange(spawns.Enemies);

        all.Sort((a, b) => b.GetAGI().CompareTo(a.GetAGI()));
        foreach (var u in all) if (u) _queue.Enqueue(u);
        RebuildForecast();
    }

    public CharacterScript PopNext()
    {
        if (_queue.Count == 0) return null;
        var u = _queue.Dequeue();
        RebuildForecast();
        return u;
    }

    public void Requeue(CharacterScript unit)
    {
        if (!unit) return;
        _queue.Enqueue(unit);
        RebuildForecast();
    }

    private void RebuildForecast()
    {
        _forecast.Clear();
        _forecast.AddRange(_queue);
        ForecastChanged?.Invoke();
    }
}
