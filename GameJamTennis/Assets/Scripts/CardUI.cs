using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image artworkImage;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button button;

    private CardData currentCard;
    private Action<CardData> onSelected;

    public void Setup(CardData card, Action<CardData> selectionCallback)
    {
        currentCard = card;
        onSelected = selectionCallback;

        descriptionText.text = card.description;
        artworkImage.sprite = card.artwork;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(SelectCard);
    }

    private void SelectCard()
    {
        if (currentCard != null)
            onSelected?.Invoke(currentCard);
    }
}