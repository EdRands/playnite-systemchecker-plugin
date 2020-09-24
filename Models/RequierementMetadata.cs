﻿using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Globalization;

namespace SystemChecker.Models
{
    public abstract class RequierementMetadata
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        internal Game _game;
        internal GameRequierements gameRequierements = new GameRequierements
        {
            Minimum = new Requirement(),
            Recommanded = new Requirement()
        };


        public bool IsFind()
        {
            return gameRequierements.Minimum.Os.Count > 0;
        }

        public static string SizeSuffix(Int64 value)
        {
            string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return "0" + CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator + "0 bytes"; }

            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            return string.Format("{0} {1}", adjustedSize.ToString("0.0", CultureInfo.CurrentUICulture), SizeSuffixes[mag]);
        }


        public abstract GameRequierements GetRequirements();

        public abstract GameRequierements GetRequirements(string url);
    }
}
