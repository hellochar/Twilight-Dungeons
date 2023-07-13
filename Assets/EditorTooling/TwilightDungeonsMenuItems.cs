using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

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

	[MenuItem("TwilightDungeons/Export atlases as PNG")]
	static void ExportAtlases() {
		string exportPath = Path.Combine(Directory.GetCurrentDirectory(), "Atlases");
		foreach (UnityEngine.Object obj in Selection.objects) {
			SpriteAtlas atlas = (SpriteAtlas) obj;
			if (atlas == null) continue;
			Debug.Log("Exporting selected atlas: " + atlas);
			
			// use reflection to run this internal editor method
			// UnityEditor.U2D.SpriteAtlasExtensions.GetPreviewTextures
			// internal static extern Texture2D[] GetPreviewTextures(this SpriteAtlas spriteAtlas);
			Type type = typeof(UnityEditor.U2D.SpriteAtlasExtensions);
			MethodInfo methodInfo = type.GetMethod("GetPreviewTextures", BindingFlags.Static | BindingFlags.NonPublic);
			if (methodInfo == null) {
				Debug.LogWarning("Failed to get UnityEditor.U2D.SpriteAtlasExtensions");
				return;
			}
			Texture2D[] textures = (Texture2D[])methodInfo.Invoke(null, new object[] {atlas} );
			if (textures == null) {
				Debug.LogWarning("Failed to get texture results");
				continue;
			}

			foreach (Texture2D texture in textures) {
				// these textures in memory are not saveable so copy them to a RenderTexture first
				Texture2D textureCopy = DuplicateTexture(texture);
				if (!Directory.Exists(exportPath)) Directory.CreateDirectory(exportPath);
				string filename = exportPath + "/" + texture.name + ".png";
				FileStream fs = new FileStream(filename, FileMode.Create);
				BinaryWriter bw = new BinaryWriter(fs);
				bw.Write(textureCopy.EncodeToPNG());
				bw.Close();
				fs.Close();
				Debug.Log("Saved texture to " + filename);
			}
		}
	}
	
	private static Texture2D DuplicateTexture(Texture2D source) {
		RenderTexture renderTex = RenderTexture.GetTemporary(
			source.width,
			source.height,
			0,
			RenderTextureFormat.Default,
			RenderTextureReadWrite.Linear);

		Graphics.Blit(source, renderTex);
		RenderTexture previous = RenderTexture.active;
		RenderTexture.active = renderTex;
		Texture2D readableText = new Texture2D(source.width, source.height);
		readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
		readableText.Apply();
		RenderTexture.active = previous;
		RenderTexture.ReleaseTemporary(renderTex);
		return readableText;
	}
}