using UnityEngine;
using UnityEngine.SceneManagement;

public class PrebuiltSceneController : MonoBehaviour {

  void Awake() {
    // scan through all objects in the scene and convert them to gameobjects
    Prebuilt prebuilt = Prebuilt.ConvertSceneIntoPrebuilt(SceneManager.GetActiveScene());

    // immediately transition to a GameModel that just contains this floor and start the Game scene
    GameModel.GenerateFromPrebuiltAndSetMain(prebuilt);
    SceneManager.LoadScene("Scenes/Game");
  }
}
