using System.Collections.Generic;
using UnityEngine;

public class CardUIManager : MonoBehaviour
{
    [SerializeField] private List<CardDisplay> cardDisplays = new List<CardDisplay>();
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private CardInventory playerInventory;

    private List<ActionCard> currentCards = new List<ActionCard>();
    private int currentCardIndex = 0;

    void Start()
    {
        for (int i = 0; i < cardDisplays.Count; i++)
        {
            int index = i;
            cardDisplays[i].myButton.onClick.AddListener(() => UseCard(index));
        }
    }

    // Display Cards
    public void ShowPlayerCards()
    {
        DisplayCards(playerInventory.cards);
    }

    private void DisplayCards(List<ActionCard> cardsToDisplay)
    {
        currentCards = cardsToDisplay;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        for (int i = 0; i < cardDisplays.Count; i++)
        {
            if (cardDisplays[i] == null)
            {
                Debug.LogError($"{name}: Card Displays slot {i} is empty in the Inspector.", this);
                continue;
            }

            int cardIndex = currentCardIndex + i;
            if (cardIndex >= currentCards.Count) continue;

            ActionCard card = currentCards[cardIndex];
            if (card == null)
            {
                Debug.LogError($"{name}: Card {cardIndex} in {playerInventory.name}'s inventory is missing.", playerInventory);
                continue;
            }

            cardDisplays[i].ChangeDisplay(card.cardName, card.cardDescription, card.points);
        }
    }

    private void UseCard(int displayIndex)
    {
        int cardIndex = currentCardIndex + displayIndex;

        if (cardIndex < currentCards.Count)
        {
            ActionCard card = currentCards[cardIndex];
            card.Play(playerManager);
        }
    }

    public void NextCards()
    {
        currentCardIndex += 3;
        if (currentCardIndex >= currentCards.Count)
            currentCardIndex = 0;

        UpdateDisplay();
    }

    public void PreviousCards()
    {
        currentCardIndex -= 3;
        if (currentCardIndex < 0)
            currentCardIndex = Mathf.Max(0, currentCards.Count - 3);

        UpdateDisplay();
    }
}