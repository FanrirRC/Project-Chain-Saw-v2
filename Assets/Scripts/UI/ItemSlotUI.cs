using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Button button;

    private Data.ItemDefinition _item;
    private Action<Data.ItemDefinition> _onPick;

    public void Bind(Data.ItemDefinition item, int count, Action<Data.ItemDefinition> onPick)
    {
        _item = item;
        _onPick = onPick;

        if (iconImage) iconImage.sprite = item.icon;
        if (nameText) nameText.text = item.displayName;
        if (countText) countText.text = $"x{count}";

        bool available = count > 0;
        if (button)
        {
            button.interactable = available;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onPick?.Invoke(_item));
        }
        if (iconImage) iconImage.color = available ? Color.white : new Color(1, 1, 1, 0.35f);
    }
}
