using System.Collections.Generic;
using UnityEngine;

public class CardSelectionManager : MonoBehaviour
{
    [Header("Card Database")]
    [SerializeField] private List<CardData> availableCards = new List<CardData>();

    [Header("Card UI")]
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private CardUI[] cardSlots = new CardUI[3];

    [Header("Selection Settings")]
    [SerializeField] private int hitsPerSelection = 2;
    [SerializeField] private bool pauseGameWhileSelecting = true;

    [Header("Effect Manager")]
    [SerializeField] private CardEffectManager effectManager;

    private int playerHitCount;
    private float previousTimeScale = 1f;
    private bool isSelecting;

    // Cards the player has already picked.
    private List<CardData> ownedCards = new List<CardData>();

    private void Start()
    {
        selectionPanel.SetActive(false);
    }

    public void RegisterPlayerHit()
    {
        if (isSelecting)
            return;

        playerHitCount++;

        if (playerHitCount >= hitsPerSelection)
        {
            playerHitCount = 0;
            ShowCardChoices();
        }
    }

    private void ShowCardChoices()
    {
        if (availableCards.Count < cardSlots.Length)
        {
            return;
        }

        // Make a temporary copy so the same card can't appear twice.
        List<CardData> randomPool = new List<CardData>(availableCards);

        if (effectManager != null)
            effectManager.ClearSelectionEffects();

        for (int i = 0; i < cardSlots.Length; i++)
        {
            int randomIndex = Random.Range(0, randomPool.Count);
            CardData chosenCard = randomPool[randomIndex];

            randomPool.RemoveAt(randomIndex);

            cardSlots[i].Setup(chosenCard, SelectCard);
        }

        isSelecting = true;
        selectionPanel.SetActive(true);

        if (pauseGameWhileSelecting)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
    }

    private void SelectCard(CardData selectedCard)
    {
        if (!isSelecting)
            return;

        ownedCards.Add(selectedCard);
        effectManager.ApplyCard(selectedCard);

        selectionPanel.SetActive(false);
        isSelecting = false;

        if (pauseGameWhileSelecting)
            Time.timeScale = previousTimeScale;
    }
}