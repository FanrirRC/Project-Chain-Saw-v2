using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class TurnOrderBar : MonoBehaviour
    {
        [SerializeField] private HorizontalLayoutGroup container;
        [SerializeField] private GameObject iconPrefab;

        [Header("Visuals")]
        [SerializeField] private Sprite fallbackSprite;
        [SerializeField] private bool showActiveFirst = true;

        [Header("Fallback Colors (if no portrait)")]
        [SerializeField] private Color playerColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color enemyColor = new Color(0.2f, 1f, 0.2f);

        private TurnOrderController _turn;
        private CharacterScript _active;

        public void Bind(TurnOrderController turn)
        {
            if (_turn != null) _turn.ForecastChanged -= Redraw;
            _turn = turn;
            if (_turn != null) _turn.ForecastChanged += Redraw;
            Redraw();
        }

        public void SetActive(CharacterScript active)
        {
            _active = active;
            Redraw();
        }

        private void OnDestroy()
        {
            if (_turn != null) _turn.ForecastChanged -= Redraw;
        }

        private void Redraw()
        {
            if (!container) return;

            foreach (Transform c in container.transform) Destroy(c.gameObject);

            if (showActiveFirst && _active != null)
                CreateIcon(_active, true);

            if (_turn == null || _turn.Forecast == null) return;
            foreach (var u in _turn.Forecast)
                CreateIcon(u, false);
        }

        private void CreateIcon(CharacterScript unit, bool isActive)
        {
            if (!unit) return;
            var go = Instantiate(iconPrefab, container.transform);
            var img = go.GetComponentInChildren<Image>();
            if (img)
            {
                var portrait = unit.GetPortrait();
                if (portrait)
                {
                    img.sprite = portrait;
                    img.color = Color.white;
                    img.preserveAspect = true;
                }
                else
                {
                    img.sprite = fallbackSprite;
                    img.color = unit.IsEnemy ? enemyColor : playerColor;
                }
            }

            if (isActive) go.transform.localScale = Vector3.one * 1.1f;
        }
    }
}
