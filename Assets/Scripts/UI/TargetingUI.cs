using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    public class TargetingUI : MonoBehaviour
    {
        public List<CharacterScript> ResultTargets { get; private set; } = new();
        public bool WasCancelled { get; private set; }

        [Header("Cursor Prefab")]
        [SerializeField] private GameObject cursorPrefab;

        [Header("Screen-space mode (recommended)")]
        [SerializeField] private bool useScreenSpaceCursor = true;
        [SerializeField] private Canvas screenCanvas;
        [SerializeField] private Vector2 screenOffset = new(0, 60f);

        [Header("World-space mode")]
        [SerializeField] private Vector3 worldOffset = new(0, 1.5f, 0);

        private GameObject _cursorInstance;
        private RectTransform _cursorRT;

        private readonly List<GameObject> _cursorInstancesAll = new();
        private readonly List<RectTransform> _cursorRTsAll = new();

        private void Awake()
        {
            if (!cursorPrefab) return;

            if (useScreenSpaceCursor && screenCanvas && screenCanvas.renderMode != RenderMode.WorldSpace)
            {
                _cursorInstance = Instantiate(cursorPrefab, screenCanvas.transform);
                _cursorRT = _cursorInstance.GetComponent<RectTransform>();
            }
            else
            {
                _cursorInstance = Instantiate(cursorPrefab);
            }
            _cursorInstance.SetActive(false);
        }

        public IEnumerator SelectTargets(CharacterScript actor, IReadOnlyList<CharacterScript> pool, TargetMode mode)
        {
            ResultTargets.Clear();
            WasCancelled = false;
            gameObject.SetActive(true);

            var live = new List<CharacterScript>();
            if (pool != null)
                foreach (var u in pool)
                    if (u && u.currentHP > 0) live.Add(u);

            EventSystem.current?.SetSelectedGameObject(null);
            yield return null;

            if (live.Count == 0) { WasCancelled = true; gameObject.SetActive(false); yield break; }

            ClearAllCursors();

            // --- NEW: ALL mode — show a cursor on EVERY live target and wait for Enter/Esc ---
            if (mode == TargetMode.All)
            {
                // build ResultTargets immediately
                ResultTargets.AddRange(live);

                // spawn a cursor for each target
                foreach (var t in live)
                {
                    GameObject c;
                    RectTransform rt = null;

                    if (useScreenSpaceCursor && screenCanvas && screenCanvas.renderMode != RenderMode.WorldSpace)
                    {
                        c = Instantiate(cursorPrefab, screenCanvas.transform);
                        rt = c.GetComponent<RectTransform>();
                        // position in screen-space
                        Vector2 screen = Camera.main ? (Vector2)Camera.main.WorldToScreenPoint(t.transform.position + (Vector3)worldOffset) : Vector2.zero;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            (RectTransform)screenCanvas.transform, screen,
                            screenCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : screenCanvas.worldCamera,
                            out var local);
                        rt.anchoredPosition = local + screenOffset;
                    }
                    else
                    {
                        c = Instantiate(cursorPrefab);
                        c.transform.position = t.transform.position + worldOffset;
                    }

                    c.SetActive(true);
                    _cursorInstancesAll.Add(c);
                    if (rt) _cursorRTsAll.Add(rt);
                }

                // Wait for Enter to confirm or Esc to cancel
                while (true)
                {
                    if (Input.GetKeyDown(KeyCode.Escape)) { WasCancelled = true; break; }
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) { break; }
                    // keep the cursors following targets if they move:
                    if (_cursorRTsAll.Count > 0 && screenCanvas && screenCanvas.renderMode != RenderMode.WorldSpace)
                    {
                        for (int i = 0; i < live.Count && i < _cursorRTsAll.Count; i++)
                        {
                            var t = live[i];
                            var rt = _cursorRTsAll[i];
                            if (!t || !rt) continue;
                            Vector2 screen = Camera.main ? (Vector2)Camera.main.WorldToScreenPoint(t.transform.position + (Vector3)worldOffset) : Vector2.zero;
                            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                                (RectTransform)screenCanvas.transform, screen,
                                screenCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : screenCanvas.worldCamera,
                                out var local);
                            rt.anchoredPosition = local + screenOffset;
                        }
                    }
                    yield return null;
                }

                ClearAllCursors();
                gameObject.SetActive(false);
                yield break;
            }

            int index = 0;
            Highlight(live, index); // your existing single-cursor highlight
            while (true)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow)) { index = (index - 1 + live.Count) % live.Count; Highlight(live, index); }
                if (Input.GetKeyDown(KeyCode.RightArrow)) { index = (index + 1) % live.Count; Highlight(live, index); }
                if (Input.GetKeyDown(KeyCode.Escape)) { WasCancelled = true; break; }
                if (Input.GetKeyDown(KeyCode.Return)) { ResultTargets.Clear(); ResultTargets.Add(live[index]); break; }
                yield return null;
            }

            _cursorInstance?.SetActive(false);
            gameObject.SetActive(false);
        }

        private void Highlight(IReadOnlyList<CharacterScript> pool, int index)
        {
            if (pool.Count == 0) return;
            var u = pool[index];

            if (_cursorInstance)
            {
                _cursorInstance.SetActive(true);

                if (useScreenSpaceCursor && screenCanvas && screenCanvas.renderMode != RenderMode.WorldSpace)
                {
                    var cam = screenCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : screenCanvas.worldCamera;
                    Vector2 screen = Camera.main ? (Vector2)Camera.main.WorldToScreenPoint(u.transform.position + worldOffset) : Vector2.zero;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        (RectTransform)screenCanvas.transform, screen, cam, out var local);
                    _cursorRT.anchoredPosition = local + screenOffset;
                }
                else
                {
                    _cursorInstance.transform.position = u.transform.position + worldOffset;
                }
            }
        }

        private void ClearAllCursors()
        {
            if (_cursorInstance) _cursorInstance.SetActive(false);
            foreach (var go in _cursorInstancesAll) if (go) Destroy(go);
            _cursorInstancesAll.Clear();
            _cursorRTsAll.Clear();
        }
    }
}
