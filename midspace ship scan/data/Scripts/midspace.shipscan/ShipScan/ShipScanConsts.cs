namespace midspace.shipscan
{
    public enum MassCategory
    {
        Unknown = 0,
        Junk = 1,
        Tiny = 2,
        Small,
        Large,
        Huge,
        Enormous,
        Ridiculous
    };

    public enum SpeedCategory
    {
        Stationary = 0,
        Drifting = 3,
        Moving = 20,
        Flying
    };

    public enum ScanType
    {
        ChatConsole = 0,
        MissionScreen = 1,
        GpsCoordinates = 2
    };
}
