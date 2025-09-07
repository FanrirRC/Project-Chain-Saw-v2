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

            int index = 0;
            Highlight(live, index);

            while (true)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    index = (index - 1 + live.Count) % live.Count;
                    Highlight(live, index);
                }
                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    index = (index + 1) % live.Count;
                    Highlight(live, index);
                }
                if (Input.GetKeyDown(KeyCode.Escape)) { WasCancelled = true; break; }
                if (Input.GetKeyDown(KeyCode.Return)) { break; }
                yield return null;
            }

            if (!WasCancelled)
            {
                if (mode == TargetMode.All) ResultTargets.AddRange(live);
                else ResultTargets.Add(live[index]);
            }

            if (_cursorInstance) _cursorInstance.SetActive(false);
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
    }
}
