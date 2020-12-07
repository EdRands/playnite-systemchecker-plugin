﻿using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using SystemChecker.Models;

namespace SystemChecker.Clients
{
    class PCGamingWikiRequierements: RequierementMetadata
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private readonly IPlayniteAPI _PlayniteApi;

        private SteamApi steamApi;

        private readonly string UrlSteamId = "https://pcgamingwiki.com/api/appid.php?appid={0}";
        private string UrlPCGamingWiki { get; set; } = string.Empty;
        private string UrlPCGamingWikiSearch { get; set; } = @"https://pcgamingwiki.com/w/index.php?search=";
        private int SteamId { get; set; } = 0;


        public PCGamingWikiRequierements(IPlayniteAPI PlayniteApi, string PluginUserDataPath)
        {
            _PlayniteApi = PlayniteApi;
            steamApi = new SteamApi(PluginUserDataPath);
        }


        public override GameRequierements GetRequirements()
        {
            gameRequierements = SystemChecker.PluginDatabase.GetDefault(_game);

            // Search data with SteamId (is find) or game url (if defined)
            if (SteamId != 0)
            {
                gameRequierements = GetRequirements(string.Format(UrlSteamId, SteamId));
                if (IsFind())
                {
#if DEBUG
                    logger.Debug($"SystemChecker - PCGamingWikiRequierements.IsFind - SteamId: {SteamId} - gameRequierements: {JsonConvert.SerializeObject(gameRequierements)}");
#endif

                    return gameRequierements;
                }
            }

            if (!UrlPCGamingWiki.IsNullOrEmpty())
            {
                gameRequierements = GetRequirements(UrlPCGamingWiki);
                if (IsFind())
                {
#if DEBUG
                    logger.Debug($"SystemChecker - PCGamingWikiRequierements.IsFind - UrlPCGamingWiki: {UrlPCGamingWiki} - gameRequierements: {JsonConvert.SerializeObject(gameRequierements)}");
#endif

                    return gameRequierements;
                }
            }

            logger.Warn($"SystemChecker - PCGamingWikiRequierements - Not find for {_game.Name}");

            return gameRequierements;
        }

        public GameRequierements GetRequirements(Game game)
        {
            _game = game;
            SteamId = 0;
            UrlPCGamingWiki = string.Empty;


            if (_game.SourceId != Guid.Parse("00000000-0000-0000-0000-000000000000"))
            {
                if (game.Source.Name.ToLower() == "steam")
                {
                    SteamId = int.Parse(game.GameId);
                }
            }
            if (SteamId == 0)
            {   
                SteamId = steamApi.GetSteamId(game.Name);
            }

            if (_game.Links != null)
            {
                foreach (Link link in game.Links)
                {
                    if (link.Url.ToLower().Contains("pcgamingwiki"))
                    {
                        UrlPCGamingWiki = link.Url;

                        if (UrlPCGamingWiki.Contains(UrlPCGamingWikiSearch))
                        {
                            UrlPCGamingWiki = UrlPCGamingWikiSearch + WebUtility.UrlEncode(UrlPCGamingWiki.Replace(UrlPCGamingWikiSearch, string.Empty));
                        }
                        if (UrlPCGamingWiki.Contains(@"http://pcgamingwiki.com/w/index.php?search="))
                        {
                            UrlPCGamingWiki = UrlPCGamingWikiSearch + WebUtility.UrlEncode(UrlPCGamingWiki.Replace(@"http://pcgamingwiki.com/w/index.php?search=", string.Empty));
                        }
                    }
                }
            }

            if (UrlPCGamingWiki.IsNullOrEmpty() && _game.ReleaseDate != null)
            {
                UrlPCGamingWiki = UrlPCGamingWikiSearch + WebUtility.UrlEncode(game.Name) + $"+%28{((DateTime)game.ReleaseDate).ToString("yyyy")}%29";
            }

#if DEBUG
            logger.Debug($"SystemChecker - PCGamingWikiRequierements - {_game.Name} - SteamId: {SteamId} - UrlPCGamingWiki: {UrlPCGamingWiki}");
#endif

            return GetRequirements();
        }

