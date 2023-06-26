using System.IO;
using UnityEditor;
using UnityEngine;
public class MenuItemBakeAllPrebuilts : MonoBehaviour {
  [MenuItem("TwilightDungeons/Bake All Prebuilts")]
  static void BakeAllPrebuilts() {
    // Application.LoadLevelAdditive()
    foreach(var path in Directory.EnumerateFiles(
        Path.Combine("Assets", "Scenes", "Prebuilts"),
      // Path.Combine(
      //     System.Environment.CurrentDirectory,
      //     "Assets\\Scenes\\Prebuilts"),
        "*.unity"
      )) {
      Debug.Log(path);
      var scenePath = path;
      var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
      BakePrebuiltSceneOnSave.ConvertSceneToPrebuiltAndBake(scene);
      UnityEditor.SceneManagement.EditorSceneManager.UnloadSceneAsync(scene);
    }
  }
}