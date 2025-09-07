using UnityEngine;
using TMPro;
using System.Collections;

namespace UI
{
    public class DamagePopup : MonoBehaviour
    {
        [SerializeField] private GameObject popupPrefab; // must have TMP_Text

        [Header("Screen-space (recommended)")]
        [SerializeField] private Canvas screenCanvas;               // main UI Canvas (non-WorldSpace)
        [SerializeField] private Vector2 screenOffset = new(0, 40f);

        [Header("World-space (if no canvas assigned)")]
        [SerializeField] private Vector3 worldOffset = new(0, 1.2f, 0);

        [Header("Anim")]
        [SerializeField] private float riseDistance = 40f; // px (screen) or meters (world)
        [SerializeField] private float duration = 0.7f;
        [SerializeField] private AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Colors")]
        [SerializeField] private Color damageColor = Color.white;
        [SerializeField] private Color healColor = new(0.3f, 1f, 0.3f);
        [SerializeField] private Color critColor = new(1f, 0.9f, 0.2f);

        public void Spawn(Vector3 worldPos, int amount, bool isCrit, bool isHeal)
        {
            if (!popupPrefab) return;

            if (screenCanvas && screenCanvas.renderMode != RenderMode.WorldSpace)
            {
                var go = Instantiate(popupPrefab, screenCanvas.transform);
                var rt = go.GetComponent<RectTransform>();
                var cam = screenCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : screenCanvas.worldCamera;

                Vector2 screen = Camera.main ? (Vector2)Camera.main.WorldToScreenPoint(worldPos + worldOffset) : Vector2.zero;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)screenCanvas.transform, screen, cam, out var local);
                rt.anchoredPosition = local + screenOffset;

                var text = go.GetComponentInChildren<TMP_Text>();
                if (text) { text.text = (isHeal ? "+" : "") + amount; text.color = isCrit ? critColor : (isHeal ? healColor : damageColor); }

                StartCoroutine(AnimateScreen(go, rt));
            }
            else
            {
                var go = Instantiate(popupPrefab, worldPos + worldOffset, Quaternion.identity);
                var text = go.GetComponentInChildren<TMP_Text>();
                if (text) { text.text = (isHeal ? "+" : "") + amount; text.color = isCrit ? critColor : (isHeal ? healColor : damageColor); }
                StartCoroutine(AnimateWorld(go));
            }
        }

        private IEnumerator AnimateScreen(GameObject go, RectTransform rt)
        {
            Vector2 start = rt.anchoredPosition;
            Vector2 end = start + Vector2.up * riseDistance;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / duration);
                rt.anchoredPosition = Vector2.LerpUnclamped(start, end, riseCurve.Evaluate(a));
                yield return null;
            }
            Destroy(go);
        }

        private IEnumerator AnimateWorld(GameObject go)
        {
            Vector3 start = go.transform.position;
            Vector3 end = start + Vector3.up * (riseDistance * 0.01f);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / duration);
                go.transform.position = Vector3.LerpUnclamped(start, end, riseCurve.Evaluate(a));
                yield return null;
            }
            Destroy(go);
        }
    }
}
