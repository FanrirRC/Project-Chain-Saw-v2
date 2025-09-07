using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace UI
{
    public class BattleHUD : MonoBehaviour
    {
        [System.Serializable]
        public class Panel
        {
            public CharacterScript unit;

            [Header("Basics")]
            public TMP_Text nameText;
            public TMP_Text hpText;
            public TMP_Text spText;

            [Header("HP Fillers (pick one)")]
            public Image hpFillImage;
            public Slider hpSlider;

            [Header("SP Fillers (pick one)")]
            public Image spFillImage;
            public Slider spSlider;

            [Header("Highlight")]
            public GameObject turnHighlight;
            public RectTransform infoTransform;

            [Header("Status Strip (optional)")]
            public RectTransform statusBar;
            public GameObject statusIconPrefab;

            [HideInInspector] public Vector2 baseInfoAnchoredPos;
        }

        [SerializeField] public List<Panel> panels = new();

        [Header("Active Info Offset")]
        [Tooltip("How far to nudge the panel's Info RectTransform when the unit is active.")]
        [SerializeField] private float activeInfoOffsetX = 11.125f;
        [SerializeField] private float activeInfoOffsetY = 11.125f;

        private bool _cachedLayout = false;

        public void Bind(IReadOnlyList<CharacterScript> players)
        {
            for (int i = 0; i < panels.Count; i++)
            {
                var p = panels[i];
                p.unit = (players != null && i < players.Count) ? players[i] : null;

                if (!_cachedLayout && p.infoTransform)
                    p.baseInfoAnchoredPos = p.infoTransform.anchoredPosition;

                UpdatePanel(p);
            }
            _cachedLayout = true;
        }

        public void RefreshAll()
        {
            foreach (var p in panels) if (p != null) UpdatePanel(p);
        }

        public void SetActiveUnit(CharacterScript active)
        {
            foreach (var p in panels)
            {
                if (p == null) continue;

                if (p.infoTransform)
                {
                    var pos = p.baseInfoAnchoredPos;
                    if (p.unit == active)
                        pos += new Vector2(activeInfoOffsetX, activeInfoOffsetY);
                    p.infoTransform.anchoredPosition = pos;
                }

                if (p.turnHighlight)
                    p.turnHighlight.SetActive(p.unit == active);
            }
        }

        public void Highlight(CharacterScript active) => SetActiveUnit(active);


        private void UpdatePanel(Panel p)
        {
            if (p == null)
                return;

            if (!p.unit)
            {
                if (p.nameText) p.nameText.text = "-";
                if (p.hpText) p.hpText.text = "HP:--/--";
                if (p.spText) p.spText.text = "SP:--/--";
                SetHPFill(p, 0, 0);
                SetSPFill(p, 0, 0);
                ClearStatusIcons(p);
                return;
            }

            if (p.nameText) p.nameText.text = p.unit.characterName;
            if (p.hpText) p.hpText.text = $"HP:{p.unit.currentHP}/{p.unit.maxHP}";
            if (p.spText) p.spText.text = $"SP:{p.unit.currentSP}/{p.unit.maxSP}";

            SetHPFill(p, p.unit.currentHP, p.unit.maxHP);
            SetSPFill(p, p.unit.currentSP, p.unit.maxSP);

            DrawStatusIcons(p);
        }

        private void SetHPFill(Panel p, int cur, int max)
        {
            float ratio = (max <= 0) ? 0f : Mathf.Clamp01(cur / (float)max);

            if (p.hpFillImage)
                p.hpFillImage.fillAmount = ratio;

            if (p.hpSlider)
            {
                if (!Mathf.Approximately(p.hpSlider.maxValue, max))
                    p.hpSlider.maxValue = Mathf.Max(1, max);
                p.hpSlider.value = Mathf.Clamp(cur, 0, max);
            }
        }

        private void SetSPFill(Panel p, int cur, int max)
        {
            float ratio = (max <= 0) ? 0f : Mathf.Clamp01(cur / (float)max);

            if (p.spFillImage)
                p.spFillImage.fillAmount = ratio;

            if (p.spSlider)
            {
                if (!Mathf.Approximately(p.spSlider.maxValue, max))
                    p.spSlider.maxValue = Mathf.Max(1, max);
                p.spSlider.value = Mathf.Clamp(cur, 0, max);
            }
        }

        private void ClearStatusIcons(Panel p)
        {
            if (!p.statusBar) return;
            for (int i = p.statusBar.childCount - 1; i >= 0; i--)
                Destroy(p.statusBar.GetChild(i).gameObject);
        }

        private void DrawStatusIcons(Panel p)
        {
            if (!p.statusBar || !p.statusIconPrefab || p.unit == null) return;

            ClearStatusIcons(p);

            foreach (var s in p.unit.activeStatusEffects)
            {
                if (s.effect == null || s.effect.icon == null) continue;

                var go = Instantiate(p.statusIconPrefab);
                go.transform.SetParent(p.statusBar, false);
                go.transform.SetAsFirstSibling();

                var img = go.GetComponentInChildren<Image>();
                if (img) img.sprite = s.effect.icon;

                var tmp = go.GetComponentInChildren<TMP_Text>();
                if (tmp) tmp.text = s.remainingTurns.ToString();
            }
        }
    }
}
