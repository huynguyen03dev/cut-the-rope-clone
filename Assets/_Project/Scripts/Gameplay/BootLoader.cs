using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Boot composition root. Services land in M3 (US-012/US-014); until then Boot
/// just forwards to the Game scene. The Game scene must also work when played
/// directly, without Boot.
/// </summary>
public sealed class BootLoader : MonoBehaviour
{
    [SerializeField] string gameSceneName = "Game";

    void Start() => SceneManager.LoadScene(gameSceneName);
}
