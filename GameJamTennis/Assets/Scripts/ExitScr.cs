using UnityEngine;

public class ExitScr : MonoBehaviour
{
    public void QuitGame()
    {
        Debug.Log("Quitting game...");

        Application.Quit();
    }
}
