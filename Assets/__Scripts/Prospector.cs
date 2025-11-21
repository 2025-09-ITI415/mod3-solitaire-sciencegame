using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Deck))]
[RequireComponent(typeof(JsonParseLayout))]
public class Prospector : MonoBehaviour
{
    private static Prospector S; // Private Singleton

    [Header("Dynamic")]
    public List<CardProspector> drawPile;
    public List<CardProspector> discardPile;
    public List<CardProspector> mine;
    public CardProspector target;

    private Transform layoutAnchor;

    private Deck deck;
    private JsonLayout jsonLayout;

    // A Dictionary to pair mine layout IDs and actual Cards (still kept, though Golf 不再用覆盖)
    private Dictionary<int, CardProspector> mineIdToCardDict;


    void Start()
    {
        // Set the private Singleton.
        if (S != null) Debug.LogError("Attempted to set S more than once!");
        S = this;

        jsonLayout = GetComponent<JsonParseLayout>().layout;

        deck = GetComponent<Deck>();
        deck.InitDeck();
        Deck.Shuffle(ref deck.cards);

        drawPile = ConvertCardsToCardProspectors(deck.cards);

        LayoutMine();

        MoveToTarget(Draw());
        UpdateDrawPile();
    }


    List<CardProspector> ConvertCardsToCardProspectors(List<Card> listCard)
    {
        List<CardProspector> listCP = new List<CardProspector>();
        CardProspector cp;
        foreach (Card card in listCard)
        {
            cp = card as CardProspector;
            listCP.Add(cp);
        }
        return listCP;
    }


    CardProspector Draw()
    {
        CardProspector cp = drawPile[0];
        drawPile.RemoveAt(0);
        return cp;
    }

    /// <summary>
    /// Positions the initial tableau of cards, a.k.a. the "mine"
    /// For Golf Solitaire：所有 tableau 牌一开始就 faceUp.
    /// </summary>
    void LayoutMine()
    {
        // Create an empty GameObject to serve as an anchor for the tableau
        if (layoutAnchor == null)
        {

            GameObject tGO = new GameObject("_LayoutAnchor");
            layoutAnchor = tGO.transform;
        }

        CardProspector cp;

        // Dictionary for mine layout ID to CardProspector (保留，方便扩展)
        mineIdToCardDict = new Dictionary<int, CardProspector>();
        mine = new List<CardProspector>();


        // Iterate through the JsonLayoutSlots pulled from the JSON_Layout
        foreach (JsonLayoutSlot slot in jsonLayout.slots)
        {
            cp = Draw(); // Pull a card from the top of the drawPile

            // Golf：tableau 牌全部面朝上，不再依赖 slot.faceUp / hiddenBy
            cp.faceUp = true;

            // Make the CardProspector a child of layoutAnchor
            cp.transform.SetParent(layoutAnchor);

            // Convert the last char of the layer string to an int (e.g. "Row0")
            int z = int.Parse(slot.layer[slot.layer.Length - 1].ToString());

            // Set the localPosition of the card based on the slot information
            cp.SetLocalPos(new Vector3(
                jsonLayout.multiplier.x * slot.x,
                jsonLayout.multiplier.y * slot.y,
                -z));

            cp.layoutID = slot.id;
            cp.layoutSlot = slot;
            // CardProspectors in the mine have the state CardState.mine
            cp.state = eCardState.mine;

            // Set the sorting layer of all SpriteRenderers on the Card
            cp.SetSpriteSortingLayer(slot.layer);

            mine.Add(cp);
            mineIdToCardDict.Add(slot.id, cp);
        }
    }

    /// <summary>
    /// Moves the current target card to the discardPile
    /// </summary>
    /// 
    void MoveToDiscard(CardProspector cp)
    {

        cp.state = eCardState.discard;
        discardPile.Add(cp);
        cp.transform.SetParent(layoutAnchor);

        cp.SetLocalPos(new Vector3(
            jsonLayout.multiplier.x * jsonLayout.discardPile.x,
            jsonLayout.multiplier.y * jsonLayout.discardPile.y,
            0));

        cp.faceUp = true;

        // Place it on top of the pile for depth sorting
        cp.SetSpriteSortingLayer(jsonLayout.discardPile.layer);
        cp.SetSortingOrder(-200 + (discardPile.Count * 3));
    }

