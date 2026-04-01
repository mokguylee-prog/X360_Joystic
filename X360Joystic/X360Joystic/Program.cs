using System;
using System.Runtime.InteropServices;

namespace X360Joystic
{
    [StructLayout(LayoutKind.Sequential)]
    struct XINPUT_GAMEPAD
    {
        public ushort wButtons;
        public byte   bLeftTrigger;
        public byte   bRightTrigger;
        public short  sThumbLX;
        public short  sThumbLY;
        public short  sThumbRX;
        public short  sThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XINPUT_STATE
    {
        public uint           dwPacketNumber;
        public XINPUT_GAMEPAD Gamepad;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XINPUT_VIBRATION
    {
        public ushort wLeftMotorSpeed;
        public ushort wRightMotorSpeed;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XINPUT_BATTERY_INFORMATION
    {
        public byte BatteryType;
        public byte BatteryLevel;
    }

    static class Battery
    {
        public const byte DEVTYPE_GAMEPAD    = 0x00;
        public const byte TYPE_DISCONNECTED  = 0x00;
        public const byte TYPE_WIRED         = 0x01;
        public const byte TYPE_ALKALINE      = 0x02;
        public const byte TYPE_NIMH          = 0x03;
        public const byte TYPE_UNKNOWN       = 0xFF;
        public const byte LEVEL_EMPTY        = 0x00;
        public const byte LEVEL_LOW          = 0x01;
        public const byte LEVEL_MEDIUM       = 0x02;
        public const byte LEVEL_FULL         = 0x03;
    }

    [Flags]
    enum GamepadButtons : ushort
    {
        DPAD_UP        = 0x0001,
        DPAD_DOWN      = 0x0002,
        DPAD_LEFT      = 0x0004,
        DPAD_RIGHT     = 0x0008,
        START          = 0x0010,
        BACK           = 0x0020,
        LEFT_THUMB     = 0x0040,
        RIGHT_THUMB    = 0x0080,
        LEFT_SHOULDER  = 0x0100,
        RIGHT_SHOULDER = 0x0200,
        A              = 0x1000,
        B              = 0x2000,
        X              = 0x4000,
        Y              = 0x8000,
    }
}
