using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameMaster : MonoBehaviour
{
    private static GameMaster instance;
    public BetMaster BM;
    public int MaxNumOfDice = 6;
    public int StartingNumOfDice = 6;
    public int CurrNumOfDice;
    public TMP_Text CurrNumOfDiceUI;
    public List<DicePlayer> Players;
    private int CurrPlayerIndex;
    public DicePlayer CurrDicePlayer;
    private bool TurnEnded = false;
    private bool RoundEnded = false;
    private bool AnnouncementEnded = false;
    public TMP_Text AnnouncmentsH;
    public TMP_Text AnnouncmentsL;
    private int RoundCounter;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    public static GameMaster getInstance()
    {
        return instance;
    }
    // Start is called before the first frame update
    void Start()
    {
        BM = GetComponent<BetMaster>();
        CurrPlayerIndex = 0;
        RoundCounter = 1;
        StartCoroutine(NewRound());
    }

    void Update()
    {
        CurrNumOfDiceUI.text = CurrNumOfDice.ToString();
    }
    IEnumerator NewRound()
    {
        AnnouncementEnded = false;
        RoundEnded = false;
        yield return new WaitForSeconds(1f);
        CountDice();
        Announce("Round " + RoundCounter + "\n Num of dice: " + CurrNumOfDice);
        yield return new WaitUntil(() => CheckifAnnouncementEnded());
        while (!RoundEnded)
        {
            nextPlayer();
            Announce(CurrDicePlayer.PlayerName+"'s Turn");
            yield return new WaitUntil(() => CheckifAnnouncementEnded());
            TurnEnded = false;
            NextTurn();
            yield return new WaitUntil(() => CheckifPlayerEnded());
        }
        RoundCounter += 1;
        BM.ResetBet();
        StartCoroutine(NewRound());
        yield break;
    }

    private void CountDice()
    {
        CurrNumOfDice = 0;
        Players.ForEach(x =>
        {
            x.NewRound();
            CurrNumOfDice += x.getNumofDice();
        });
    }

    private bool CheckifPlayerEnded()
    {
        return TurnEnded;
    }


    private bool CheckifAnnouncementEnded()
    {
        return AnnouncementEnded;
    }
    public void PlayerEndTurn()
    {
        TurnEnded = true;
    }

    public void AnnouncmentEnded()
    {
        AnnouncementEnded = true;
    }

    public  void CheckNum(int NumtoCheck)
    {
        int Overall = 0;
        Players.ForEach(x => Overall += x.Checknum(BM.CurrBet.Value));
    }

    public void Announce(string Announcement)
    {
        AnnouncementEnded = false;
        AnnouncmentsH.text = Announcement;
        AnnouncmentsH.gameObject.SetActive(true);
        AnnouncmentsH.gameObject.GetComponent<TextEffects>().MakeAnnouncement();
    }

    public void AnnounceWarning(string Announcement)
    {
        AnnouncementEnded = false;
        AnnouncmentsL.text = Announcement;
        AnnouncmentsL.gameObject.SetActive(true);
        AnnouncmentsL.gameObject.GetComponent<TextEffects>().MakeAnnouncement();
    }
    public void NextTurn()
    {
        
        if (CurrDicePlayer.IsHuman)
        {
            BM.PlayerBet();
        }
        StartCoroutine(CurrDicePlayer.Turn());
    }


    IEnumerator LieRoutine()
    {
        Announce("Lie Called!");
        yield return new WaitUntil(() => CheckifAnnouncementEnded());
        int currentInstances = 0;
        Players.ForEach(x =>
            {
                x.ShowDice();
                currentInstances += x.Checknum(BM.CurrBet.Value);
            }
        );
        if (currentInstances < BM.CurrBet.Times)
        {
            Announce("Lie Caught!");
            yield return new WaitUntil(() => CheckifAnnouncementEnded());
            BM.CurrBet.Better.RemoveDie();
        }
        else
        {
            Announce("Not a Lie!");
            yield return new WaitUntil(() => CheckifAnnouncementEnded());
            CurrDicePlayer.RemoveDie();
            IncreasePlayerIndex();
        }
        RoundEnded = true;
        PlayerEndTurn();
        yield break;
    }

    IEnumerator BetRoutine(DiceBet newDicebet = null)
    {
        if(newDicebet == null)
            BM.AssignNewDiceBet();
        else
            BM.AssignNewDiceBet(newDicebet);
        Announce("Bet Increased to: "+BM.CurrBet.ToString());
        yield return new WaitUntil(() => CheckifAnnouncementEnded());
        PlayerEndTurn();
        yield break;
    }

    public void LieCalled()
    {
        BM.CloseWindow();
        Players.ForEach(x =>
        {
            if (!x.IsHuman)
                x.gameObject.GetComponent<AIBrain>().AdjustTrust();
        });
        StartCoroutine(LieRoutine());
    }

    public void BetCalled()
    {
        BM.CloseWindow();
        StartCoroutine(BetRoutine());
    }

    public void BetCalled(DiceBet newDicebet)
    {
        BM.CloseWindow();
        StartCoroutine(BetRoutine(newDicebet));
    }

    private void IncreasePlayerIndex()
    {
        CurrPlayerIndex++;
        if (CurrPlayerIndex == Players.Count)
            CurrPlayerIndex = 0;
    }
    private void nextPlayer()
    {
        IncreasePlayerIndex();
        CurrDicePlayer = Players[CurrPlayerIndex];
    }

    public DiceBet GetCurrBet()
    {
        return BM.CurrBet;
    }

}
