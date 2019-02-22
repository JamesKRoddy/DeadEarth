using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PathDisplayMode { None, Connections, Paths };

public class AIWaypointNetwork : MonoBehaviour {

    [HideInInspector]
    public PathDisplayMode DisplayMode = PathDisplayMode.Connections;
    [HideInInspector]
    public int UIStart;
    [HideInInspector]
    public int UIEnd;


    public List<Transform> Waypoints = new List<Transform>();

}
