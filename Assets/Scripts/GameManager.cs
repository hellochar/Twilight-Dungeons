using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
  public Floor floor;
  private GameObject floorPrefab;

  // Start is called before the first frame update
  void Start()
  {
    floor = Floor.generateRandomLevel();
    this.floorPrefab = Resources.Load<GameObject>("Floor");
    GameObject floorInstance = Instantiate(floorPrefab);
    FloorComponent floorComponent = floorInstance.GetComponent<FloorComponent>();
    floorComponent.floor = floor;
  }

  void Awake()
  {
  }

  // Update is called once per frame
  void Update()
  {

  }
}
