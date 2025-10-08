using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UI
{
    public class ActionListPanel : MonoBehaviour
    {
        [SerializeField] private Transform content;
        [SerializeField] private SkillSlotUI skillSlotPrefab;
        [SerializeField] private ItemSlotUI itemSlotPrefab;
        [SerializeField] private GameObject emptyLabel;

        [Header("Keyboard Navigation")]
        [SerializeField] private bool allowKeyboard = true;
        [SerializeField] private GameObject selectionCursorPrefab;
        [SerializeField] private Vector2 cursorOffset = new Vector2(-20f, 0f);

        public bool WasCancelled { get; private set; }
        public Data.SkillDefinition LastPickedSkill { get; private set; }
        public Data.ItemDefinition LastPickedItem { get; private set; }

        private readonly List<Button> _buttons = new();
        private int _idx = 0;
        private RectTransform _cursorInstance;
        private Canvas _canvas;

        public IEnumerator OpenSkills(CharacterScript owner)
        {
            ResetState();
            if (!_canvas) _canvas = GetComponentInParent<Canvas>();
            PopulateSkills(owner);
            yield return KeyboardLoop();
            gameObject.SetActive(false);
        }

        public IEnumerator OpenItems(CharacterScript owner)
        {
            ResetState();
            if (!_canvas) _canvas = GetComponentInParent<Canvas>();
            PopulateItems(owner);
            yield return KeyboardLoop();
            gameObject.SetActive(false);
        }

        public void Cancel() => WasCancelled = true;

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

            Canvas.ForceUpdateCanvases();
            var crt = content as RectTransform;
            if (crt) LayoutRebuilder.ForceRebuildLayoutImmediate(crt);
            Canvas.ForceUpdateCanvases();

            BuildButtonsList();
            EnsureCursor();
            SelectCurrent();
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

            Canvas.ForceUpdateCanvases();
            var crt = content as RectTransform;
            if (crt) LayoutRebuilder.ForceRebuildLayoutImmediate(crt);
            Canvas.ForceUpdateCanvases();

            BuildButtonsList();
            EnsureCursor();
            SelectCurrent();
        }

        private IEnumerator KeyboardLoop()
        {
            EventSystem.current?.SetSelectedGameObject(null);
            yield return null;

            while (!WasCancelled && LastPickedSkill == null && LastPickedItem == null)
            {
                PruneDestroyedButtons();
                if (_buttons.Count == 0)
                {
                    if (Input.GetKeyDown(KeyCode.Escape)) WasCancelled = true;
                    yield return null;
                    continue;
                }

                _idx = Mathf.Clamp(_idx, 0, _buttons.Count - 1);
                SelectCurrent();

                if (allowKeyboard)
                {
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
                        var btn = _buttons[_idx];
                        if (btn && btn.interactable)
                        {
                            btn.onClick?.Invoke();
                        }
                        else
                        {
                            // play "error" sfx
                        }
                    }
                }

                if (Input.GetKeyDown(KeyCode.Escape)) WasCancelled = true;

                yield return null;
            }

            if (_cursorInstance) _cursorInstance.gameObject.SetActive(false);
        }

        private void OnPickSkill(Data.SkillDefinition s) => LastPickedSkill = s;
        private void OnPickItem(Data.ItemDefinition i) => LastPickedItem = i;

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
                if (b && b.gameObject.activeInHierarchy)
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
                var go = Instantiate(selectionCursorPrefab, transform.parent);
                _cursorInstance = go.GetComponent<RectTransform>();
            }
            if (_cursorInstance) _cursorInstance.SetAsLastSibling();
            if (!_canvas) _canvas = GetComponentInParent<Canvas>();
        }

        private void SelectCurrent()
        {
            if (_buttons.Count == 0) return;

            var go = _buttons[_idx].gameObject;
            EventSystem.current?.SetSelectedGameObject(go);

            if (_cursorInstance)
            {
                _cursorInstance.gameObject.SetActive(true);
                _cursorInstance.SetAsLastSibling();

                var rt = go.GetComponent<RectTransform>();
                var canvasRT = _canvas ? _canvas.transform as RectTransform : null;

                if (rt && canvasRT)
                {
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
