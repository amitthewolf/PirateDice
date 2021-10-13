using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DicePlayer : MonoBehaviour
{
    private DiceBoard DiceBoard;
    public String PlayerName;
    public bool IsHuman;
    public List<TMP_Text> DiceValues;
    private bool HideDice;
    private bool TurnEnded;
    public GameObject DiceInUI;
    //Buttons
    public List<Button> Buttons;

    public int getNumofDice()
    {
        return DiceBoard.getNumofDice();
    }

    void Start()
    {
        Buttons.ForEach(x => x.interactable = false);
        DiceBoard = new DiceBoard();
        Debug.Log("Rolled");
        HideDice = false;
        if(!IsHuman)
            DiceInUI.SetActive(false);
    }

    public void ShowDice()
    {
        if (!IsHuman)
            DiceInUI.SetActive(true);
    }

    public IEnumerator Turn()
    {
        TurnEnded = false;
        if (IsHuman)
        {
            Debug.Log("Turn started");
            EnableButtons();
        }
        else
        {
            ReachDesicion();
        }

        while (!TurnEnded)
        {
            yield return new WaitForSeconds(0.5f);
        }
        Debug.Log("Turn Ended");
        yield break;
    }

    private void EnableButtons()
    {
        if (GameMaster.getInstance().GetCurrBet().Better == null)
            Buttons[1].interactable = true;
        else
            Buttons.ForEach(x => x.interactable = true);
    }


    public void ReachDesicion()
    {
        if (!IsHuman)
        {
            AIBrain brain = GetComponent<AIBrain>();
            brain.AIReachDecision();
        }
    }
    public void NewRound()
    {
        if (!IsHuman)
            DiceInUI.SetActive(false);
        DiceBoard.Roll();
        if (!IsHuman)
            GetComponent<AIBrain>().NewRound(this.DiceBoard);
    }


    public void RemoveDie()
    {
        DiceBoard.RemoveDie();
    }
    public void Lie()
    {
        Buttons.ForEach(x => x.interactable = false);
        GameMaster.getInstance().LieCalled();
        TurnEnded = true;
    }

    public int Checknum(int NumToCheck)
    {
        return DiceBoard.CheckNum(NumToCheck);
    }
    public void endTurnBet()
    {
        if (GameMaster.getInstance().BM.NewbetValid)
        {
            Buttons.ForEach(x => x.interactable = false);
            TurnEnded = true;
            GameMaster.getInstance().BetCalled();
        }
        else
        {
            GameMaster.getInstance().AnnounceWarning("Bet not valid! \n Must increase bet!");
            return;
        }
    }


    public void endTurnAI(DiceBet NewBet)
    {
        TurnEnded = true;
        GameMaster.getInstance().BetCalled(NewBet);
    }
    void Update()
    {
        if (!HideDice)
        {
            Debug.Log("Showing");
            int index = 0;
            DiceBoard.getValues().ForEach(x =>
            {
                DiceValues[index].text = x.ToString();
                index++;
            });
            while (index < 6)
            {
                DiceValues[index].text = "";
                index++;
            }
        }
    }
}
