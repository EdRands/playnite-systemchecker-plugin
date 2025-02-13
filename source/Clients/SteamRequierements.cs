﻿using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPluginsPlaynite.PluginLibrary.SteamLibrary.SteamShared;
using CommonPluginsShared;
using CommonPluginsShared.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using SystemChecker.Models;

namespace SystemChecker.Clients
{
    class SteamRequierements : RequierementMetadata
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private uint AppId { get; set; }


        public SteamRequierements()
        {

        }

        private string GetSteamData()
        {
            string url = string.Empty;
            try
            {
                url = $"https://store.steampowered.com/api/appdetails?appids={AppId}&l=english";
                return Web.DownloadStringData(url).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Failed to download {url}");
                return string.Empty;
            }
        }


        public override GameRequierements GetRequirements()
        {
            gameRequierements = SystemChecker.PluginDatabase.GetDefault(_game);

            Requirement Minimum = new Requirement();
            Requirement Recommanded = new Requirement();

            try
            {
                string data = GetSteamData();
                var parsedData = Serialization.FromJson<Dictionary<string, StoreAppDetailsResult>>(data);

                if (parsedData[AppId.ToString()].data != null && Serialization.ToJson(parsedData[AppId.ToString()].data.pc_requirements) != "[]")
                {
                    Common.LogDebug(true, Serialization.ToJson(parsedData[AppId.ToString()].data.pc_requirements));

                    dynamic pc_requirements = Serialization.FromJson<dynamic>(Serialization.ToJson(parsedData[AppId.ToString()].data.pc_requirements));

                    if (pc_requirements["minimum"] != null)
                    {
                        Minimum = ParseRequirement((string)pc_requirements["minimum"]);
                    }

                    if (pc_requirements["recommended"] != null)
                    {
                        Recommanded = ParseRequirement((string)pc_requirements["recommended"]);
                    }


                    gameRequierements.SourcesLink = new SourceLink
                    {
                        Name = "Steam",
                        GameName = parsedData[AppId.ToString()].data.name,
                        Url = $"https://store.steampowered.com/app/{AppId}/"
                    };
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            Minimum.IsMinimum = true;
            gameRequierements.Items = new List<Requirement> { Minimum, Recommanded };
            return gameRequierements;
        }

        public GameRequierements GetRequirements(Game game, uint appId = 0)
        {
            _game = game;

            this.AppId = appId;
            if (appId == 0)
            {
                this.AppId = uint.Parse(_game.GameId);
            }

            return GetRequirements();
        }

        public override GameRequierements GetRequirements(string url)
        {
            throw new NotImplementedException();
        }


        private Requirement ParseRequirement(string pc_requirement)
        {
            Requirement requirement = new Requirement();

            HtmlParser parser = new HtmlParser();
            IHtmlDocument HtmlRequirement = parser.Parse(pc_requirement);

            // Only recent game
            foreach (var ElementRequirement in HtmlRequirement.QuerySelectorAll("li"))
            {
                Common.LogDebug(true, $"SteamRequierements - {ElementRequirement.InnerHtml}");

                //<strong>OS:</strong> Windows XP / 7 / 8 / 8.1 / 10 x32 and x64<br> </ li >
                if (ElementRequirement.InnerHtml.IndexOf("<strong>OS") > -1)
                {
                    string os = ElementRequirement.InnerHtml
                        .Replace("\t", " ")
                        .Replace("<strong>OS:</strong>", string.Empty)
                        .Replace("with Platform Update for  7 ( versions only)", string.Empty)
                        .Replace("Win ", string.Empty)
                        .Replace("win ", string.Empty)
                        .Replace("windows", string.Empty)
                        .Replace("Windows", string.Empty)
                        .Replace("Microsoft", string.Empty)
                        .Replace("microsoft", string.Empty)
                        .Replace(", 32-bit", string.Empty)
                        .Replace(", 32bit", string.Empty)
                        .Replace(", 64-bit", string.Empty)
                        .Replace(", 64bit", string.Empty)
                        .Replace("®", string.Empty)
                        .Replace("+", string.Empty)
                        .Replace("and above", string.Empty)
                        .Replace("x32", string.Empty)
                        .Replace("and", string.Empty)
                        .Replace("x64", string.Empty)
                        .Replace("32-bit", string.Empty)
                        .Replace("32Bit", string.Empty)
                        .Replace("32 Bit", string.Empty)
                        .Replace("64-bit", string.Empty)
                        .Replace("64Bit", string.Empty)
                        .Replace("64 Bit", string.Empty)
                        .Replace("latest Service Pack", string.Empty)
                        .Replace("latest service pack", string.Empty)
                        .Replace("32-bit/64-bit", string.Empty)
                        .Replace("32bit/64bit", string.Empty)
                        .Replace("64-bit Operating System Required", string.Empty)
                        .Replace("32-bit Operating System Required", string.Empty)
                        .Replace(" Operating System Required", string.Empty)
                        .Replace("Operating System Required", string.Empty)
                        .Replace(" equivalent or better", string.Empty)
                        .Replace(" or equivalent.", string.Empty)
                        .Replace(" or equivalent", string.Empty)
                        .Replace(" or Newer", string.Empty)
                        .Replace(" or newer", string.Empty)
                        .Replace("or Newer", string.Empty)
                        .Replace("or newer", string.Empty)
                        .Replace("or later", string.Empty)
                        .Replace("or higher", string.Empty)
                        .Replace("()", string.Empty)
                        .Replace("<br>", string.Empty)
                        .Trim();

                    foreach (string sTemp in os.Replace(",", "¤").Replace(" or ", "¤").Replace("/", "¤").Split('¤'))
                    {
                        requirement.Os.Add(sTemp.Trim());
                    }
                }

                //< li >< strong > Processor:</ strong > Intel Core2 Duo E6320 or equivalent /\t AMD Athlon 64 X2 5000 + (2 * 2.6 GHz) or equivalent<br></ li >
                if (ElementRequirement.InnerHtml.IndexOf("<strong>Processor") > -1)
                {
                    string cpu = ElementRequirement.InnerHtml
                            .Replace("\t", " ")
                            .Replace("<strong>Processor:</strong>", string.Empty)
                            .Replace("&nbsp;", string.Empty)
                            .Replace("equivalent or higher processor", string.Empty)
                            .Replace("- Low budget CPUs such as Celeron or Duron needs to be at about twice the CPU speed", string.Empty)
                            .Replace(" equivalent or faster processor", string.Empty)
                            .Replace(" equivalent or better", string.Empty)
                            .Replace("above", string.Empty)
                            .Replace("and up", string.Empty)
                            .Replace("(or higher)", string.Empty)
                            .Replace("or higher", string.Empty)
                            .Replace(" or equivalent.", string.Empty)
                            .Replace(" over", string.Empty)
                            .Replace(" or faster", string.Empty)
                            .Replace(" or better", string.Empty)
                            .Replace(" or equivalent", string.Empty)
                            .Replace(" or Equivalent", string.Empty)
                            .Replace("4 CPUs", string.Empty)
                            .Replace(", ~2.4GHz", string.Empty)
                            .Replace(", ~3.1GHz", string.Empty)
                            .Replace("ghz", "GHz")
                            .Replace("Ghz", "GHz")
                            .Replace("Processor", string.Empty)
                            .Replace("processor", string.Empty)
                            .Replace("(not recommended for Intel HD Graphics cards)", ", not recommended for Intel HD Graphics cards")
                            .Replace("()", string.Empty)
                            .Replace("<br>", string.Empty)
                            .Trim();

                    cpu = Regex.Replace(cpu, ", ([0-9])", " $1");
                    cpu = Regex.Replace(cpu, "([0-9]),([0-9] GHz)", "$1.$2");
                    cpu = Regex.Replace(cpu, "([0-9])GHz", "$1 GHz");
                    cpu = Regex.Replace(cpu, "([0-9999])k", "$1K");
                    cpu = cpu.Replace(",", "¤").Replace(" / ", "¤").Replace(" or ", "¤").Replace(" OR ", "¤")
                        .Replace(" and ", "¤").Replace(" AND ", "¤").Replace(" | ", "¤");
                    foreach (string sTemp in cpu.Split('¤'))
                    {
                        requirement.Cpu.Add(sTemp.Trim());
                    }
                }

                //< li >< strong > Memory:</ strong > 2048 MB RAM<br></ li >
                if (ElementRequirement.InnerHtml.IndexOf("<strong>Memory") > -1)
                {
                    string ram = ElementRequirement.InnerHtml.ToLower()
                            .Replace("\t", " ")
                            .Replace("<strong>memory:</strong>", string.Empty)
                            .Replace("ram", string.Empty)
                            .Replace("of system", string.Empty)
                            .Replace("<br>", string.Empty)
                            .Trim();
                    ram = ram.Split('/')[ram.Split('/').Length - 1];

                    if (ram.ToLower().IndexOf("mb") > -1)
                    {
                        requirement.Ram = 1024 * 1024 * long.Parse(ram.ToLower()
                            .Replace("mb", string.Empty)
                            .Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator).Trim());
                    }
                    if (ram.ToLower().IndexOf("gb") > -1)
                    {
                        requirement.Ram = 1024 * 1024 * 1024 * long.Parse(ram.ToLower().
                            Replace("gb", string.Empty)
                            .Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator).Trim());
                    }
                    requirement.RamUsage = Tools.SizeSuffix(requirement.Ram, true);
                }

                //< li >< strong > Graphics:</ strong > GeForce GT 440(1024 MB) or equivalent / Radeon HD 6450(512 MB) or equivalent / Iris Pro Graphics 5200(1792 MB) < br ></ li >
                if (ElementRequirement.InnerHtml.IndexOf("<strong>Graphics") > -1)
                {
                    string gpu = ElementRequirement.InnerHtml
                            .Replace("\t", " ")
                            .Replace("<strong>Graphics:</strong>", string.Empty)
                            .Replace("ATI or NVidia card w/ 1024 MB RAM (NVIDIA GeForce GTX 260 or ATI HD 4890)", "NVIDIA GeForce GTX 260 or ATI HD 4890")
                            .Replace("Video card must be 128 MB or more and should be a DirectX 9-compatible with support for Pixel Shader 2.0b (", string.Empty)
                            .Replace("- *NOT* an Express graphics card).", string.Empty)
                            .Replace("DirectX 11 class GPU with 1GB VRAM (", string.Empty)
                            //.Replace(")<br>", string.Empty)
                            .Replace("/320M 512MB VRAM", string.Empty)
                            .Replace("/Intel Extreme Graphics 82845, 82865, 82915", string.Empty)
                            .Replace(" 512MB VRAM (Intel integrated GPUs are not supported!)", " / Intel integrated GPUs are not supported!")
                            .Replace("(not recommended for Intel HD Graphics cards)", ", not recommended for Intel HD Graphics cards")
                            .Replace("or similar (no support for onboard cards)", string.Empty)
                            .Replace("level Graphics Card (requires support for SSE)", string.Empty)
                            .Replace("- Integrated graphics and very low budget cards might not work.", string.Empty)
                            .Replace("Shader Model 3.0", string.Empty)
                            .Replace("shader model 3.0", string.Empty)
                            .Replace("card capable of shader 3.0", string.Empty)
                            .Replace("3D with TnL support and", string.Empty)
                            .Replace(" compatible", string.Empty)
                            .Replace("Any", string.Empty)
                            .Replace("any", string.Empty)
                            .Replace("/Nvidia", " / Nvidia")
                            .Replace("or AMD equivalent", string.Empty)
                            .Replace("DX9 Compliant with PS3.0 support", string.Empty)
                            .Replace("DX9 Compliant", string.Empty)

                            .Replace("ATI or NVidia card", "Card")
                            .Replace("w/", "with")
                            .Replace("Graphics: ", string.Empty)
                            .Replace(" equivalent or better", string.Empty)
                            .Replace(" or equivalent.", string.Empty)
                            .Replace("or equivalent.", string.Empty)
                            .Replace(" or equivalent", string.Empty)
                            .Replace(" or better.", string.Empty)
                            .Replace("or better.", string.Empty)
                            .Replace(" or better", string.Empty)
                            .Replace(" or newer", string.Empty)
                            .Replace("or newer", string.Empty)
                            .Replace("or higher", string.Empty)
                            .Replace("or better", string.Empty)
                            .Replace("or greater graphics card", string.Empty)
                            .Replace("or equivalent", string.Empty)
                            .Replace("Mid-range", string.Empty)
                            .Replace(" Memory Minimum", string.Empty)
                            .Replace(" memory minimum", string.Empty)
                            .Replace(" Memory Recommended", string.Empty)
                            .Replace(" memory recommended", string.Empty)
                            .Replace("e.g.", string.Empty)
                            .Replace("Laptop integrated ", string.Empty)
                            .Replace("GPU 1GB VRAM", "GPU 1 GB VRAM")
                            .Replace("with 3GB system ram", "(3 GB)")
                            .Replace("with 512MB", "(512 MB)")
                            .Replace("(1Gb)", "(1 GB)")
                            .Replace("(1GB)", "(1 GB)")
                            .Replace(" 1GB", " (1 GB)")
                            .Replace(" 2GB", " (2 GB)")
                            .Replace("(2GB)", " (2 GB)")
                            .Replace("(3GB)", " (3 GB)")
                            .Replace("(4GB)", " (4 GB)")
                            .Replace(" 6GB", " (6 GB)")
                            .Replace(" 4GB", " (4 GB)")
                            .Replace("8GB Memory 8 GB RAM", "(8 GB)")
                            .Replace(" or more and should be a DirectX 9-compatible with support for Pixel Shader 3.0", string.Empty)
                            .Replace(", or ", string.Empty)
                            .Replace("()", string.Empty)
                            .Replace("<br>", string.Empty)
                            .Replace("  ", " ")
                            .Replace(". Integrated Intel HD Graphics should work but is not supported; problems are generally solved with a driver update.", string.Empty)
                            .Trim();

                    gpu = Regex.Replace(gpu, " - ([0-9]) GB", " ($1 GB)");
                    //gpu = Regex.Replace(gpu, "([0-9])Gb", "($1 GB)");
                    gpu = gpu.Replace(",", "¤").Replace(" or ", "¤").Replace(" OR ", "¤").Replace(" / ", "¤").Replace(" | ", "¤");
                    foreach (string sTemp in gpu.Split('¤'))
                    {
                        if (sTemp.Trim() != string.Empty)
                        {
                            requirement.Gpu.Add(sTemp.Trim());
                        }
                    }
                }
                if (ElementRequirement.InnerHtml.IndexOf("<strong>DirectX") > -1 && ElementRequirement.InnerHtml.IndexOf("8") > -1)
                {
                    requirement.Gpu.Add("DirectX 8");
                }
                if (ElementRequirement.InnerHtml.IndexOf("<strong>DirectX") > -1 && ElementRequirement.InnerHtml.IndexOf("9") > -1)
                {
                    requirement.Gpu.Add("DirectX 9");
                }
                if (ElementRequirement.InnerHtml.IndexOf("<strong>DirectX") > -1 && ElementRequirement.InnerHtml.IndexOf("10") > -1)
                {
                    requirement.Gpu.Add("DirectX 10");
                }
                if (ElementRequirement.InnerHtml.IndexOf("<strong>DirectX") > -1 && ElementRequirement.InnerHtml.IndexOf("11") > -1)
                {
                    requirement.Gpu.Add("DirectX 11");
                }


                //< li >< strong > DirectX:</ strong > Version 10 < br ></ li >
                //< li >< strong > Network:</ strong > Broadband Internet connection<br></ li >

                //< li >< strong > Storage:</ strong > 350 MB available space </ li >
                if (ElementRequirement.InnerHtml.IndexOf("<strong>Storage") > -1 || ElementRequirement.InnerHtml.IndexOf("<strong>Hard Drive") > -1)
                {
                    string storage = ElementRequirement.InnerHtml.ToLower()
                        .Replace("\t", " ")
                        .Replace("<strong>storage:</strong>", string.Empty)
                        .Replace("<strong>hard drive:</strong>", string.Empty)
                        .Replace("available space", string.Empty)
                        .Replace("equivalent or better", string.Empty)
                        .Replace("or equivalent", string.Empty)
                        .Replace("hd space", string.Empty)
                        .Replace("free space", string.Empty)
                        .Replace("free hard drive space", string.Empty)
                        .Replace("<br>", string.Empty)
                        .Trim();

                    if (storage.IndexOf("mb") > -1)
                    {
                        requirement.Storage = 1024 * 1024 * double.Parse(storage.Replace("mb", string.Empty)
                            .Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                            .Replace("available hard disk space", string.Empty).Trim());
                    }
                    if (storage.IndexOf("gb") > -1)
                    {
                        requirement.Storage = 1024 * 1024 * 1024 * double.Parse(storage.Replace("gb", string.Empty)
                            .Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                            .Replace("available hard disk space", string.Empty).Trim());
                    }
                    requirement.StorageUsage = Tools.SizeSuffix(requirement.Storage);
                }
            }

            return requirement;
        }
    }
}
