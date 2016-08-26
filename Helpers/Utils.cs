using System;

namespace PokemonGo.RocketAPI.Helpers
{
    public class Utils
    {
        public static ulong FloatAsUlong(double value)
        {
            var bytes = BitConverter.GetBytes(value);
            return BitConverter.ToUInt64(bytes, 0);
        }

        public static int GetTime(bool ms = false)
        {
            TimeSpan timeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1));

            if (ms)
                return (int)Math.Round(timeSpan.TotalMilliseconds);
            else
                return (int)Math.Round(timeSpan.TotalSeconds);
        }
    }
}