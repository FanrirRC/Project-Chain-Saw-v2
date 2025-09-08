using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class SPStripUI : MonoBehaviour
    {
        [Header("Slots container")]
        [SerializeField] private Transform container;  // parent with 9 slot children (defaults to this)

        [Header("Child names inside each slot")]
        [SerializeField] private string emptyChildName = "SP_Empty";
        [SerializeField] private string fullChildName = "SP_Full";

        [Header("Options")]
        [Tooltip("Discover slots automatically from children when binding.")]
        [SerializeField] private bool autoDiscover = true;

        private CharacterScript bound;

        private struct Slot
        {
            public GameObject root;
            public GameObject empty;
            public GameObject full;
            public bool warned;
        }

        private readonly List<Slot> slots = new(9);

        /// <summary>Bind to a unit; call from BattleHUD when assigning the row.</summary>
        public void Bind(CharacterScript unit)
        {
            Unsubscribe();

            bound = unit;

            if (autoDiscover) DiscoverSlots();
            RefreshAll();

            if (bound != null)
            {
                bound.OnSPChanged += OnSPChanged;
                bound.OnHPChanged += OnSPChanged; // refresh on KO
            }
        }

        private void OnDestroy() => Unsubscribe();

        private void Unsubscribe()
        {
            if (bound != null)
            {
                bound.OnSPChanged -= OnSPChanged;
                bound.OnHPChanged -= OnSPChanged;
            }
        }

        private void OnSPChanged(CharacterScript _) => RefreshAll();

        private void DiscoverSlots()
        {
            slots.Clear();
            if (!container) container = transform;

            for (int i = 0; i < container.childCount; i++)
            {
                var slotRootT = container.GetChild(i);
                if (!slotRootT) continue;

                // Find SP_Empty (direct)
                Transform emptyT = null;
                if (!string.IsNullOrEmpty(emptyChildName))
                    emptyT = slotRootT.Find(emptyChildName);

                // Find SP_Full (try direct, then nested under empty, then deep scan)
                Transform fullT = null;
                if (!string.IsNullOrEmpty(fullChildName))
                {
                    // direct
                    fullT = slotRootT.Find(fullChildName);

                    // nested under empty
                    if (!fullT && emptyT)
                        fullT = emptyT.Find(fullChildName);

                    // deep scan (inactive too)
                    if (!fullT)
                    {
                        foreach (var t in slotRootT.GetComponentsInChildren<Transform>(true))
                        {
                            if (t.name == fullChildName) { fullT = t; break; }
                        }
                    }
                }

                var slot = new Slot
                {
                    root = slotRootT.gameObject,
                    empty = emptyT ? emptyT.gameObject : null,
                    full = fullT ? fullT.gameObject : null,
                    warned = false
                };

                slots.Add(slot);
                if (slots.Count == CharacterScript.SP_CAP) break;
            }
        }

        private void RefreshAll()
        {
            if (slots.Count == 0) return;

            int max = bound ? Mathf.Clamp(bound.maxSP, 0, CharacterScript.SP_CAP) : 0;
            int cur = bound ? Mathf.Clamp(bound.currentSP, 0, max) : 0;

            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                bool withinMax = i < max;

                if (s.root) s.root.SetActive(withinMax);

                // Empty orbs ALWAYS visible within max
                if (s.empty) s.empty.SetActive(withinMax);

                // Full star visible for leftmost 'cur' only
                if (s.full) s.full.SetActive(withinMax && i < cur);

                // Helpful warning once per slot if something’s missing
                if (withinMax && !s.warned && (!s.empty || !s.full))
                {
                    s.warned = true;
                    Debug.LogWarning(
                        $"[SPStripUI] Slot {i + 1} in '{container.name}' missing child(s). " +
                        $"Found Empty={(s.empty != null)} Full={(s.full != null)}. " +
                        $"Expected names: '{emptyChildName}' and '{fullChildName}'.",
                        container
                    );
                }

                slots[i] = s; // write back 'warned'
            }
        }
    }
}
