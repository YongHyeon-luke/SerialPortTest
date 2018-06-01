/*
 Luke
 */
// PITCH = X축
// YAW = Y축
// ROLL = Z축
using UnityEngine;
using System.IO.Ports;
using System;
using UnityEngine.UI;
using System.Linq;

public class SerialSendReceive : MonoBehaviour {

    private int errorCount = 0;
    private float rcvFPSTimer = 0.0f;
    private bool isButtonsOn = false;
    private const float SCALE_FACTOR = 180.0f / 32767.0f;
    private readonly float MIN_REACT_RANGE = -3.0f;
    private readonly float MAX_REACT_RANGE = 3.0f;

    //PC to Device
    private SerialPort mySerialPort;
    private byte[] serialSendBuffer = Enumerable.Repeat((byte)0x00, 8).ToArray();
    private bool bConnetedDevice = false;

    private readonly int serialBaudRate = 115200;
    public string SerialPortName = "COM3";

    //Device to PC
    public float RCVSerialRate = 1.0f;

    private int rcvCount = 0;
    private byte[] serialDataBuffer;
    private byte[] RCVSerialBuffer;
    private byte chkSum = 0;
    private float RCVTimer = 0.0f;
    private readonly int RCVBUFFERSIZE = 2048;

    //TestUI
    public Text[] Texts;

    void OnEnable()
    {
        SetSerialPort();
        CheckSerialPort();
        RCVSerialBuffer = new byte[RCVBUFFERSIZE];
        serialDataBuffer = Enumerable.Repeat((byte)0x00, 20).ToArray();
        Texts[7].text = "Connect : " + bConnetedDevice.ToString();
        InitializeSendData();
    }

    void OnDisable()
    {
        if (mySerialPort == null)
            return;

        if (mySerialPort.IsOpen)
        {
            InitializeSendData();
            mySerialPort.Close();
        }
    }


