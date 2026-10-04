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
            int cardIndex = currentCardIndex + i;

            if (cardIndex < currentCards.Count)
            {
                ActionCard card = currentCards[cardIndex];
                cardDisplays[i].ChangeDisplay(card.cardName, card.cardDescription, card.points);
            }
        }
    }

    private void UseCard(int displayIndex)
    {
        int cardIndex = currentCardIndex + displayIndex;

        if (cardIndex < currentCards.Count)
        {
            ActionCard card = currentCards[cardIndex];
            card.InitiateAction(playerManager);
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