    /// <summary>
    /// Make cp the new target card
    /// </summary>
    /// 
    void MoveToTarget(CardProspector cp)
    {
        // If there is currently a target card, move it to discardPile
        if (target != null) MoveToDiscard(target);

        // Use MoveToDiscard to move the target card to the correct location
        MoveToDiscard(cp);

        // Then set a few additional things to make cp the new target
        target = cp;
        cp.state = eCardState.target;

        // Set the depth sorting so that cp is on top of the discardPile
        cp.SetSpriteSortingLayer("Target");
        cp.SetSortingOrder(0);
    }

    /// <summary>
    /// Arranges all the cards of the drawPile to show how many are left
    /// </summary>
    void UpdateDrawPile()
    {
        CardProspector cp;

        for (int i = 0; i < drawPile.Count; i++)
        {
            cp = drawPile[i];
            cp.transform.SetParent(layoutAnchor);


            Vector3 cpPos = new Vector3();
            cpPos.x = jsonLayout.multiplier.x * jsonLayout.drawPile.x;

            cpPos.x += jsonLayout.drawPile.xStagger * i;
            cpPos.y = jsonLayout.multiplier.y * jsonLayout.drawPile.y;
            cpPos.z = 0.1f * i;
            cp.SetLocalPos(cpPos);

            cp.faceUp = false; // DrawPile Cards are all face-down
            cp.state = eCardState.drawpile;
            // Set depth sorting
            cp.SetSpriteSortingLayer(jsonLayout.drawPile.layer);
            cp.SetSortingOrder(-10 * i);
        }
    }

    /// <summary>
    /// For Golf Solitaire：所有 mine 中牌始终 faceUp，
    /// 所以此函数只确保它们都保持为 true。
    /// </summary>
    public void SetMineFaceUps()
    {
        if (mine == null) return;
        foreach (CardProspector cp in mine)
        {
            if (cp != null)
            {
                cp.faceUp = true;
            }

        }
    }

    /// <summary>
    /// Test whether the game is over
    /// </summary>
    void CheckForGameOver()
    {
        // If the mine is empty, the game is over (win)
        if (mine.Count == 0)
        {
            GameOver(true);
            return;
        }

        // If there are still cards in the mine & draw pile, the game’s not over
        if (drawPile.Count > 0) return;

        // Check for remaining valid plays
        foreach (CardProspector cp in mine)
        {
            // If there is a valid play, the game’s not over
            if (target.AdjacentTo(cp)) return;
        }

        // Since there are no valid plays, the game is over (loss)
        GameOver(false);
    }

    /// <summary>
    /// Called when the game is over.
    /// </summary>
    
    void GameOver(bool won)
    {
        if (won)
        {

            ScoreManager.TALLY(eScoreEvent.gameWin);
        }
        else
        {

            ScoreManager.TALLY(eScoreEvent.gameLoss);
        }

        // Reset the CardSpritesSO singleton to null
        CardSpritesSO.RESET();
        // Reload the scene, resetting the game

        SceneManager.LoadScene("__Prospector_Scene_0");
    }

    /// <summary>
    /// Handler for any time a card in the game is clicked
    /// </summary>
    public static void CARD_CLICKED(CardProspector cp)
    {
        // The reaction is determined by the state of the clicked card
        switch (cp.state)
        {
            case eCardState.target:
                // Clicking the target card does nothing
                break;

            case eCardState.drawpile:
                // Clicking *any* card in the drawPile will draw the next card
                S.MoveToTarget(S.Draw());
                S.UpdateDrawPile();
                ScoreManager.TALLY(eScoreEvent.draw);
                break;

            case eCardState.mine:
                // Clicking a card in the mine will check if it’s a valid play
                bool validMatch = true;

                // If the card is face-down, it’s not valid
                if (!cp.faceUp) validMatch = false;

                // If it’s not an adjacent rank, it’s not valid
                if (!cp.AdjacentTo(S.target)) validMatch = false;

                if (validMatch)
                {
                    S.mine.Remove(cp);
                    S.MoveToTarget(cp);

                    // Golf: 所有 tableau 牌始终是 faceUp，此处调用只是确保状态统一
                    S.SetMineFaceUps();

                    ScoreManager.TALLY(eScoreEvent.mine);
                }
                break;
        }
        S.CheckForGameOver();

    }

}
