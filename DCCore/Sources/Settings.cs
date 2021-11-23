using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using D2Tools.Structs;

public class Settings
{
    public enum ParsingMode
    {
        CONFIG,
        FILTERS
    }

    public class FilterEntry
    {
        public string Text;

        public List<D2ItemRarity> Rarity = new List<D2ItemRarity>();

        public bool CheckEtheral;

        public bool CheckSockets;
        public int MinSockets;
        public int MaxSockets;
    }

    public List<FilterEntry> Filters = new List<FilterEntry>();

    public int TextX = 80;
    public int TextY = 80;

    public int CheckDelayMs = 500;

    public int TextFontSize = 24;
    public int TextLineStep = 24;

    public bool NewItemsBlink = true;
    public float BlinkSpeed = 10f;
    public int BlinkDuration = 60;

    public Settings()
    {
        ParseConfig();

        Console.WriteLine(Filters.Count + " filters enabled");
        Console.WriteLine("--------------------");
    }

    void ParseConfig(List<string> lines = null)
    {
        Filters.Clear();

        string configFile = "filters/filters.txt";

        if (lines == null)
        {
            if (!File.Exists(configFile))
            {
                Console.WriteLine($"'{configFile}' not found!");
                return;
            }
            else
            {
                Console.WriteLine($"Loading '{configFile}'...");
            }

            lines = File.ReadAllLines(configFile).ToList();
        }

        var currentParsingMode = ParsingMode.CONFIG;

        bool parseAgain = false;

        Regex sockets = new Regex("{.*?}", RegexOptions.Compiled | RegexOptions.Singleline);

        foreach (var l in lines)
        {
            if (string.IsNullOrWhiteSpace(l)) continue;

            string line = l.Trim('\n', '\r', '\t', ' ');

            if (line.StartsWith("#")) continue;

            if (line.Contains("CONFIG:"))
            {
                currentParsingMode = ParsingMode.CONFIG;
                continue;
            }

            if (line.Contains("FILTERS:"))
            {
                currentParsingMode = ParsingMode.FILTERS;
                continue;
            }

            switch (currentParsingMode)
            {
                case ParsingMode.CONFIG:
                    if (!line.Contains("="))
                    {
                        Console.WriteLine($"Can't parse CONFIG line: '{line}'");
                        continue;
                    }

                    string[] paramValue = line.Split('=');

                    if (paramValue.Length > 2 || paramValue.Length < 2)
                    {
                        Console.WriteLine($"Wrong CONFIG format of line: '{line}'");
                        continue;
                    }

                    string parameter = paramValue[0].Trim(' ');

                    // TODO handle exceptions
                    switch (parameter)
                    {
                        case "TextX":
                            TextX = int.Parse(paramValue[1].Trim(' '), NumberStyles.Integer);
                            break;

                        case "TextY":
                            TextY = int.Parse(paramValue[1].Trim(' '), NumberStyles.Integer);
                            break;

                        case "TextFontSize":
                            TextFontSize = int.Parse(paramValue[1].Trim(' '), NumberStyles.Integer);
                            break;

                        case "TextLineStep":
                            TextLineStep = int.Parse(paramValue[1].Trim(' '), NumberStyles.Integer);
                            break;

                        case "CheckDelayMs":
                            CheckDelayMs = int.Parse(paramValue[1].Trim(' '), NumberStyles.Integer);

                            CheckDelayMs = Math.Clamp(CheckDelayMs, 20, 2000);
                            break;

                        case "NewItemsBlink":
                            NewItemsBlink = int.Parse(paramValue[1].Trim(' '), NumberStyles.Integer) > 0;
                            break;

                        case "BlinkSpeed":
                            BlinkSpeed = float.Parse(paramValue[1].Trim(' '), NumberStyles.Float);
                            break;

                        case "BlinkDuration":
                            BlinkDuration = int.Parse(paramValue[1].Trim(' '), NumberStyles.Integer);
                            break;

                        default:
                            Console.WriteLine($"Unknown CONFIG parameter: '{parameter}'");
                            break;
                    }

                    break;

                case ParsingMode.FILTERS:

                    FilterEntry filterEntry = new FilterEntry();

                    if (line.StartsWith("(") && line.EndsWith(")"))
                    {
                        string includeFile = "filters/" + line + ".txt";

                        if (!File.Exists(includeFile))
                        {
                            Console.WriteLine($"FILTER file not found: '{includeFile}'");
                            continue;
                        }

                        string[] includeLines = File.ReadAllLines(includeFile);

                        lines.Remove(l);
                        lines.AddRange(includeLines);

                        // 200 IQ
                        parseAgain = true;
                    }

                    if (!parseAgain)
                    {
                        if (line.Contains("[M]"))
                        {
                            filterEntry.Rarity.Add(D2ItemRarity.MAGIC);
                            line = line.Replace("[M]", "");
                        }

                        if (line.Contains("[S]"))
                        {
                            filterEntry.Rarity.Add(D2ItemRarity.SET);
                            line = line.Replace("[S]", "");
                        }

                        if (line.Contains("[R]"))
                        {
                            filterEntry.Rarity.Add(D2ItemRarity.RARE);
                            line = line.Replace("[R]", "");
                        }

                        if (line.Contains("[U]"))
                        {
                            filterEntry.Rarity.Add(D2ItemRarity.UNIQUE);
                            line = line.Replace("[U]", "");
                        }

                        if (line.Contains("[HQ]"))
                        {
                            filterEntry.Rarity.Add(D2ItemRarity.HIGH_QUALITY);
                            line = line.Replace("[HQ]", "");
                        }

                        if (line.Contains("[ETH]"))
                        {
                            filterEntry.CheckEtheral = true;
                            line = line.Replace("[ETH]", "");
                        }

                        if (line.Contains("{") && line.Contains("}"))
                        {
                            var socketParam = sockets.Match(line).Value;

                            var socketParamTrimmed = socketParam.Substring(1, socketParam.Length - 2);

                            string[] socketsMinMax = socketParamTrimmed.Split('-');

                            if (socketsMinMax.Length > 2)
                            {
                                Console.WriteLine($"Wrong sockets FILTER '{socketParam}'");
                            }
                            else
                            {
                                filterEntry.CheckSockets = true;

                                if (socketsMinMax.Length == 1)
                                {
                                    int socketsExact = int.Parse(socketsMinMax[0], NumberStyles.Integer);

                                    filterEntry.MaxSockets = filterEntry.MinSockets = socketsExact;
                                }
                                else
                                {
                                    int socketsMin = int.Parse(socketsMinMax[0].Trim(' '), NumberStyles.Integer);
                                    int socketsMax = int.Parse(socketsMinMax[1].Trim(' '), NumberStyles.Integer);

                                    filterEntry.MinSockets = socketsMin;
                                    filterEntry.MaxSockets = socketsMax;
                                }
                            }

                            line = line.Replace(socketParam, "");
                        }

                        line = line.Trim(' ');

                        if (!string.IsNullOrWhiteSpace(line))
                            filterEntry.Text = line;

                        Filters.Add(filterEntry);
                    }

                    break;
            }

            if (parseAgain) break;
        }

        if (parseAgain)
            ParseConfig(lines);
    }
    }