using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public string GameScene;

    [Header("Menu Buttons")]
    public Button startButton;
    public Button exitButton;

    [Header("Options Panel")]
    public GameObject optionsPanel;  // Ses ayarlarının yer aldığı panel (başlangıçta kapalı)
    public Slider volumeSlider;      // Ses seviyesi ayarı için slider

    [Header("Background Music")]
    public AudioSource backgroundMusic;

    void Start()
    {
        // Butonlara tıklama eventlerini ekleyelim:
        startButton.onClick.AddListener(StartGame);
        exitButton.onClick.AddListener(ExitGame);

        // Slider değişikliğinde ses ayarını yapalım:
        

        // Options panel başlangıçta kapalı olsun:
        

        // Slider değerini mevcut ses seviyesine eşitleyelim:
    
    }

    void StartGame()
    {
        // Örneğin "GameScene" adlı sahneyi yükler (sahne adını projenize göre değiştirin)
        SceneManager.LoadScene(GameScene);
    }

    void ToggleOptions()
    {
        // Options paneli aç/kapa yap:
        optionsPanel.SetActive(!optionsPanel.activeSelf);
    }

    void ExitGame()
    {
        // Uygulamayı kapat (Editor'de çalışırken bu çalışmayabilir)
        Application.Quit();
    }

    void SetVolume(float value)
    {
        if (backgroundMusic != null)
        {
            backgroundMusic.volume = value;
        }
    }
}
