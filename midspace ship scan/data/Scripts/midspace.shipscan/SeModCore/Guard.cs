namespace MidSpace.ShipScan.SeModCore
{
    using System;

    public static class Guard
    {
        public static void IsNotZero(int value, string message)
        {
            if (value == 0)
                throw new Exception(message);
        }

        public static void IsNotZero(uint value, string message)
        {
            if (value == 0)
                throw new Exception(message);
        }

        public static void IsNotZero(ulong value, string message)
        {
            if (value == 0)
                throw new Exception(message);
        }

        public static void IsNotEmpty(object value, string message)
        {
            string str = value as string;
            if (str != null && string.IsNullOrEmpty(str))
                throw new Exception(message);
            if (value == null)
                throw new Exception(message);
        }

        public static void IsNotEqual(object value1, object value2, string message)
        {
            if (value1 == null && value2 == null)
                throw new Exception(message);
            if (value1?.Equals(value2) ?? false)
                throw new Exception(message);
        }
    }
}
