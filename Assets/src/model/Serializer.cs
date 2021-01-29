using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public static class Serializer {
  public const string SAVE0 = "save0";
  public static string SAVE_PATH => Application.persistentDataPath + "/save0.dat";
  public static bool LoadFromFile(out GameModel model) {
    if (File.Exists(SAVE_PATH)) {
      Debug.Log("Loading save from " + SAVE_PATH);
      var bf = GetBinaryFormatter();
      FileStream file = File.Open(SAVE_PATH, FileMode.Open);
      model = (GameModel) bf.Deserialize(file);
      file.Close();
      return true;
    }
    model = null;
    return false;
    // if (PlayerPrefs.HasKey(SAVE0)) {
    //   var savedGameString = PlayerPrefs.GetString(SAVE0);
    //   var bf = GetBinaryFormatter();
    //   try {
    //     using (var stream = GenerateStreamFromString(savedGameString)) {
    //       model = (GameModel)bf.Deserialize(stream);
    //       return true;
    //     }
    //   } catch (Exception e) {
    //     Debug.LogError(e);
    //     model = null;
    //     return false;
    //   }
    // } else {
    //   model = null;
    //   return false;
    // }
  }

  public static bool SaveToFile(GameModel model) {
    var bf = GetBinaryFormatter();
    FileStream file = File.Create(SAVE_PATH);
    bf.Serialize(file, model);
    file.Close();
    return true;
    // using (var stream = new MemoryStream()) {
    //   bf.Serialize(stream, model);
    //   stream.Position = 0;
    //   var streamReader = new StreamReader(stream);
    //   var savedGameString = streamReader.ReadToEnd();
    //   PlayerPrefs.SetString(SAVE0, savedGameString);
    //   PlayerPrefs.Save();
    //   Debug.Log("SAVE0 size" + savedGameString.Length);
    //   return true;
    // }
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
