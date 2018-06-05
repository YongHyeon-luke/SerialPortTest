using System;

public static class DeviceData {

    ///<summary>
    ///0x00000000 ~ 0xFFFFFFFF
    ///0~255
    ///</summary>
    public static int SpeedCount { get; set; }
    ///<summary>
    ///0x00000000 ~ 0xFFFFFFFF
    ///0~255
    ///</summary>
    public static int RotaryCount { get; set; }
    ///<summary>
    ///0x0000 ~ 0x03FF
    ///</summary>
    public static int Handle { get; set; }
   // public static int Button { get; set; }

    public static bool BTN_L1 { get; set; }
    public static bool BTN_L2 { get; set; }
    public static bool BTN_R1 { get; set; }
    public static bool BTN_R2 { get; set; }

    public static bool Limit_Left { get; set; }
    public static bool Limit_Center { get; set; }
    public static bool Limit_Right { get; set; }

    ///<summary>
    ///범위 : -180 ~ 180
    ///</summary>
    public static float Roll { get; set; }
    ///<summary>
    ///범위 : -180 ~ 180
    ///</summary>
    public static float Pitch { get; set; }
    ///<summary>
    ///범위 : -180 ~ 180
    ///</summary>
    public static float Yaw { get; set; }
}
