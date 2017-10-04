using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {

    public static GameManager INSTANCE;

    Agent[] agents;

    public Agent[] Agents {
        get;
        set;
    }

	// Use this for initialization
	void Start () {
		if (INSTANCE != null) {
            this.enabled = false;
            return;
        }
        INSTANCE = this;
        Agents = FindObjectsOfType<Agent>();
	}
}
