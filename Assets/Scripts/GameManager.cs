using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
  private GameObject floorPrefab;
  public GameObject player;

  // Start is called before the first frame update
  void Start()
  {
    this.floorPrefab = Resources.Load<GameObject>("Floor");
    GameObject floorInstance = Instantiate(floorPrefab);
    FloorComponent floorComponent = floorInstance.GetComponent<FloorComponent>();
    floorComponent.floor = GameModel.model.floors[0];

    // GameObject playerPrefab = Resources.Load<GameObject>("Player");
    // GameObject player = Instantiate(playerPrefab);
  }

  void Awake()
  {
  }

  // Update is called once per frame
  void Update()
  {

  }
}
