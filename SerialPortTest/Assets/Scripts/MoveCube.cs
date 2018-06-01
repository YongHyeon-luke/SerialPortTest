using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCube : MonoBehaviour {

    public Transform myTransfrom;

	// Use this for initialization
	void Start () {
        myTransfrom = GetComponent<Transform>();
	}
	
	// Update is called once per frame
	void Update () {
        myTransfrom.Rotate(new Vector3(DeviceData.Pitch, DeviceData.Yaw, DeviceData.Roll));
	}
}
