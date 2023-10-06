using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GamblerController : NPCController {
  public Gambler gambler => (Gambler) actor;
}