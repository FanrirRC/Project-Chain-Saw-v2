using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Data;

namespace UI
{
    public class CommandDecision
    {
        public enum DecisionType { None, Attack, Skill, Item, Guard }
        public DecisionType Type;
        public SkillDefinition Skill;
        public ItemDefinition Item;
        public bool NeedsTarget;
        public bool TargetsAllies;
        public TargetMode TargetMode;
        public List<CharacterScript> Targets;
    }

    public enum TargetMode { Single, All }

    public class CommandUI : MonoBehaviour
    {
        [Header("Optional: single list panel reused by Skills & Items")]
        [SerializeField] private ActionListPanel actionPanel;

        [Header("Keyboard Navigation")]
        [SerializeField] private Transform buttonsRoot;             // parent with Attack/Skills/Items/Guard/Cancel buttons
        [SerializeField] private bool allowKeyboard = true;
        [SerializeField] private GameObject selectionCursorPrefab;  // prefab of a small Image/arrow
        [SerializeField] private Vector2 cursorOffset = new Vector2(-24f, 0f);

        public CommandDecision LastDecision { get; private set; }
        public bool WasCancelled { get; private set; }

        private CharacterScript _currentActor;
        private readonly List<Button> _buttons = new();
        private int _idx = 0;
        private RectTransform _cursorInstance; // instantiated once
        private Canvas _canvas;                // for proper UI coordinate conversion

        private enum SubmenuRequest { None, Skills, Items }
        private SubmenuRequest _submenu = SubmenuRequest.None;

        // -------- Button hooks (wire these from your UI Buttons) --------
        public void ChooseAttack() => LastDecision = new CommandDecision
        {
            Type = CommandDecision.DecisionType.Attack,
            NeedsTarget = true,
            TargetsAllies = false,
            TargetMode = TargetMode.Single
        };
        public void ChooseGuard() => LastDecision = new CommandDecision
        {
            Type = CommandDecision.DecisionType.Guard,
            NeedsTarget = false
        };
        public void CancelMenu() => WasCancelled = true;

        // These **request** opening a submenu; the main loop will actually open it and pause input
        public void OpenSkills() { _submenu = SubmenuRequest.Skills; }
        public void OpenItems() { _submenu = SubmenuRequest.Items; }

        public void ChooseSkill(SkillDefinition s)
        {
            LastDecision = new CommandDecision
            {
                Type = CommandDecision.DecisionType.Skill,
                Skill = s,
                NeedsTarget = !s.TargetsSelfOnly,
                TargetsAllies = s.TargetsAllies,
                TargetMode = s.targetsAll ? TargetMode.All : TargetMode.Single
            };
        }

        public void ChooseItem(ItemDefinition i, bool allies, bool all)
        {
            LastDecision = new CommandDecision
            {
                Type = CommandDecision.DecisionType.Item,
                Item = i,
                NeedsTarget = true,
                TargetsAllies = allies,
                TargetMode = all ? TargetMode.All : TargetMode.Single
            };
        }

        // -------- Lifecycle --------
        public IEnumerator OpenFor(CharacterScript actor)
        {
            _currentActor = actor;
            WasCancelled = false;
            LastDecision = new CommandDecision();
            _submenu = SubmenuRequest.None;

            if (!_canvas) _canvas = GetComponentInParent<Canvas>();

            gameObject.SetActive(true);

            EventSystem.current?.SetSelectedGameObject(null);
            yield return null;

            if (allowKeyboard)
            {
                BuildButtonsList();
                EnsureCursor();
                SelectCurrent(); // place cursor on current button
            }

            while (LastDecision.Type == CommandDecision.DecisionType.None && !WasCancelled)
            {
                // If a submenu was requested, open it synchronously and pause this loop's input
                if (_submenu != SubmenuRequest.None && actionPanel != null)
                {
                    // hide command cursor while submenu is active
                    if (_cursorInstance) _cursorInstance.gameObject.SetActive(false);

                    if (_submenu == SubmenuRequest.Skills)
                    {
                        yield return actionPanel.OpenSkills(_currentActor);
                        if (!actionPanel.WasCancelled && actionPanel.LastPickedSkill)
                            ChooseSkill(actionPanel.LastPickedSkill);
                    }
                    else if (_submenu == SubmenuRequest.Items)
                    {
                        yield return actionPanel.OpenItems(_currentActor);
                        if (!actionPanel.WasCancelled && actionPanel.LastPickedItem)
                            ChooseItem(actionPanel.LastPickedItem, allies: true, all: false);
                    }

                    // submenu handled; clear request and restore command cursor
                    _submenu = SubmenuRequest.None;
                    if (_cursorInstance) _cursorInstance.gameObject.SetActive(true);
                    SelectCurrent();

                    if (LastDecision.Type != CommandDecision.DecisionType.None || WasCancelled)
                        break;

                    // continue next frame to avoid eating the Enter/Esc that closed submenu
                    yield return null;
                    continue;
                }

                // Command menu input (only when no submenu is open)
                if (allowKeyboard && _buttons.Count > 0)
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
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    {
                        _buttons[_idx].onClick?.Invoke(); // invoke selected button
                    }
                }

                if (Input.GetKeyDown(KeyCode.Escape)) WasCancelled = true;
                yield return null;
            }

            if (_cursorInstance) _cursorInstance.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        // -------- Helpers --------
        private void BuildButtonsList()
        {
            _buttons.Clear();
            if (!buttonsRoot) buttonsRoot = transform;

            foreach (var b in buttonsRoot.GetComponentsInChildren<Button>(true))
                if (b.gameObject.activeInHierarchy && b.interactable)
                    _buttons.Add(b);

            _idx = Mathf.Clamp(_idx, 0, Mathf.Max(0, _buttons.Count - 1));
        }

        private void EnsureCursor()
        {
            if (selectionCursorPrefab && _cursorInstance == null)
            {
                // parent beside the menu (same canvas)
                var go = Instantiate(selectionCursorPrefab, transform.parent);
                _cursorInstance = go.GetComponent<RectTransform>();
            }
        }

        private void SelectCurrent()
        {
            if (_buttons.Count == 0) return;

            var go = _buttons[_idx].gameObject;
            EventSystem.current?.SetSelectedGameObject(go);

            if (_cursorInstance)
            {
                _cursorInstance.gameObject.SetActive(true);

                var btnRT = go.GetComponent<RectTransform>();
                var canvasRT = _canvas ? _canvas.transform as RectTransform : null;

                if (btnRT && canvasRT)
                {
                    // --- LEFT EDGE (Y centered) ---
                    // world corners: 0=bottom-left, 1=top-left, 2=top-right, 3=bottom-right
                    var corners = new Vector3[4];
                    btnRT.GetWorldCorners(corners);
                    var leftEdgeMidWorld = (corners[0] + corners[1]) * 0.5f;

                    // convert to canvas local space
                    Vector2 screen = RectTransformUtility.WorldToScreenPoint(
                        _canvas ? _canvas.worldCamera : null, leftEdgeMidWorld);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRT, screen, _canvas ? _canvas.worldCamera : null, out var local);

                    _cursorInstance.SetParent(canvasRT, worldPositionStays: false);
                    _cursorInstance.anchoredPosition = local + cursorOffset;
                }
                else
                {
                    // fallback
                    _cursorInstance.position = btnRT.position + (Vector3)cursorOffset;
                }
            }
        }
    }
}