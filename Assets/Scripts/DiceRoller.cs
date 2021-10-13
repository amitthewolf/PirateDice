using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class DiceRoller
{
    private Random Generator;
    private static DiceRoller instance;

    public static DiceRoller getInstance()
    {
        if (instance == null)
            instance = new DiceRoller();
        return instance;
    }

    public DiceRoller()
    {
        Generator = new Random();
    }

    public int RollDie()
    {
        return Generator.Next(1, 7);
    }
}
