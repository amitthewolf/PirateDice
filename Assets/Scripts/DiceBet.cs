using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceBet
{
    public int Times { get; set; }
    public int Value { get; set; }
    public DicePlayer Better { get; set; }
    public DiceBet(int t, int v, DicePlayer player)
    {
        Times = t;
        Value = v;
        Better = player;
    }

    public DiceBet(int t, int v)
    {
        Times = t;
        Value = v;
        Better = null;
    }

    public override string ToString()
    {
        return "Times:"+Times+" | Value:"+Value;
    }

    public static bool operator >(DiceBet NewBet, DiceBet LastBet)
    {
        if (NewBet.Times == 0 || NewBet.Value == 0)
            return false;
        if (LastBet is null || NewBet is null)
            return false;
        if (LastBet.Better == null)
            return true;
        if (LastBet.Value == 1)
            if(NewBet.Value != 1 && NewBet.Times >= LastBet.Times * 2)
                return true;
            else if(NewBet.Value == 1 && NewBet.Times > LastBet.Times)
                return true;
            else
                return false;
        else if (LastBet.Value != 1 && NewBet.Value == 1 && NewBet.Times * 2 > LastBet.Times)
            return true;
        else if (LastBet.Value < NewBet.Value && NewBet.Times >= LastBet.Times)
            return true;
        else if (LastBet.Value >= NewBet.Value && NewBet.Times > LastBet.Times)
            return true;
        return false;
    }

    public static bool operator <(DiceBet LastBet, DiceBet NewBet)
    {
        return false;
    }
}
