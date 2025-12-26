using UnityEngine;
using Connect.Core;

public class GameplayButtons : MonoBehaviour
{

    public void RestartLevel()
    {
        GameManager.Instance.RestartLevel();
    }

    public void NextLevel()
    {
        if (!GameplayManager.Instance.hasGameFinished)
            return;

        GameManager.Instance.NextLevel();
    }

    public void ShowHint()
    {
        GameplayManager.Instance.ShowHint();
    }
}
