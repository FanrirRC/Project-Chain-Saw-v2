using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Pre-built SP strip:
    /// - The container already has up to 9 slot children laid out in the prefab.
    /// - Each slot contains two children: one named SP_Empty and one named SP_Full (configurable).
    /// - This script only toggles visibility. No instantiation.
    ///
    /// Expected hierarchy (example):
    ///   Skill Points v2 (this)
    ///     SP (1)
    ///       SP_Empty  (Image)
    ///       SP_Full   (Image)
    ///     SP (2)
    ///       SP_Empty
    ///       SP_Full
    ///     ...
    ///     SP (9)
    ///       SP_Empty
    ///       SP_Full
    /// </summary>
    public class SPStripUI : MonoBehaviour
    {
        [Header("Slots container")]
        [SerializeField] private Transform container;  // parent with 9 slot children (defaults to this)

        [Header("Child names inside each slot")]
        [SerializeField] private string emptyChildName = "SP_Empty";
        [SerializeField] private string fullChildName = "SP_Full";

        [Header("Options")]
        [Tooltip("If true, hide slots above maxSP. If false, show empties there.")]
        [SerializeField] private bool hideBeyondMax = true;

        [Tooltip("Discover slots automatically from children when binding.")]
        [SerializeField] private bool autoDiscover = true;

        private CharacterScript bound;

        private struct Slot
        {
            public GameObject root;
            public GameObject empty;
            public GameObject full;
        }

        private readonly List<Slot> slots = new(9);

        // ---------- Public API ----------
        /// <summary>Bind to a unit; call this when the HUD row gets its unit.</summary>
        public void Bind(CharacterScript unit)
        {
            Unsubscribe();

            bound = unit;

            if (autoDiscover) DiscoverSlots();
            RefreshAll();

            if (bound != null)
            {
                bound.OnSPChanged += OnSPChanged;
                bound.OnHPChanged += OnSPChanged; // optional refresh on KO
            }
        }

        private void OnDestroy() => Unsubscribe();

        // ---------- Internals ----------
        private void Unsubscribe()
        {
            if (bound != null)
            {
                bound.OnSPChanged -= OnSPChanged;
                bound.OnHPChanged -= OnSPChanged;
            }
        }

        private void OnSPChanged(CharacterScript _) => RefreshAll();

        /// <summary>Find up to 9 slot children and cache their empty/full sub-objects.</summary>
        private void DiscoverSlots()
        {
            slots.Clear();
            if (!container) container = transform;

            // Iterate direct children in order (left->right as in prefab)
            for (int i = 0; i < container.childCount; i++)
            {
                var slotRootT = container.GetChild(i);
                if (!slotRootT) continue;

                var slotRoot = slotRootT.gameObject;

                // locate empty & full under this slot
                Transform emptyT = null, fullT = null;

                if (!string.IsNullOrEmpty(emptyChildName))
                    emptyT = slotRootT.Find(emptyChildName);
                if (!string.IsNullOrEmpty(fullChildName))
                    fullT = slotRootT.Find(fullChildName);

                // fallback: first two children if names not found
                if (emptyT == null || fullT == null)
                {
                    for (int c = 0; c < slotRootT.childCount; c++)
                    {
                        var child = slotRootT.GetChild(c);
                        if (emptyT == null) { emptyT = child; continue; }
                        if (fullT == null) { fullT = child; break; }
                    }
                }

                var slot = new Slot
                {
                    root = slotRoot,
                    empty = emptyT ? emptyT.gameObject : null,
                    full = fullT ? fullT.gameObject : null
                };

                slots.Add(slot);
                if (slots.Count == CharacterScript.SP_CAP) break; // stop at 9
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

                // Slot root visibility (hide beyond max if desired)
                if (s.root) s.root.SetActive(!hideBeyondMax || withinMax);

                // Empty orb: visible within max (or everywhere if not hiding)
                if (s.empty)
                {
                    bool showEmpty = withinMax || !hideBeyondMax;
                    s.empty.SetActive(showEmpty);
                }

                // Full star: show for the leftmost 'cur' slots only
                if (s.full)
                {
                    bool showFull = i < cur;
                    s.full.SetActive(showFull);
                }
            }
        }
    }
}
