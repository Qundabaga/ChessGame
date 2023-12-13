using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverScreen : MonoBehaviour
{
    public TMP_Text gameOverText;

    public void Message(string message)
    {
        gameObject.SetActive(true);
        gameOverText.text = message;
    }
}
