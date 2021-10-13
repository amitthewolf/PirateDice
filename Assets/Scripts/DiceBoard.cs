using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

public class DiceBoard
{
    private int NumOfDice;
    private List<int> DiceValues;
    public bool PlayerActive;
    public DiceBoard()
    {

        DiceValues = new List<int>();
        NumOfDice = GameMaster.getInstance().StartingNumOfDice;
        PlayerActive = true;
        Roll();
    }

    public List<int> getValues()
    {
        return new List<int>(DiceValues);
    }

    public void Roll()
    {
        DiceValues = new List<int>();
        for (int i = 0; i < NumOfDice; i++)
        {
            DiceValues.Add(DiceRoller.getInstance().RollDie());
        }
    }


    public int CheckNum(int NumToCheck)
    {
        int Counter = 0;
        DiceValues.ForEach(DiceValue =>
        {
            if (DiceValue == NumToCheck || DiceValue == 1)
            {
                Counter++;
            }
        });
        return Counter;
    }

    public int[] CheckBoardForValue()
    {
        int[] Values = new int[6];
        int counter = 0;
        getValues().ForEach(x =>
        {
            Values[counter] = CheckNum(counter + 1);
            counter++;
        });
        return Values;
    }

    public int getNumofDice()
    {
        return NumOfDice;
    }
    public void RemoveDie()
    {
        Debug.Log("Removed Die");
        NumOfDice--;
        if (NumOfDice == 0)
            PlayerActive = false;
    }

    public void AddDie()
    {
        if (NumOfDice < GameMaster.getInstance().MaxNumOfDice)
            NumOfDice++;
    }
}
