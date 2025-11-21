using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// An enum to handle all the possible scoring events
public enum eScoreEvent
{
    draw,
    mine,
    gameWin,
    gameLoss
}

// ScoreManager handles all of the scoring
public class ScoreManager : MonoBehaviour
{
    static private ScoreManager S;

    static public int SCORE_FROM_PREV_ROUND = 0;
    static public int SCORE_THIS_ROUND = 0;
    static public int HIGH_SCORE = 0;

    [Header("Inscribed")]
    [Tooltip("If true, then score events are logged to the Console.")]
    public bool logScoreEvents = true;

    [Header("Dynamic")]
    public int chain = 0;
    public int scoreRun = 0;
    public int score = 0;

    [Header("Check this box to reset the ProspectorHighScore to 100")]
    public bool checkToResetHighScore = false;

    void Awake()
    {
        if (S != null) Debug.LogError("ScoreManager.S is already set!");
        S = this;

        // Check for a high score in PlayerPrefs
        if (PlayerPrefs.HasKey("ProspectorHighScore"))
        {
            HIGH_SCORE = PlayerPrefs.GetInt("ProspectorHighScore");
        }
        // Add the score from last round, which will not be 0 if it was a win
        score += SCORE_FROM_PREV_ROUND;
        // And reset SCORE_THIS_ROUND to 0 (it is not used until GameOver(win))
        SCORE_THIS_ROUND = 0;
    }

    /// <summary>
    /// This static method enables any other class to post an eScoreEvent.
    /// </summary>
    public static void TALLY(eScoreEvent evt)
    {
        S.Tally(evt);
    }

    /// <summary>
    /// Handle eScoreEvents (mostly sent by the Prospector/Golf class).
    /// </summary>
    void Tally(eScoreEvent evt)
    {
        switch (evt)
        {
            case eScoreEvent.mine:     // Remove a mine card
                chain++;
                scoreRun += chain;
                break;

            case eScoreEvent.draw:
            case eScoreEvent.gameWin:
            case eScoreEvent.gameLoss:
                chain = 0;
                score += scoreRun;
                scoreRun = 0;
                break;
        }

        string scoreStr = score.ToString("#,##0");
        switch (evt)
        {
            case eScoreEvent.gameWin:
                SCORE_THIS_ROUND = score - SCORE_FROM_PREV_ROUND;
                Log($"You won this round! Round score: {SCORE_THIS_ROUND}");

                SCORE_FROM_PREV_ROUND = score;

                if (HIGH_SCORE <= score)
                {
                    Log($"Game Win. Your new high score was: {scoreStr}");
                    HIGH_SCORE = score;
                    PlayerPrefs.SetInt("ProspectorHighScore", score);
                }
                break;

            case eScoreEvent.gameLoss:
                if (HIGH_SCORE <= score)
                {
                    Log($"Game Over. Your new high score was: {scoreStr}");
                    HIGH_SCORE = score;
                    PlayerPrefs.SetInt("ProspectorHighScore", score);
                }
                else
                {
                    Log($"Game Over. Your final score was: {scoreStr}");
                }
                SCORE_FROM_PREV_ROUND = 0;
                break;

            default:
                Log($"score:{scoreStr}  scoreRun:{scoreRun}  chain:{chain}");
                break;
        }
    }

    void Log(string str)
    {
        if (logScoreEvents) Debug.Log(str);
    }

    void OnDrawGizmos()
    {
        if (checkToResetHighScore)
        {
            checkToResetHighScore = false;
            PlayerPrefs.SetInt("ProspectorHighScore", 100);
            Debug.LogWarning("PlayerPrefs.ProspectorHighScore reset to 100!");
        }
    }

    // These static properties allow other classes to get ScoreManager’s state
    public static int CHAIN { get { return S.chain; } }
    public static int SCORE { get { return S.score; } }
    public static int SCORE_RUN { get { return S.scoreRun; } }
}
