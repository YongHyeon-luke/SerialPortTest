using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovePlayer : MonoBehaviour {

    private Transform myTransform;
    private ControlDevice controlDevice;

	// Use this for initialization
	void Start () {
        myTransform = GetComponent<Transform>();
        controlDevice = GetComponent<ControlDevice>();
	}
	
	// Update is called once per frame
	void Update () {
        //float movePos = DeviceData.Yaw * Time.deltaTime;
        //myTransform.Rotate(0, movePos, 0);
    }
}
