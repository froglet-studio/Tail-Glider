using UnityEngine;

public class TimeBasedTurnMonitor : TurnMonitor
{
    [SerializeField] float duration;
    float elapsedTime;

    public override bool CheckForEndOfTurn()
    {
        return elapsedTime > duration;
    }

    public override void NewTurn(string playerName)
    {
        elapsedTime = 0;
    }

    void Update() {
        elapsedTime += Time.deltaTime;
    }
}