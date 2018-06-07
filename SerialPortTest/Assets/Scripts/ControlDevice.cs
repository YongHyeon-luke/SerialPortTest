using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ControlDevice : MonoBehaviour {

    public float Horizontal { get; private set; }
    private byte _motorDir = 0x00;
    private byte _motorSpeed = 0x00;
    public static readonly float MAX_MOTOR_SPEED = 50.0f;
    public SerialSendReceive serialSendReceive;

    void UpdateMotor()
    {
        //if (DeviceData.Pitch > 0)
        //    _motorDir = (byte)MotorDir.RIGHT;
        //else if (DeviceData.Pitch < 0)
        //    _motorDir = (byte)MotorDir.LEFT;
        //else
        //    _motorDir = (byte)MotorDir.STOP;

        Horizontal = Input.GetAxis("Horizontal") * MAX_MOTOR_SPEED;
        _motorSpeed = (byte)Math.Abs(Horizontal);

        if (serialSendReceive.bConnetedDevice)
            serialSendReceive.SendDataPcToDevice(0x00, 0x00, 0x00, _motorDir, _motorSpeed);
    }

    void Update()
    {

        if (Horizontal < 0)
            _motorDir = (byte)MotorDir.LEFT;
        else if (Horizontal > 0)
            _motorDir = (byte)MotorDir.RIGHT;
        else
            _motorDir = (byte)MotorDir.STOP;

        UpdateMotor();

    }


}
