using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class HUDManager : MonoBehaviour
{
    float score_current = 0f;    // Currently displayed score
    float score_target = 0f;     // Target score to lerp toward
    float score_multiplier = 1f;
    
    public TextMeshProUGUI scoreText;

    public TextMeshProUGUI tracker1;
    public TextMeshProUGUI tracker2;
    public TextMeshProUGUI tracker3;

    public TextMeshProUGUI trick_line_text;

    public TextMeshProUGUI slammed_text;

    public float lerpSpeed = 5f; // Adjust this to control animation speed

    // queue will be static size of 3
    List<int> tracker_queue = new List<int>(3) {-1, -1, -1};

    public bool is_in_trick_line = false;
    public int tricks_in_current_line = 0;
    public bool is_slammed = false;
    bool should_reset_points = false;


    void Start()
    {
        scoreText.text = "Score: 0 \n\n Mult: 1x";
        tracker1.text = "trick";
        tracker2.text = "";
        tracker3.text = "";

        trick_line_text.text = "";
        slammed_text.text = "";
    }

    void Update()
    {
        if (is_slammed) {
            slammed_text.text = "SLAMMED";
            should_reset_points = true;
        } else {
            slammed_text.text = "";
        }

        LerpScore();
        UpdateTrackerText();
        UpdateTrickLineText();
        CheckResetPointsOnSlammed();

    }

    void CheckResetPointsOnSlammed()
    {
        if (should_reset_points && !is_slammed)
        {
            should_reset_points = false;
            score_current = 0;
            score_target = 0;
            score_multiplier = 1f;
        }

    }

    void LerpScore()
    {
        // Smoothly lerp current score toward target
        score_current = Mathf.Lerp(score_current, score_target, Time.deltaTime * lerpSpeed);
        
        // Update display
        scoreText.text = "Score: " + score_current.ToString("F0") + 
                         "\n Mult: " + score_multiplier.ToString("F1") + "x";

    }

    void UpdateTrickLineText()
    {
        if (is_in_trick_line)
        {
            trick_line_text.text = "Trick Line: " + tricks_in_current_line.ToString();
        }
        else
        {
            trick_line_text.text = "";
        }
    }

    public void UpdateAddScore(float score)
    {
        // Add to the target score
        score_target += score * score_multiplier;
    }

    public void UpdateScoreMultiplier(float multiplier)
    {
        score_multiplier = multiplier;
    }

    public void AddTrickToQueue(int trick)
    {
        tracker_queue.Insert(0, trick);
        if (tracker_queue.Count > 3)
        {
            tracker_queue.RemoveAt(tracker_queue.Count - 1);
        }
    }

    void UpdateTrackerText()
    {
        tracker1.text = TrickIntToString(tracker_queue[0]);
        tracker2.text = TrickIntToString(tracker_queue[1]);
        tracker3.text = TrickIntToString(tracker_queue[2]);
    }

    string TrickIntToString(int trick)
    {
        switch (trick) {
            case 0:
                return "kickflip";
            case 1:
                return "shuvit";
            case 2:
                return "heelflip";
            case 3:
                return "varial kickflip";
            case 4:
                return "varial heelflip";
            case 5:
                return "50-50";
            case 6:
                return "front board";
            case 7:
                return "back board";
            case 8:
                return "5-0";
            case 9:
                return "nosegrind";
            case 10:
                return "tailslide";
            case 11:
                return "noseslide";
            default:
                return " ";
        }
    }
}