        public override GameRequierements GetRequirements(string url)
        {
            try
            {
#if DEBUG
                logger.Debug($"SystemChecker PCGamingWikiRequierements.GetRequirements - url {url}");
#endif

                // Get data & parse
                string ResultWeb = string.Empty;
                try
                {
                    ResultWeb =  Web.DownloadStringData(url).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SystemChecker", $"Failed to download {url}");
                }


                HtmlParser parser = new HtmlParser();
                IHtmlDocument HtmlRequirement = parser.Parse(ResultWeb);

                var systemRequierement = HtmlRequirement.QuerySelector("div.sysreq_Windows");
                if (systemRequierement != null)
                {
                    gameRequierements.Link = url;

                    Requirement Minimum = new Requirement();
                    Requirement Recommanded = new Requirement();

                    foreach (var row in systemRequierement.QuerySelectorAll(".table-sysreqs-body-row"))
                    {
                        string dataTitle = row.QuerySelector(".table-sysreqs-body-parameter").InnerHtml.ToLower();
                        string dataMinimum = row.QuerySelector(".table-sysreqs-body-minimum").InnerHtml.Trim();

                        string dataRecommended = string.Empty;
                        if (row.QuerySelector(".table-sysreqs-body-recommended") != null)
                        {
                            dataRecommended = row.QuerySelector(".table-sysreqs-body-recommended").InnerHtml.Trim();
                        }

#if DEBUG
                        logger.Debug($"SystemChecker - PCGamingWikiRequierements - dataMinimum: {dataMinimum}");
                        logger.Debug($"SystemChecker - PCGamingWikiRequierements - dataRecommended: {dataRecommended}");
#endif

                        switch (dataTitle)
                        {
                            case "operating system (os)":
                                if (!dataMinimum.IsNullOrEmpty())
                                {
                                    dataMinimum = dataMinimum.Replace("(1803 or later)", string.Empty)
                                        .Replace(" (Only inclusive until patch 1.16.1. Patch 1.17+ Needs XP and greater.)", string.Empty);
                                    Minimum.Os = dataMinimum.Split(',').Select(x => x.Trim()).ToList();
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    dataRecommended = dataRecommended.Replace("(1803 or later)", string.Empty)
                                        .Replace(" (Only inclusive until patch 1.16.1. Patch 1.17+ Needs XP and greater.)", string.Empty);
                                    Recommanded.Os = dataRecommended.Split(',').Select(x => x.Trim()).ToList();
                                }
                                break;

                            case "processor (cpu)":
                                if (!dataMinimum.IsNullOrEmpty())
                                {
                                    dataMinimum = dataMinimum.Replace("(or equivalent)", string.Empty).Replace("or equivalent", string.Empty)
                                        .Replace("or AMD equivalent", string.Empty)
                                        .Replace(" / ", "¤").Replace("<br>", "¤");
                                    Minimum.Cpu = dataMinimum.Split('¤').Select(x => x.Trim()).ToList();
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    dataRecommended = dataRecommended.Replace("(or equivalent)", string.Empty).Replace("or equivalent", string.Empty)
                                        .Replace("or AMD equivalent", string.Empty)
                                        .Replace(" / ", "¤").Replace("<br>", "¤");
                                    Recommanded.Cpu = dataMinimum.Split('¤').Select(x => x.Trim()).ToList();
                                }
                                break;

                            case "system memory (ram)":
                                if (!dataMinimum.IsNullOrEmpty())
                                {
                                    if (dataMinimum.ToLower().IndexOf("mb") > -1)
                                    {
                                        dataMinimum = dataMinimum.Substring(0, dataMinimum.ToLower().IndexOf("mb"));
                                        Minimum.Ram = 1024 * 1024 * long.Parse(dataMinimum.ToLower().Replace("mb", string.Empty).Trim());
                                    }
                                    if (dataMinimum.ToLower().IndexOf("gb") > -1)
                                    {
                                        dataMinimum = dataMinimum.Substring(0, dataMinimum.ToLower().IndexOf("gb"));
                                        Minimum.Ram = 1024 * 1024 * 1024 * long.Parse(dataMinimum.ToLower().Replace("gb", string.Empty).Trim());
                                    }
                                    Minimum.RamUsage = SizeSuffix(Minimum.Ram, true);
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    if (dataRecommended.ToLower().IndexOf("mb") > -1)
                                    {
                                        dataRecommended = dataRecommended.Substring(0, dataRecommended.ToLower().IndexOf("mb"));
                                        Recommanded.Ram = 1024 * 1024 * long.Parse(dataRecommended.ToLower().Replace("mb", string.Empty).Trim());
                                    }
                                    if (dataRecommended.ToLower().IndexOf("gb") > -1)
                                    {
                                        dataRecommended = dataRecommended.Substring(0, dataRecommended.ToLower().IndexOf("gb"));
                                        Recommanded.Ram = 1024 * 1024 * 1024 * long.Parse(dataRecommended.ToLower().Replace("gb", string.Empty).Trim());
                                    }
                                    Recommanded.RamUsage = SizeSuffix(Recommanded.Ram, true);
                                }
                                break;

                            case "hard disk drive (hdd)":
                                double hdd = 0;
                                if (!dataMinimum.IsNullOrEmpty())
                                {
                                    if (dataMinimum.ToLower().IndexOf("mb") > -1)
                                    {
                                        dataMinimum = dataMinimum.Substring(0, dataMinimum.ToLower().IndexOf("mb"));

                                        Double.TryParse(dataMinimum.ToLower().Replace("mb", string.Empty).Trim()
                                            .Replace(".", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Trim()
                                            .Replace(",", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Trim(), out hdd);

                                        Minimum.Storage = (long)(1024 * 1024 * hdd);
                                    }
                                    if (dataMinimum.ToLower().IndexOf("gb") > -1)
                                    {
                                        dataMinimum = dataMinimum.Substring(0, dataMinimum.ToLower().IndexOf("gb"));

                                        Double.TryParse(dataMinimum.ToLower().Replace("mb", string.Empty).Trim()
                                            .Replace(".", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Trim()
                                            .Replace(",", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Trim(), out hdd);

                                        Minimum.Storage = (long)(1024 * 1024 * 1024 * hdd);
                                    }
                                    Minimum.StorageUsage = SizeSuffix(Minimum.Storage);
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    if (dataRecommended.ToLower().IndexOf("mb") > -1)
                                    {
                                        dataRecommended = dataRecommended.Substring(0, dataRecommended.ToLower().IndexOf("mb"));

                                        Double.TryParse(dataMinimum.ToLower().Replace("mb", string.Empty).Trim()
                                           .Replace(".", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Trim()
                                           .Replace(",", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Trim(), out hdd);

                                        Recommanded.Storage = (long)(1024 * 1024 * hdd);
                                    }
                                    if (dataRecommended.ToLower().IndexOf("gb") > -1)
                                    {
                                        dataRecommended = dataRecommended.Substring(0, dataRecommended.ToLower().IndexOf("gb"));

                                        Double.TryParse(dataMinimum.ToLower().Replace("mb", string.Empty).Trim()
                                           .Replace(".", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Trim()
                                           .Replace(",", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Trim(), out hdd);

                                        Minimum.Storage = (long)(1024 * 1024 * 1024 * hdd);
                                    }
                                    Recommanded.StorageUsage = SizeSuffix(Recommanded.Storage);
                                }
                                break;

                            case "video card (gpu)":
                                if (!dataMinimum.IsNullOrEmpty())
                                {
                                    dataMinimum = dataMinimum.Replace("(or equivalent)", string.Empty).Replace("or equivalent", string.Empty)
                                        .Replace("Integrated", string.Empty).Replace("Dedicated", string.Empty)
                                        .Replace("+ compatible", string.Empty).Replace("compatible", string.Empty)
                                        .Replace("that supports DirectDraw at 640x480 resolution, 256 colors", string.Empty)
                                        .Replace(" / ", "¤").Replace("<br>", "¤");

                                    dataMinimum = Regex.Replace(dataMinimum, "(</[^>]*>)", "");
                                    dataMinimum = Regex.Replace(dataMinimum, "(<[^>]*>)", "");

                                    Minimum.Gpu = dataMinimum.Split('¤')
                                        .Select(x => x.Trim()).ToList()
                                        .Where(x => x.Length > 6)
                                        .Where(x => x.ToLower().IndexOf("shader") == -1)
                                        .Where(x => x.ToLower().IndexOf("anything") == -1)
                                        .Where(x => x.ToLower().IndexOf("any card") == -1)
                                        .Where(x => x.Trim() != string.Empty).ToList();
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    dataRecommended = dataRecommended.Replace("(or equivalent)", string.Empty).Replace("or equivalent", string.Empty)
                                        .Replace("Integrated", string.Empty).Replace("Dedicated", string.Empty)
                                        .Replace("+ compatible", string.Empty).Replace("compatible", string.Empty)
                                        .Replace("that supports DirectDraw at 640x480 resolution, 256 colors", string.Empty)
                                        .Replace(" / ", "¤").Replace("<br>", "¤");

                                    dataRecommended = Regex.Replace(dataRecommended, "(</[^>]*>)", "");
                                    dataRecommended = Regex.Replace(dataRecommended, "(<[^>]*>)", "");

                                    Recommanded.Gpu = dataRecommended.Split('¤')
                                        .Select(x => x.Trim()).ToList()
                                        .Where(x => x.Length > 6)
                                        .Where(x => x.ToLower().IndexOf("shader") == -1).ToList()
                                        .Where(x => x.ToLower().IndexOf("anything") == -1).ToList()
                                        .Where(x => x.ToLower().IndexOf("any card") == -1)
                                        .Where(x => x.Trim() != string.Empty).ToList();
                                }
                                break;

                            default :
                                logger.Warn($"SystemChecker - No treatment for {dataTitle}");
                                break;
                        }
                    }

                    Minimum.IsMinimum = true;
#if DEBUG
                    logger.Debug($"SystemChecker - PCGamingWikiRequierements - Minimum: {JsonConvert.SerializeObject(Minimum)}");
                    logger.Debug($"SystemChecker - PCGamingWikiRequierements - Recommanded: {JsonConvert.SerializeObject(Recommanded)}");
#endif
                    gameRequierements.Items = new List<Requirement> { Minimum, Recommanded };
                }
                else
                {
                    logger.Warn($"SystemChecker - No data find for {_game.Name} in {url}");
                }

            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SystemChecker");
            }

            return gameRequierements;
        }
    }
}
