using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button button;

    private Data.SkillDefinition _skill;
    private CharacterScript _owner;
    private Action<Data.SkillDefinition> _onPick;

    public void Bind(CharacterScript owner, Data.SkillDefinition skill, Action<Data.SkillDefinition> onPick)
    {
        _owner = owner;
        _skill = skill;
        _onPick = onPick;

        if (iconImage) iconImage.sprite = skill.icon;
        if (nameText) nameText.text = skill.displayName;
        if (costText) costText.text = skill.spCost > 0 ? $"SP {skill.spCost}" : "Free";

        bool canAfford = owner.currentSP >= skill.spCost;
        if (button)
        {
            button.interactable = canAfford;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onPick?.Invoke(_skill));
        }
        if (iconImage) iconImage.color = canAfford ? Color.white : new Color(1, 1, 1, 0.35f);
    }
}
