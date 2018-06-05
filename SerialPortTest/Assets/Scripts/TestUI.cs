using UnityEngine.UI;
using UnityEngine;

public class TestUI : MonoBehaviour {

    public Text[] Texts;
    public SerialSendReceive serialSendReceive;

    private void TestFunc()
    {
        Texts[0].text = "Roll :" + DeviceData.Roll;
        Texts[1].text = "Pitch : " + DeviceData.Pitch;
        Texts[2].text = "Yaw : " + DeviceData.Yaw;
        Texts[3].text = "Speed : " + DeviceData.SpeedCount;
        Texts[4].text = "Rotary : " + DeviceData.RotaryCount;
        Texts[5].text = "Handle : " + DeviceData.Handle;
        Texts[6].text = "Limite_Center : " + DeviceData.Limit_Center;
        Texts[7].text = "Limite_Right : " + DeviceData.Limit_Right;
        Texts[8].text = "Limite_Left : " + DeviceData.Limit_Left;
        Texts[9].text = "Buttons : " + DeviceData.BTN_L1 + " "
            + DeviceData.BTN_L2 + " " + DeviceData.BTN_R1 + " "
            + DeviceData.BTN_R2;

        Texts[10].text = "Connect : " + serialSendReceive.bConnetedDevice.ToString();
        Texts[11].text = "ErrorCount : " + serialSendReceive.errorCount;
        Texts[12].text = "Error : " + serialSendReceive.errorMsg;
    }
    // Update is called once per frame
    void Update () {
        TestFunc();
    }
}
