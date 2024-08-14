﻿using System.Globalization;

namespace CdrAuthServer.Domain.Extensions
{
    public static class StringExtensions
    {
        public static bool HasValue(this string? value)
        {
            return !string.IsNullOrEmpty(value);
        }

        public static bool IsNullOrEmpty(this string? value)
        {
            return string.IsNullOrEmpty(value);
        }

        public static bool IsNullOrWhiteSpace(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        public static bool TryParseToDateTime(this string stringDateTime, out DateTime? dateTime)
        {
            dateTime = default;

            if (stringDateTime.IsNullOrWhiteSpace())
            {
                return false;
            }

            var provider = new CultureInfo("en-US");
            if (DateTime.TryParse(stringDateTime, provider, DateTimeStyles.None, out var parsedDate))
            {
                dateTime = parsedDate;
                return true;
            }

            return false;
        }
    }
}
