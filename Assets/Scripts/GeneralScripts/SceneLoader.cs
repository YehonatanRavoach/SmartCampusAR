using UnityEngine;
using UnityEngine.SceneManagement;
/*
 * SceneLoader.cs
 * -----------------------------------------------------------------------------
 * Simple utility MonoBehaviour that exposes a public method for loading
 * Unity scenes by name.  Attach this script to any UI Button and wire the
 * Button’s OnClick event to <see cref="LoadSceneByName(string)"/> in the
 * Inspector, passing the desired scene name.
 *
 *  ✦ Requires that the target scene is included in “Build Settings → Scenes in Build”.
 *  ✦ No error–handling is performed; invalid scene names will throw at runtime.
 */

public class SceneLoader : MonoBehaviour
{
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}

