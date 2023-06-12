using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public static class Serializer {
  public static string SAVE_PATH;
  public static string CHECKPOINT_PATH;
  static Serializer() {
    SAVE_PATH = Application.persistentDataPath + "/save0.dat";
    CHECKPOINT_PATH = Application.persistentDataPath + "/checkpoint.dat";
  }

  /// <summary>Does *not* set main.</summary>
  public static GameModel LoadSave0(bool useBackup = true) {
    try {
      return Load(SAVE_PATH);
    } catch (Exception e) {
      Debug.LogError(e);
      if (useBackup) {
        return LoadCheckpoint();
      } else {
        throw e;
      }
    }
  }

  public static GameModel LoadCheckpoint() {
    return Load(CHECKPOINT_PATH);
  }

  private static GameModel Load(string path) {
    Debug.Log("Loading save from " + path);
    using (FileStream file = File.Open(path, FileMode.Open)) {
      var model = (GameModel) Deserialize(file);
      file.Close();
      SaveUpgrader.Upgrade(model);
      return model;
    }
  }

  public static bool HasSave() => File.Exists(SAVE_PATH) || File.Exists(CHECKPOINT_PATH);

  public static void DeleteSave0() {
    File.Delete(SAVE_PATH);
  }

  public static void DeleteCheckpoint() {
    File.Delete(CHECKPOINT_PATH);
  }

  public static bool SaveMainToFile() => Save(GameModel.main, SAVE_PATH);

  public static bool SaveMainToCheckpoint() {
    var checkpoint = Save(GameModel.main, CHECKPOINT_PATH);
    if (checkpoint) {
      // propagate the checkpoint to save0
      File.Copy(CHECKPOINT_PATH, SAVE_PATH, true);
    }
    return checkpoint;
  }

  private static bool Save(GameModel model, string path) {
    if (model.home is TutorialFloor || model.player.IsDead) {
      // don't save the tutorial, don't save if player is dead
      return true;
    }
    using(FileStream file = File.Create(path)) {
      Serialize(file, model);
      Debug.Log($"Saved {path}");
      file.Close();
      return true;
    }
  }

  private static BinaryFormatter GetBinaryFormatter() {
    BinaryFormatter bf = new BinaryFormatter();
    SurrogateSelector surrogateSelector = new SurrogateSelector();
    Vector2IntSerializationSurrogate vector2ISS = new Vector2IntSerializationSurrogate();
    surrogateSelector.AddSurrogate(typeof(Vector2Int), new StreamingContext(StreamingContextStates.All), vector2ISS);
    bf.SurrogateSelector = surrogateSelector;
    return bf;
  }

  /// Serialize using our better BinaryFormatter
  public static void Serialize(Stream serializationStream, object graph) {
    var bf = GetBinaryFormatter();
    bf.Serialize(serializationStream, graph);
  }

  /// Deserialize using our better BinaryFormatter
  public static object Deserialize(Stream serializationStream) {
    var bf = GetBinaryFormatter();
    return bf.Deserialize(serializationStream);
  }

  public static Stream GenerateStreamFromString(string s) {
    var stream = new MemoryStream();
    var writer = new StreamWriter(stream);
    writer.Write(s);
    writer.Flush();
    stream.Position = 0;
    return stream;
  }
}

public class Vector2IntSerializationSurrogate : ISerializationSurrogate {

  // Method called to serialize a Vector3 object
  public void GetObjectData(System.Object obj, SerializationInfo info, StreamingContext context) {

    Vector2Int v2 = (Vector2Int)obj;
    info.AddValue("x", v2.x);
    info.AddValue("y", v2.y);
  }

  // Method called to deserialize a Vector3 object
  public System.Object SetObjectData(System.Object obj, SerializationInfo info,
                                     StreamingContext context, ISurrogateSelector selector) {

    Vector2Int v2 = (Vector2Int)obj;
    v2.x = (int)info.GetValue("x", typeof(int));
    v2.y = (int)info.GetValue("y", typeof(int));
    obj = v2;
    return obj;
  }
}