    void SetSerialPort()
    {

        if (mySerialPort != null)
            if (mySerialPort.IsOpen)
                mySerialPort.Close();

        try
        {
            if (mySerialPort == null)
            {
                mySerialPort = new SerialPort(SerialPortName, serialBaudRate);
                mySerialPort.Parity = Parity.None;
                mySerialPort.DataBits = 8;
                mySerialPort.StopBits = StopBits.One;
                mySerialPort.Open();

            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
            Texts[5].text = "Error : " + ex.ToString();
            mySerialPort.Dispose();
            mySerialPort = null;
        }

    }

    void CheckSerialPort()
    {
        if (mySerialPort == null)
            return;

        if (mySerialPort.IsOpen)
            bConnetedDevice = true;
        else
            bConnetedDevice = false;
    }

    public void InitializeSendData()
    {
        if (bConnetedDevice)
        {
            SendDataPcToDevice(0xFF, 0xFF, 0x00, 0x00, 0x00);
        }
    }

    void SendDataPcToDevice(byte brake, byte vibrate, byte fan, byte acDirection, byte acSpeed)
    {
        serialSendBuffer[0] = 0x02;
        serialSendBuffer[1] = brake;
        serialSendBuffer[2] = vibrate;
        serialSendBuffer[3] = fan;
        serialSendBuffer[4] = acDirection;
        serialSendBuffer[5] = acSpeed;

        serialSendBuffer[6] = CalcCheckSum();

        serialSendBuffer[7] = 0x03;

        mySerialPort.Write(serialSendBuffer, 0, serialSendBuffer.Length);
        mySerialPort.DiscardOutBuffer();
    }

    public byte CalcCheckSum()
    {
        int sum = (serialSendBuffer[1] +
                            serialSendBuffer[2] +
                            serialSendBuffer[3] +
                            serialSendBuffer[4] +
                            serialSendBuffer[5]);

        byte[] checkSum = BitConverter.GetBytes(sum);

        return checkSum[0];
    }

    public void StartSerialCommunication()
    {
        if (!bConnetedDevice)
            return;

        if (RCVTimer >= RCVSerialRate)
        {
            RCVTimer = 0.0f;

            InitializeRCVSerialBuffer();

            int length = ReadSerialPortBuffer();

            //serialDataBuffer 20바이트 배열
            //RCVSerialBuffer = 2048바이트 배열
            for (int i = 0; i < length; i++)
            {
                //STX
                if (rcvCount == 0)
                {
                    StartSTX(i);
                }
                //Data
                else if (rcvCount >= 1 && rcvCount <= 17)
                {
                    GetDeviceBufferData(i);
                }
                else if (rcvCount == 18)
                {
                    CheckSum(i);
                }
                //ETX
                else if (rcvCount == 19)
                {
                    serialDataBuffer[rcvCount] = RCVSerialBuffer[i];

                    if (serialDataBuffer[rcvCount] == 0x03)
                    {

                        GetSpeedCount();
                        GetRotaryCount();
                        GetHandle();

                        //DeviceData.Button = (int)serialDataBuffer[11];

                        SetButton();

                        GetRoll();
                        GetPitch();
                        GetYaw();
                    }
                    else
                    {
                        errorCount++;
                    }

                    rcvCount = 0;
                }
            }
        }
        else
        {
            RCVTimer += Time.deltaTime;
        }
    }

    void Update()
    {
        StartSerialCommunication();
        TestFunc();
    }

    void TestFunc()
    {
        Texts[0].text = "Roll :" + DeviceData.Roll;
        Texts[1].text = "Pitch : " + DeviceData.Pitch;
        Texts[2].text = "Yaw : " + DeviceData.Yaw;
        Texts[3].text = "Speed : " + DeviceData.SpeedCount;
        Texts[4].text = "Rotation : " + DeviceData.RotaryCount;
        Texts[6].text = "Handle : " + DeviceData.Handle;
        Texts[8].text = "Limite_Center : " + DeviceData.Limit_Center;
        Texts[9].text = "Limite_Right : " + DeviceData.Limit_Right;
        Texts[10].text = "Limite_Left : " + DeviceData.Limit_Left;
        Texts[11].text = "Buttons : " + DeviceData.BTN_L1 + " "
            + DeviceData.BTN_L2 + " " + DeviceData.BTN_R1 + " "
            + DeviceData.BTN_R2;
    }

    void InitializeRCVSerialBuffer()
    {
        for (int i = 0; i < RCVBUFFERSIZE; i++)
        {
            RCVSerialBuffer[i] = 0x00;
        }
    }

    int ReadSerialPortBuffer()
    {
        try
        {
            return mySerialPort.Read(RCVSerialBuffer, 0, RCVBUFFERSIZE);
        }
        catch (Exception e)
        {
            return -1;
        }
    }

    void StartSTX(int i)
    {
        if (RCVSerialBuffer[i] == 0x02)
        {
            InitializeSerialDataBuffer();
            serialDataBuffer[rcvCount] = RCVSerialBuffer[i];
            chkSum = 0;
            rcvCount++;
        }
        else
        {
            errorCount++;
        }
    }

    void InitializeSerialDataBuffer()
    {
        for (int j = 0; j < serialDataBuffer.Length; j++)
        {
            serialDataBuffer[j] = 0x00;
        }
    }

    void GetDeviceBufferData(int i)
    {
        serialDataBuffer[rcvCount] = RCVSerialBuffer[i];
        chkSum += serialDataBuffer[rcvCount];
        rcvCount++;
    }

    void CheckSum(int i)
    {
        serialDataBuffer[rcvCount] = RCVSerialBuffer[i];
        if (chkSum == serialDataBuffer[rcvCount])
            rcvCount++;
        else
        {
            rcvCount = 0;
            errorCount++;
        }
    }

    void GetSpeedCount()
    {
        DeviceData.SpeedCount = (int)(serialDataBuffer[1] << 24 & 0xFF000000) |
                                                                                    (serialDataBuffer[2] << 16 & 0x00FF0000) |
                                                                                    (serialDataBuffer[3] << 8 & 0x0000FF00) |
                                                                                    (serialDataBuffer[4] & 0x000000FF);
    }

    void GetRotaryCount()
    {
        DeviceData.RotaryCount = (int)(serialDataBuffer[5] << 24 & 0xFF000000) |
                                                                                        (serialDataBuffer[6] << 16 & 0x00FF0000) |
                                                                                        (serialDataBuffer[7] << 8 & 0x0000FF00) |
                                                                                        (serialDataBuffer[8] & 0x000000FF);
    }

    void GetHandle()
    {
        DeviceData.Handle = (int)(serialDataBuffer[9] << 8 & 0xFF00) |
                                                                           (serialDataBuffer[10] & 0x03FF);
    }

    void SetButton()
    {
        if ((serialDataBuffer[11] & 0x01) == 0x00)
            DeviceData.BTN_L1 = true;
        else
            DeviceData.BTN_L1 = false;

        if ((serialDataBuffer[11] & 0x02) == 0x00)
            DeviceData.BTN_L2 = true;
        else
            DeviceData.BTN_L2 = false;

        if ((serialDataBuffer[11] & 0x04) == 0x00)
            DeviceData.BTN_R1 = true;
        else
            DeviceData.BTN_R1 = false;

        if ((serialDataBuffer[11] & 0x08) == 0x00)
            DeviceData.BTN_R2 = true;
        else
            DeviceData.BTN_R2 = false;

        if ((serialDataBuffer[11] & 0x10) == 0x00)
            DeviceData.Limit_Left = true;
        else
            DeviceData.Limit_Left = false;

        if ((serialDataBuffer[11] & 0x20) == 0x00)
            DeviceData.Limit_Center = true;
        else
            DeviceData.Limit_Center = false;

        if ((serialDataBuffer[11] & 0x40) == 0x00)
            DeviceData.Limit_Right = true;
        else
            DeviceData.Limit_Right = false;

        isButtonsOn = DeviceData.BTN_L1 |
                                      DeviceData.BTN_L2 |
                                      DeviceData.BTN_R1 |
                                      DeviceData.BTN_R2;
    }

    void GetRoll()
    {
        DeviceData.Roll = ((short)((serialDataBuffer[12] & 0xFF) | (serialDataBuffer[13] & 0xFF) << 8)
                                                ) * SCALE_FACTOR;

        if (DeviceData.Roll > MIN_REACT_RANGE && DeviceData.Roll < MAX_REACT_RANGE)
            DeviceData.Roll = 0.0f;
    }

    void GetPitch()
    {
        DeviceData.Pitch = ((short)((serialDataBuffer[14] & 0xFF) | (serialDataBuffer[15] & 0xFF) << 8)
                                                 ) * SCALE_FACTOR;

        if (DeviceData.Pitch > MIN_REACT_RANGE && DeviceData.Pitch < MAX_REACT_RANGE)
            DeviceData.Pitch = 0.0f;
    }

    void GetYaw()
    {
        DeviceData.Yaw = ((short)((serialDataBuffer[16] & 0xFF) | (serialDataBuffer[17] & 0xFF) << 8)
                                                 ) * SCALE_FACTOR;

        if (DeviceData.Yaw > MIN_REACT_RANGE && DeviceData.Yaw < MAX_REACT_RANGE)
            DeviceData.Yaw = 0.0f;
    }

}
