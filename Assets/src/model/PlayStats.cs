using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayLog {
  public static PlayLog Load() {
    var logJson = PlayerPrefs.GetString("playLog");
    var log = JsonUtility.FromJson<PlayLog>(logJson);
    if (log == null) {
      log = new PlayLog();
    }
    return log;
  }

  public static void Save(PlayLog log) {
    PlayerPrefs.SetString("playLog", JsonUtility.ToJson(log));
  }

  public static void Update(Action<PlayLog> method) {
    var log = Load();
    method(log);
    Save(log);
  }

  public List<PlayStats> stats;

  public PlayLog() {
    stats = new List<PlayStats>();
  }
}

[Serializable]
public class PlayStats {
  // only defined if not won
  public string killedBy;
  public bool won;
  // updated only at the end
  public float timeTaken;
  public int waterCollected;
  // updated only at the end
  public int floorsCleared;
  public int damageDealt;
  public int damageTaken;
  public int enemiesDefeated;
  internal int plantsPlanted;
  // public int numPerfectClears;
  /**
- # Floors cleared
- Damage dealt
- Damage taken
- Damage blocked
- Creatures defeated
- # Perfect Clears
- Fastest clear
- Average time per floor
- Time taken per floor
- Water collected
- Floor when you first took damage
- Plant harvest log (a list of what plants harvested when for what items)
*/
}
