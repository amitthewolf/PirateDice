using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NodeCanvas.BehaviourTrees;
using UnityEngine;

public class AIBrain : MonoBehaviour
{
    public BehaviourTreeOwner BT;
    public float Threshold;
    public float TrustLevel;
    private DiceBoard _diceBoard;
    private int[] DiceTimes;
    public BetMaster BM;
    private DiceBet _newBet;
    public double Solution;
    private List<DiceBet> opponnentBets;
    private List<DiceBet> BetOptions;


    void Start()
    {
        BM = GameMaster.getInstance().BM;
        TrustLevel = 0.5f;
        opponnentBets = new List<DiceBet>();
        BetOptions = new List<DiceBet>();
        BT = GetComponent<BehaviourTreeOwner>();
    }

    public void CheckIfFirst()
    {
        if (BM.CurrBet.Better != null)
            Solution = 1;
        else
            Solution = 0;
    }
    public void CheckPlausability()
    {
       CheckProbForBet(BM.CurrBet);
    }

    public void CallLie()
    {
        BT.enabled = false;
        GetComponent<DicePlayer>().Lie();
    }

    public void StandardBetOptions()
    {
        BetOptions = new List<DiceBet>();
        for (int i = 0; i < 6; i++)
        {
            BetOptions.Add(BM.AutoIncreaseWithValueAndTimes(i + 1, DiceTimes[i]));
        }
        int Random = DiceRoller.getInstance().RollDie();
        if (Random < 4)
            Solution = 1;
        else
            Solution = 0;
    }

    public void MaxBetOptions()
    {
        BetOptions = new List<DiceBet>();
        for (int i = 0; i < 6; i++)
        {
            BetOptions.Add(BM.AutoIncreaseWithValueAndTimes(i + 1, (int)(GameMaster.getInstance().CurrNumOfDice/3)));
        }
        int Random = DiceRoller.getInstance().RollDie();
        if (Random < 4)
            Solution = 1;
        else
            Solution = 0;
    }

    public void DiceRoll()
    {
        int Random = DiceRoller.getInstance().RollDie();
        if (Random < 4)
            Solution = 1;
        else
            Solution = 0;
    }

    public void CheckProbForBet(DiceBet DB)
    {
        if(DB.Better != GetComponent<DicePlayer>())
            opponnentBets.Add(DB);
        if (DB.Times - DiceTimes[DB.Value - 1] < 1)
            Solution = 1;
        else 
            Solution = BinomialProb(GameMaster.getInstance().CurrNumOfDice - GetComponent<DicePlayer>().DiceValues.Count, DB.Times - DiceTimes[DB.Value-1], DB.Value);
    }

    public void NewRound(DiceBoard DB)
    {
        _diceBoard = DB;
        opponnentBets = new List<DiceBet>();
        DiceTimes = _diceBoard.CheckBoardForValue();
    }

    public void EndTurnBet(DiceBet newBet)
    {
        GetComponent<DicePlayer>().endTurnAI(newBet);
        BT.enabled = false;
    }
    public DiceBet LeadingBet()
    {
        DiceBet BestOption = null;
        double bestProb = 1;
        BetOptions.ForEach(x =>
        {
            CheckProbForBet(x);
            if (Solution < bestProb)
            {
                bestProb = Solution;
                BestOption = x;
            }
        });
        return BestOption;
    }

    public DiceBet StandardBet()
    {
        DiceBet BestOption = null;
        double bestProb = 0;
        BetOptions.ForEach(x =>
        {
            CheckProbForBet(x);
            if (Solution > bestProb)
            {
                bestProb = Solution;
                BestOption = x;
            }
        });
        return BestOption;
    }

    private double BinomialProb(int NumOfDice, int _times, int value)
    {
        double Prob = 1.0/3.0;
        if (value == 1)
            Prob = 1.0/6.0;
        double result = 0.0;
        for (int times = _times; times < NumOfDice ; times++)
        {
            result += (NCR(NumOfDice, times)) * Math.Pow(Prob, times) * Math.Pow((1 - Prob), (NumOfDice - times));
        }
        return result;
    }
    private double Factorial(int number)
    {
        if (number < 0)
            return -1; //Error

        double result = 1;

        for (int i = 1; i <= number; ++i)
            result *= i;

        return result;
    }

    public double NCR(int n, int r)
    {
        return Factorial(n) / (Factorial(r) * Factorial(n - r));
    }

    public void AIReachDecision()
    {
        BT.enabled = true;
    }

    void Update()
    {
        Threshold = (float)(0.25 - (0.15 * TrustLevel));
    }

    public void AdjustTrust()
    {
        Debug.Log("Adjusting Trust");
        opponnentBets.ForEach(x =>
        {
            if (x.Better.Checknum(x.Value) >= x.Times)
                TrustLevel += 0.1f ;
            else if (TrustLevel > 0)
                TrustLevel -= 0.05f * (x.Times - x.Better.Checknum(x.Value));
        });
    }
}
