namespace Server.Models.Common
{
    public enum UserRole : byte
    {
        Admin = 0,
        Operator = 1,
        Viewer = 2
    }

    public enum DeviceStatus : byte
    {
        Offline = 0,
        Running = 1,
        Alarm = 2,
        Stopped = 3
    }

    public enum RegisterType : byte
    {
        InputRegister = 3,
        HoldingRegister = 4
    }

    public enum DataPointType : byte
    {
        Int16 = 0,
        UInt16 = 1,
        Int32 = 2,
        Float = 3
    }

    public enum AlarmLevel : byte
    {
        Critical = 0,
        Major = 1,
        Minor = 2,
        Info = 3
    }

    public enum AlarmType : byte
    {
        HighLimit = 0,
        LowLimit = 1
    }

    public enum AlarmStatus : byte
    {
        Unacknowledged = 0,
        Acknowledged = 1
    }
}
