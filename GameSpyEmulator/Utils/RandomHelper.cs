using System;
using System.Text;
using Reality.Net.Extensions;

namespace GameSpyEmulator
{
    internal static class RandomHelper
    {
        readonly static Random _random = new Random();
        public static string GetString(int length, string chars)
        {
           return Extensions.GetString(_random, length, chars);
        }

        public static string GetString(int length)
        {
            return Extensions.GetString(_random, length);
        }
    }
}
