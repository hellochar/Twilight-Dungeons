#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[UnityEditor.InitializeOnLoad]
static class BakePrebuiltSceneOnSave {
  static BakePrebuiltSceneOnSave() {
    UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += OnSceneSaved;
  }

  static void OnSceneSaved(Scene scene) {
    if (scene.path.StartsWith("Assets/Scenes/Prebuilts/")) {
      // convert scene to a Prebuilt and serialize it to /Assets/Resources/Prebuilts
      ConvertSceneToPrebuiltAndBake(scene);
    }
  }

  public static string BakedPrebuiltsPath = $"Assets/Resources/{Prebuilt.BakedPrebuiltsFolderName}";
  public static void ConvertSceneToPrebuiltAndBake(Scene scene) {
    var prebuilt = Prebuilt.ConvertSceneIntoPrebuilt(scene);

    // use .bytes extension so it can be read as a binary TextAsset
    var filename = $"{BakedPrebuiltsPath}/{scene.name}.bytes";
    using (FileStream file = File.Create(filename)) {
      Serializer.Serialize(file, prebuilt);
      Debug.LogFormat("Converted {0} to {1}", scene.path, file.Name);
      file.Close();
    }
  }
}
#endif