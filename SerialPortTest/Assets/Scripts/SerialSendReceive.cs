﻿/*
 Luke
 */
// PITCH = X축
// YAW = Y축
// ROLL = Z축
using UnityEngine;
using System.IO.Ports;
using System;
using System.Linq;

enum MotorDir : byte
{
    STOP = 0,
    RIGHT,
    LEFT,
}

public class SerialSendReceive : MonoBehaviour
{

    public int errorCount { get; private set; }
    public String errorMsg { get; private set; }

    private float rcvFPSTimer = 0.0f;
    private bool isButtonsOn = false;
    private const float SCALE_FACTOR = 180.0f / 32767.0f;
    public static readonly float MIN_REACT_RANGE = -3.0f;
    public static readonly float MAX_REACT_RANGE = 3.0f;
    public static readonly float MIN_LIMITE_ANGLE = -60.0f;
    public static readonly float MAX_LIMITE_ANGLE = 60.0f;


    //PC to Device
    private SerialPort mySerialPort;
    private byte[] serialSendBuffer = Enumerable.Repeat((byte)0x00, 8).ToArray();
    public bool bConnetedDevice { get; private set; }

    private readonly int serialBaudRate = 115200;
    public  string currentSerialPortName { get; set; }

    //Device to PC
    public float RCVSerialRate = 1.0f;

    private int rcvCount = 0;
    private byte[] serialDataBuffer;
    private byte[] RCVSerialBuffer;
    private byte chkSum = 0;
    private float RCVTimer = 0.0f;
    private readonly int RCVBUFFERSIZE = 2048;

    //드롭다운
    //private String[] serialPorts;
    //public Dropdown dropdown;
    //private List<Dropdown.OptionData> DropdownData = new List<Dropdown.OptionData>();

    //void GetSerialPort()
    //{
    //    serialPorts = SerialPort.GetPortNames();
    //    dropdown.ClearOptions();
    //    if (serialPorts.Length == 0)
    //    {
    //        DropdownData.Add(new Dropdown.OptionData("빈포트"));
    //        dropdown.AddOptions(DropdownData);
    //    }
    //    else
    //    {
    //        foreach (var item in serialPorts)
    //            DropdownData.Add(new Dropdown.OptionData(item));
    //        dropdown.AddOptions(DropdownData);
    //        currentSerialPortName = serialPorts[0];
    //    }
    //}

    //void DropdownValueChanged(Dropdown change)
    //{
    //    currentSerialPortName = change.GetComponentInChildren<Text>().text;
    //    bConnetedDevice = false;
    //    InitializeSerialPort();
    //    InitializeMotorDevice();
    //}

    void OnEnable()
    {
        InitializeSerialPort();
        //dropdown.onValueChanged.AddListener(delegate { DropdownValueChanged(dropdown); });
        RCVSerialBuffer = new byte[RCVBUFFERSIZE];
        serialDataBuffer = Enumerable.Repeat((byte)0x00, 20).ToArray();
        InitializeMotorDevice();

    }

    void OnDisable()
    {
        if (mySerialPort == null)
            return;

        if (mySerialPort.IsOpen)
        {
            InitializeMotorDevice();
            mySerialPort.Close();
        }
    }

