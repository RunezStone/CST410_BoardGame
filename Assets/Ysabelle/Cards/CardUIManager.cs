using System.Collections.Generic;
using UnityEngine;

public class CardUIManager : MonoBehaviour
{
    [SerializeField] private List<CardDisplay> cardDisplays = new List<CardDisplay>();
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private CardInventory playerInventory;

    [Tooltip("Tick this on the shared Minion canvas. Cards are then played by whichever minion is taking its turn " +
             "(MinionManager.ActiveMinion), using the ActionCards on that minion (Bite). Player Manager and " +
             "Player Inventory can stay empty. Build it with Convergence > Minions > Build Minion Canvas.")]
    [SerializeField] private bool useActiveMinion = false;

    private List<ActionCard> currentCards = new List<ActionCard>();
    private int currentCardIndex = 0;

    // The character the cards are played for: the fixed player, or the minion whose turn it is.
    private PlayerManager CurrentPlayer =>
        useActiveMinion ? (MinionManager.instance != null ? MinionManager.instance.ActiveMinion : null) : playerManager;

    void Start()
    {
        for (int i = 0; i < cardDisplays.Count; i++)
        {
            int index = i;
            cardDisplays[i].myButton.onClick.AddListener(() => UseCard(index));
        }
    }

    // The minion canvas is shown again at the start of every minion's turn: refresh for the new minion.
    void OnEnable()
    {
        if (useActiveMinion) ShowPlayerCards();
    }

    // Closes the card panel (the "ActionCards" canvas that Use Deck opens).
    // Never hides the canvas this component sits in, or the player's main canvas.
    public void HideCardPanel()
    {
        foreach (CardDisplay display in cardDisplays)
        {
            if (display == null) continue;
            Canvas panel = display.GetComponentInParent<Canvas>(true);
            if (panel == null || transform.IsChildOf(panel.transform)) return;
            if (playerManager != null && playerManager.playerUICanvas == panel) return;
            panel.gameObject.SetActive(false);
            return;
        }
    }

    // Display Cards
    public void ShowPlayerCards()
    {
        if (useActiveMinion)
        {
            PlayerManager minion = CurrentPlayer;
            currentCardIndex = 0;
            DisplayCards(minion != null ? new List<ActionCard>(minion.GetComponents<ActionCard>()) : new List<ActionCard>());
            return;
        }

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

            // Minions have fewer cards than there are slots: hide the empty ones
            // (otherwise they'd keep whatever text the canvas was copied with).
            if (useActiveMinion) cardDisplays[i].gameObject.SetActive(cardIndex < currentCards.Count);

            if (cardIndex >= currentCards.Count) continue;

            ActionCard card = currentCards[cardIndex];
            if (card == null)
            {
                string owner = playerInventory != null ? playerInventory.name : "the active minion";
                Debug.LogError($"{name}: Card {cardIndex} in {owner}'s inventory is missing.", this);
                continue;
            }

            cardDisplays[i].ChangeDisplay(card.cardName, card.cardDescription, card.points);
        }
    }

    private void UseCard(int displayIndex)
    {
        int cardIndex = currentCardIndex + displayIndex;
        if (cardIndex >= currentCards.Count) return;

        PlayerManager player = CurrentPlayer;
        ActionCard card = currentCards[cardIndex];
        if (player == null || card == null) return;

        card.Play(player);
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
