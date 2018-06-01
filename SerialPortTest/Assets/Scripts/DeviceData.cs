using System;

public static class DeviceData {

    public static int SpeedCount { get; set; }
    public static int RotaryCount { get; set; }
    public static int Handle { get; set; }
   // public static int Button { get; set; }

    public static bool BTN_L1 { get; set; }
    public static bool BTN_L2 { get; set; }
    public static bool BTN_R1 { get; set; }
    public static bool BTN_R2 { get; set; }

    public static bool Limit_Left { get; set; }
    public static bool Limit_Center { get; set; }
    public static bool Limit_Right { get; set; }

    public static float Roll { get; set; }
    public static float Pitch { get; set; }
    public static float Yaw { get; set; }
}