    private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
    {
        SerialPort sp = (SerialPort)sender;
        StartSerialCommunication(ref sp);
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
                mySerialPort = new SerialPort(currentSerialPortName, serialBaudRate);
                mySerialPort.Parity = Parity.None;
                mySerialPort.DataBits = 8;
                mySerialPort.StopBits = StopBits.One;
                mySerialPort.ReadTimeout = 200;
                mySerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                mySerialPort.Open();
            }
        }
        catch (Exception ex)
        {
            errorMsg = ex.Message;
            mySerialPort.Close();
            mySerialPort = null;
            bConnetedDevice = false;
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

    public void InitializeMotorDevice()
    {
        if (bConnetedDevice)
        {
            SendDataPcToDevice(0xFF, 0xFF, 0x00, 0x00, 0x00);
        }
    }

    void InitializeSerialPort()
    {
        SetSerialPort();
        CheckSerialPort();
    }

    /// <summary>
    /// 장치에 데이터를 전송해 조종합니다.
    /// </summary>
    /// <param name="brake">Range : 0x00 ~ 0xFE</param>
    /// <param name="vibrate">Range : 0x00 ~ 0xFE</param>
    /// <param name="fan">Range : 0x00 ~0xFE</param>
    /// <param name="acDirection">0x00 : 정지, 0x01 : 시계방향, 0x02 역시계방향</param>
    /// <param name="acSpeed">Range : 0x00 ~ 0x3C</param>
    public void SendDataPcToDevice(byte brake, byte vibrate, byte fan, byte acDirection, byte acSpeed)
    {
        CheckSendData(ref brake, ref vibrate, ref fan, ref acDirection, ref acSpeed);
        serialSendBuffer[0] = 0x02;
        serialSendBuffer[1] = brake;
        serialSendBuffer[2] = vibrate;
        serialSendBuffer[3] = fan;
        serialSendBuffer[4] = acDirection;
        serialSendBuffer[5] = acSpeed;

        serialSendBuffer[6] = CalcMoterDeviceCheckSum();

        serialSendBuffer[7] = 0x03;

        mySerialPort.Write(serialSendBuffer, 0, serialSendBuffer.Length);
        mySerialPort.DiscardOutBuffer();
    }

    void CheckSendData(ref byte brake, ref byte vibrate, ref byte fan, ref byte acDirection, ref byte acSpeed)
    {
        brake = Math.Min(brake, (byte)0xFE);
        vibrate = Math.Min(vibrate, (byte)0xFE);
        fan = Math.Min(fan, (byte)0xFE);
        LimitACMotor(ref acDirection, ref acSpeed);
     }

    void LimitACMotor(ref byte acDirection, ref byte acSpeed)
    {
        bool OnAcDirectionRange = acDirection == (byte)MotorDir.STOP || acDirection == (byte)MotorDir.RIGHT || acDirection == (byte)MotorDir.LEFT;

        if (!OnAcDirectionRange)
            acDirection = (byte)MotorDir.STOP;

        if (OnLimiteAngle() || OnLimiteSensor())
            acSpeed = 0x00;
        else
            acSpeed = Math.Min(acSpeed, (byte)0x3C);
    }

    public bool OnLimiteAngle()
    {
        return DeviceData.Yaw < MIN_LIMITE_ANGLE || DeviceData.Yaw > MAX_LIMITE_ANGLE;
    }

    public bool OnLimiteSensor()
    {
        return DeviceData.Limit_Left || DeviceData.Limit_Right;
    }

    ////모터가 없는 장치
    //public  void SendDataPcToDevice(byte brake, byte vibrate, byte fan)
    //{
    //    serialSendBuffer[0] = 0x02;
    //    serialSendBuffer[1] = brake;
    //    serialSendBuffer[2] = vibrate;
    //    serialSendBuffer[3] = fan;

    //    serialSendBuffer[5] = CalcNoneMoterDeviceCheckSum();

    //    serialSendBuffer[6] = 0x03;

    //    mySerialPort.Write(serialSendBuffer, 0, serialSendBuffer.Length);
    //    mySerialPort.DiscardOutBuffer();
    //}

    byte CalcMoterDeviceCheckSum()
    {
        int sum = (serialSendBuffer[1] +
                            serialSendBuffer[2] +
                            serialSendBuffer[3] +
                            serialSendBuffer[4] +
                            serialSendBuffer[5]);

        byte[] checkSum = BitConverter.GetBytes(sum);

        return checkSum[0];
    }

    byte CalcNoneMoterDeviceCheckSum()
    {
        int sum = (serialSendBuffer[1] +
                           serialSendBuffer[2] +
                           serialSendBuffer[3]);

        byte[] checkSum = BitConverter.GetBytes(sum);

        return checkSum[0];
    }

    void StartSerialCommunication(ref SerialPort sp)
    {
        if (!bConnetedDevice)
            return;

        if (RCVTimer >= RCVSerialRate)
        {
            RCVTimer = 0.0f;

            InitializeRCVSerialBuffer();

            int length = ReadSerialPortBuffer(ref sp);

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

    //void Update()
    //{
        
    //}

    void InitializeRCVSerialBuffer()
    {
        for (int i = 0; i < RCVBUFFERSIZE; i++)
        {
            RCVSerialBuffer[i] = 0x00;
        }
    }

    int ReadSerialPortBuffer(ref SerialPort sp)
    {
        try
        {
            return sp.Read(RCVSerialBuffer, 0, RCVBUFFERSIZE);
        }
        catch (InvalidOperationException e)
        {
            errorMsg = e.Message;
            return -1;
        }
        catch (ArgumentNullException e)
        {
            errorMsg =  e.Message;
            return -1;
        }
        //finally
        //{
        //    mySerialPort.Close();
        //    mySerialPort = null;
        //    bConnetedDevice = false;
        //}
    }

    void InitializeSerialDataBuffer()
    {
        for (int j = 0; j < serialDataBuffer.Length; j++)
        {
            serialDataBuffer[j] = 0x00;
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
                                                                           (serialDataBuffer[10] & 0x00FF);
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
