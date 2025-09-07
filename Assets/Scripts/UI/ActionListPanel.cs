using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UI
{
    public class ActionListPanel : MonoBehaviour
    {
        [SerializeField] private Transform content;           // parent for slots (VerticalLayout)
        [SerializeField] private SkillSlotUI skillSlotPrefab; // existing prefab
        [SerializeField] private ItemSlotUI itemSlotPrefab;  // existing prefab
        [SerializeField] private GameObject emptyLabel;      // optional "No entries"

        [Header("Keyboard Navigation")]
        [SerializeField] private bool allowKeyboard = true;
        [SerializeField] private GameObject selectionCursorPrefab;  // prefab of a small Image/arrow
        [SerializeField] private Vector2 cursorOffset = new Vector2(-20f, 0f);

        public bool WasCancelled { get; private set; }
        public Data.SkillDefinition LastPickedSkill { get; private set; }
        public Data.ItemDefinition LastPickedItem { get; private set; }

        private readonly List<Button> _buttons = new();
        private int _idx = 0;
        private RectTransform _cursorInstance;
        private Canvas _canvas;

        // ---------- Public API ----------
        public IEnumerator OpenSkills(CharacterScript owner)
        {
            ResetState();
            if (!_canvas) _canvas = GetComponentInParent<Canvas>();
            PopulateSkills(owner);     // builds buttons & places cursor
            yield return KeyboardLoop();
            gameObject.SetActive(false);
        }

        public IEnumerator OpenItems(CharacterScript owner)
        {
            ResetState();
            if (!_canvas) _canvas = GetComponentInParent<Canvas>();
            PopulateItems(owner);      // builds buttons & places cursor
            yield return KeyboardLoop();
            gameObject.SetActive(false);
        }

        public void Cancel() => WasCancelled = true;

        // ---------- Populate ----------
        private void PopulateSkills(CharacterScript owner)
        {
            ClearContent();
            int made = 0;
            var inv = owner.GetComponent<SkillsInventory>();
            if (inv?.skills != null)
            {
                foreach (var s in inv.skills)
                {
                    if (!s) continue;
                    var slot = Instantiate(skillSlotPrefab, content);
                    slot.Bind(owner, s, OnPickSkill);
                    made++;
                }
            }

            if (emptyLabel) emptyLabel.SetActive(made == 0);
            gameObject.SetActive(true);

            // >>> NEW: force layout so RectTransforms have correct size/pos on first frame
            Canvas.ForceUpdateCanvases();
            var crt = content as RectTransform;
            if (crt) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(crt);
            Canvas.ForceUpdateCanvases();

            BuildButtonsList();
            EnsureCursor();
            SelectCurrent();   // now correct on first open
        }

        private void PopulateItems(CharacterScript owner)
        {
            ClearContent();
            int made = 0;
            var inv = owner.GetComponent<ItemsInventory>();
            if (inv?.items != null)
            {
                foreach (var e in inv.items)
                {
                    if (e == null || e.item == null || e.count <= 0) continue;
                    var slot = Instantiate(itemSlotPrefab, content);
                    slot.Bind(e.item, e.count, OnPickItem);
                    made++;
                }
            }

            if (emptyLabel) emptyLabel.SetActive(made == 0);
            gameObject.SetActive(true);

            // >>> NEW: force layout so RectTransforms have correct size/pos on first frame
            Canvas.ForceUpdateCanvases();
            var crt = content as RectTransform;
            if (crt) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(crt);
            Canvas.ForceUpdateCanvases();

            BuildButtonsList();
            EnsureCursor();
            SelectCurrent();   // now correct on first open
        }

        // ---------- Keyboard loop ----------
        private IEnumerator KeyboardLoop()
        {
            EventSystem.current?.SetSelectedGameObject(null);
            yield return null;

            // No rebuild here; already built + cursor placed during populate
            // Keep the list clean during the loop in case slots are destroyed
            while (!WasCancelled && LastPickedSkill == null && LastPickedItem == null)
            {
                PruneDestroyedButtons();
                if (_idx >= _buttons.Count) _idx = Mathf.Max(0, _buttons.Count - 1);

                if (allowKeyboard && _buttons.Count > 0)
                {
                    // Only move if there are 2+ entries
                    if (_buttons.Count > 1)
                    {
                        if (Input.GetKeyDown(KeyCode.UpArrow))
                        {
                            _idx = (_idx - 1 + _buttons.Count) % _buttons.Count;
                            SelectCurrent();
                        }
                        if (Input.GetKeyDown(KeyCode.DownArrow))
                        {
                            _idx = (_idx + 1) % _buttons.Count;
                            SelectCurrent();
                        }
                    }

                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    {
                        if (_idx >= 0 && _idx < _buttons.Count && _buttons[_idx])
                            _buttons[_idx].onClick?.Invoke(); // pick focused slot
                    }
                }

                // Esc closes the panel and returns to CommandUI
                if (Input.GetKeyDown(KeyCode.Escape))
                    WasCancelled = true;

                yield return null;
            }

            if (_cursorInstance) _cursorInstance.gameObject.SetActive(false);
        }

        // ---------- Callbacks ----------
        private void OnPickSkill(Data.SkillDefinition s) => LastPickedSkill = s;
        private void OnPickItem(Data.ItemDefinition i) => LastPickedItem = i;

        // ---------- Helpers ----------
        private void ResetState()
        {
            WasCancelled = false;
            LastPickedSkill = null;
            LastPickedItem = null;
            _idx = 0;
        }

        private void ClearContent()
        {
            foreach (Transform c in content) Destroy(c.gameObject);
            _buttons.Clear();
        }

        private void BuildButtonsList()
        {
            _buttons.Clear();
            foreach (var b in content.GetComponentsInChildren<Button>(true))
            {
                if (b && b.gameObject.activeInHierarchy && b.interactable)
                    _buttons.Add(b);
            }
            _idx = Mathf.Clamp(_idx, 0, Mathf.Max(0, _buttons.Count - 1));
        }

        private void PruneDestroyedButtons()
        {
            for (int i = _buttons.Count - 1; i >= 0; i--)
                if (_buttons[i] == null) _buttons.RemoveAt(i);
        }

        private void EnsureCursor()
        {
            if (selectionCursorPrefab && _cursorInstance == null)
            {
                var go = Instantiate(selectionCursorPrefab, transform.parent); // same canvas/panel
                _cursorInstance = go.GetComponent<RectTransform>();
            }
            if (_cursorInstance) _cursorInstance.SetAsLastSibling(); // keep on top
        }

        private void SelectCurrent()
        {
            if (_buttons.Count == 0) return;

            _idx = Mathf.Clamp(_idx, 0, _buttons.Count - 1);
            var btn = _buttons[_idx];
            if (!btn) return;

            var go = btn.gameObject;
            EventSystem.current?.SetSelectedGameObject(go);

            if (_cursorInstance)
            {
                _cursorInstance.gameObject.SetActive(true);
                _cursorInstance.SetAsLastSibling();

                var rt = go.GetComponent<RectTransform>();
                var canvasRT = _canvas ? _canvas.transform as RectTransform : null;

                if (rt && canvasRT)
                {
                    // --- LEFT EDGE (Y centered) ---
                    var corners = new Vector3[4];
                    rt.GetWorldCorners(corners);
                    var leftEdgeMidWorld = (corners[0] + corners[1]) * 0.5f;

                    Vector2 screen = RectTransformUtility.WorldToScreenPoint(
                        _canvas ? _canvas.worldCamera : null, leftEdgeMidWorld);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRT, screen, _canvas ? _canvas.worldCamera : null, out var local);

                    _cursorInstance.SetParent(canvasRT, worldPositionStays: false);
                    _cursorInstance.anchoredPosition = local + cursorOffset;
                }
                else
                {
                    _cursorInstance.position = rt.position + (Vector3)cursorOffset;
                }
            }
        }
    }
}
