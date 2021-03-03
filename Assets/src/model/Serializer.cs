using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public static class Serializer {
  public static string SAVE_PATH;
  public static string CHECKPOINT_PATH;
  public static string GARDEN_PATH;
  static Serializer() {
    SAVE_PATH = Application.persistentDataPath + "/save0.dat";
    CHECKPOINT_PATH = Application.persistentDataPath + "/checkpoint.dat";
    CHECKPOINT_PATH = Application.persistentDataPath + "/garden.dat";
  }

  /// <summary>Does *not* set main.</summary>
  public static GameModel LoadSave0() => Load(SAVE_PATH);

  public static GameModel Load(string path) {
    Debug.Log("Loading save from " + SAVE_PATH);
    var bf = GetBinaryFormatter();
    using (FileStream file = File.Open(SAVE_PATH, FileMode.Open)) {
      var model = (GameModel) bf.Deserialize(file);
      file.Close();
      return model;
    }
  }

  public static bool HasSave() => File.Exists(SAVE_PATH);

  public static void DeleteSave0() {
    File.Delete(SAVE_PATH);
  }

  public static bool SaveMainToFile() {
    var model = GameModel.main;
    if (model.home is TutorialFloor) {
      // don't save tutorial
      return true;
    }
    return Save(model, SAVE_PATH);
  }

  public static bool Save(object modelOrFloor, string path) {
    var bf = GetBinaryFormatter();
    using(FileStream file = File.Create(path)) {
      bf.Serialize(file, modelOrFloor);
      Debug.Log($"Saved {SAVE_PATH}");
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
