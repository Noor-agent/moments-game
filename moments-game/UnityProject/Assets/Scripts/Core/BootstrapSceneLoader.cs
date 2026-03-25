using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple scene loader for Bootstrap — loads Attract scene directly.
/// Bypasses Addressables for editor testing.
/// Attach this to the Bootstrap Root GameObject.
/// </summary>
public class BootstrapSceneLoader : MonoBehaviour
{
    private void Start()
    {
        SceneManager.LoadScene("Attract", LoadSceneMode.Additive);
    }
}
