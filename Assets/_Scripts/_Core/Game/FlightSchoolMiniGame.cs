using UnityEngine;

public class FlightSchoolMiniGame : MiniGame
{
    [SerializeField] Crystal Crystal;
    [SerializeField] Vector3 CrystalStartPosition;
    [SerializeField] Vector3 CrystalStartScale = Vector3.one;
    
    protected override void Start()
    {
        base.Start();

        // TODO: THIS IS A CLUDGE -- maybe not if it exists in the MiniGame base script and accounts for players up to max
        Players[0].Team = Teams.Green;
        Players[1].Team = Teams.Red;
        Players[0].Ship.DisableSkimmer();
        Players[1].Ship.DisableSkimmer();
    }

    protected override void Update()
    {
        base.Update();

        if (!gameRunning) return;

        // TODO: pull this out into an "EliminationMonitor" class
        // if any volume was destroyed, there must have been a collision
        if (StatsManager.Instance.playerStats.ContainsKey(ActivePlayer.PlayerName) && StatsManager.Instance.playerStats[ActivePlayer.PlayerName].volumeDestroyed > 0)
        {
            EliminateActivePlayer();
            EndTurn();
        }
    }

    protected override void SetupTurn()
    {
        base.SetupTurn();

        StatsManager.Instance.ResetStats(); // TODO: this belongs in the EliminationMonitor
        Crystal.transform.position = CrystalStartPosition;
        Crystal.transform.localScale = CrystalStartScale;
    }
}