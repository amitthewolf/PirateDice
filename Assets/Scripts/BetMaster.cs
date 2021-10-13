using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BetMaster : MonoBehaviour
{
    public GameObject BetWindow;
    public TMP_Text TimesCurrentText;
    public TMP_Text ValueCurrentText;
    public TMP_Text TimesBetText;
    public TMP_Text ValueBetText;
    public DiceBet CurrBet;
    public int TimesNew;
    public int ValueNew;
    public bool NewbetValid;

    void Start()
    {
        CurrBet = new DiceBet(0, 0);
        TimesNew = 1;
        ValueNew = 2;
    }

    public void ResetBet()
    {
        CurrBet = new DiceBet(0, 0);
    }


    void Update()
    {
        TimesCurrentText.text = CurrBet.Times.ToString();
        ValueCurrentText.text = CurrBet.Value.ToString();
        TimesBetText.text = TimesNew.ToString();
        ValueBetText.text = ValueNew.ToString();
        if (new DiceBet(TimesNew, ValueNew, GameMaster.getInstance().CurrDicePlayer) > CurrBet)
        {
            TimesBetText.color = Color.black;
            ValueBetText.color = Color.black;
            NewbetValid = true;
        }
        else
        {
            TimesBetText.color = Color.red;
            ValueBetText.color = Color.red;
            NewbetValid = false;
        }
    }

    public void CloseWindow()
    {
        BetWindow.SetActive(false);
    }

    public void AssignNewDiceBet()
    {
        BetWindow.SetActive(false);
        CurrBet = new DiceBet(TimesNew, ValueNew, GameMaster.getInstance().CurrDicePlayer);
    }

    public void AssignNewDiceBet(DiceBet newDB)
    {
        BetWindow.SetActive(false);
        if (newDB == null)
            return;
        CurrBet = newDB;
    }

    public void PlayerBet()
    {
        if (CurrBet.Value == 0)
        {
            TimesNew = 1;
            ValueNew = 2;
        }
        else
        {
            TimesNew = CurrBet.Times+1;
            ValueNew = CurrBet.Value;
        }
        BetWindow.SetActive(true);
    }


    public void IncreaseTimes()
    {
        if(TimesNew < GameMaster.getInstance().CurrNumOfDice)
            TimesNew += 1;
    }

    public void DecreaseTimes()
    {
        if (TimesNew > 1)
            TimesNew -= 1;
    }

    public DiceBet AutoIncreaseWithValue(int value)
    {
        TimesNew = CurrBet.Times;
        ValueNew = value;
        while (!(new DiceBet(TimesNew, ValueNew, GameMaster.getInstance().CurrDicePlayer) > CurrBet))
        {
            if (TimesNew == GameMaster.getInstance().CurrNumOfDice)
                return null;
            IncreaseTimes();
        }
        DiceBet newBet = new DiceBet(TimesNew, ValueNew, GameMaster.getInstance().CurrDicePlayer);
        return newBet;
    }

    public DiceBet AutoIncreaseWithValueAndTimes(int value, int times)
    {
        TimesNew = times;
        ValueNew = value;
        while (!(new DiceBet(TimesNew, ValueNew, GameMaster.getInstance().CurrDicePlayer) > CurrBet))
        {
            if (TimesNew == GameMaster.getInstance().CurrNumOfDice)
                return null;
            IncreaseTimes();
        }
        DiceBet newBet = new DiceBet(TimesNew, ValueNew, GameMaster.getInstance().CurrDicePlayer);
        return newBet;
    }



    public void IncreaseValue()
    {
        if (ValueNew < 6)
            ValueNew += 1;
        else if ( ValueNew == 6)
        {
            ValueNew = 1;
        }
    }

    public void DecreaseValue()
    {
        if(ValueNew == 1)
         ValueNew = 6;
        else
        {
            ValueNew -= 1;
        }
    }
   
}
