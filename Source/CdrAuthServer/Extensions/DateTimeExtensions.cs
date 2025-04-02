namespace CdrAuthServer.Extensions
{
    public static class DateTimeExtensions
    {
        private static readonly DateTime _epochTime = DateTime.UnixEpoch;

        public static bool HasExpired(this DateTime creationTime, int seconds, DateTime now)
            => creationTime.AddSeconds(seconds) < now;

        public static int ToEpoch(this DateTime time) => (int)(time - _epochTime).TotalSeconds;

        public static DateTime? FromEpoch(this long? time)
        {
            if (time == null)
            {
                return null;
            }

            return DateTimeOffset.FromUnixTimeSeconds(time.Value).UtcDateTime;
        }
    }
}
