using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using D2Tools.Structs;

public class FilterSettings
{
    public enum ParsingMode
    {
        CONFIG,
        FILTERS
    }
    
    public enum AlignMode
    {
        TOP_LEFT, TOP_RIGHT, BOTTOM_RIGHT, BOTTOM_LEFT
    }

    public class FilterEntry
    {
        public bool IgnoreMode;
        
        public string Text;

        public List<D2ItemRarity> Rarity = new List<D2ItemRarity>();

        public bool CheckEtheral;

        public bool CheckSockets;
        public int MinSockets;
        public int MaxSockets;
    }

    public bool UseCustomNamesById = false;

    public Dictionary<uint, string> ItemsNamesByID = new Dictionary<uint, string>();
    public List<FilterEntry> Filters = new List<FilterEntry>();

    public AlignMode Mode = AlignMode.BOTTOM_RIGHT;
    
    public int TextX = 8;
    public int TextY = 20;

    public int CheckDelayMs = 500;

    public int TextFontSize = 20;
    public int TextLineStep = 24;

    public bool NewItemsBlink = true;
    public float BlinkSpeed = 5;
    public int BlinkDuration = 50;

    public FilterSettings()
    {
        Console.WriteLine("--------------------");

        ParseIDs();
        Console.WriteLine(ItemsNamesByID.Count + " IDs");

        Console.WriteLine("--------------------");

        ParseConfig();

        Console.WriteLine(Filters.Count + " filters enabled");
        Console.WriteLine("--------------------");
    }

    void ParseIDs()
    {
        string namesByIDsConfig = "filters/ID_NAME.txt";

        if (!File.Exists(namesByIDsConfig))
        {
            Console.WriteLine($"'{namesByIDsConfig}' not found, vanilla names will be displayed and filtered");
        }
        else
        {
            Console.WriteLine($"Loading '{namesByIDsConfig}'...");

            string[] idNamesLines = File.ReadAllLines(namesByIDsConfig);

            foreach (var idName in idNamesLines)
            {
                if (string.IsNullOrWhiteSpace(idName) || idName.StartsWith("#")) continue;

                if (idName.StartsWith(">"))
                {
                    Console.WriteLine(idName.Substring(1));
                    continue;
                }

                if (!idName.Contains(":"))
                {
                    Console.WriteLine($"line [{Array.IndexOf(idNamesLines, idName)}] '{idName}' will be ignored");
                    continue;
                }

                string idNameToSplit = idName.Trim('\n', '\r', '\t', ' ');

                string[] splitted = idNameToSplit.Split(':', 2);

                bool idOk = int.TryParse(splitted[0], out int id);

                if (!idOk)
                {
                    Console.WriteLine(
                        $"line [{Array.IndexOf(idNamesLines, idName)}] '{idName}' - can't parse ID '{splitted[0]}'");
                    continue;
                }

                if (ItemsNamesByID.ContainsKey((uint)id))
                {
                    Console.WriteLine(
                        $"line [{Array.IndexOf(idNamesLines, idName)}] '{idName}' - duplicate ID '{splitted[0]}'");
                    continue;
                }

                ItemsNamesByID[(uint)id] = splitted[1];
            }
        }

        UseCustomNamesById = ItemsNamesByID.Count > 0;
    }
    
    public static string FiltersFileRelativePath = "filters/filters.txt";

    void ParseConfig(List<string> lines = null)
    {
        Filters.Clear();

        if (lines == null)
        {
            if (!File.Exists(FiltersFileRelativePath))
            {
                Console.WriteLine($"'{FiltersFileRelativePath}' not found!");
                return;
            }

            Console.WriteLine($"Loading '{FiltersFileRelativePath}'...");

            lines = File.ReadAllLines(FiltersFileRelativePath).ToList();
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
                // CONFIG: section
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

                    switch (parameter)
                    {
                        case "AlignMode":
                            if (int.TryParse(paramValue[1].Trim(' '), out int value1)) Mode = (AlignMode)value1;
                            else Console.WriteLine($"Can't parse '{line}'");
                            break;
                        
                        case "TextX":
                            if (int.TryParse(paramValue[1].Trim(' '), out int value2)) TextX = value2;
                            else Console.WriteLine($"Can't parse '{line}'");
                            break;

                        case "TextY":
                            if (int.TryParse(paramValue[1].Trim(' '), out int value3)) TextY = value3;
                            else Console.WriteLine($"Can't parse '{line}'");
                            break;

                        case "TextFontSize":
                            if (int.TryParse(paramValue[1].Trim(' '), out int value4)) TextFontSize = value4;
                            else Console.WriteLine($"Can't parse '{line}'");
                            break;

                        case "TextLineStep":
                            if (int.TryParse(paramValue[1].Trim(' '), out int value5)) TextLineStep = value5;
                            else Console.WriteLine($"Can't parse '{line}'");
                            break;

                        case "CheckDelayMs":
                            if (int.TryParse(paramValue[1].Trim(' '), out int value6)) CheckDelayMs = value6;
                            else Console.WriteLine($"Can't parse '{line}'");
                            
                            CheckDelayMs = Math.Clamp(CheckDelayMs, 20, 2000);
                            break;

                        case "NewItemsBlink":
                            if (int.TryParse(paramValue[1].Trim(' '), out int value7)) NewItemsBlink = value7 > 0;
                            else Console.WriteLine($"Can't parse '{line}'");
                            break;

                        case "BlinkSpeed":
                            if (float.TryParse(paramValue[1].Trim(' '), out float value8)) BlinkSpeed = value8;
                            else Console.WriteLine($"Can't parse '{line}'");
                            break;

                        case "BlinkDuration":
                            if (int.TryParse(paramValue[1].Trim(' '), out int value9)) BlinkDuration = value9;
                            else Console.WriteLine($"Can't parse '{line}'");
                            break;

                        default:
                            Console.WriteLine($"Unknown CONFIG parameter: '{parameter}'");
                            break;
                    }

                    break;

                // FILTERS: section
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
                        if (line.StartsWith("!"))
                        {
                            filterEntry.IgnoreMode = true;
                            line = line.Substring(1);
                        }
                        
                        if (line.Contains("[N]"))
                        {
                            filterEntry.Rarity.Add(D2ItemRarity.NORMAL);
                            line = line.Replace("[N]", "");
                        }
                        
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
                        
                        if (line.Contains("[LQ]"))
                        {
                            filterEntry.Rarity.Add(D2ItemRarity.LOW_QUALITY);
                            line = line.Replace("[LQ]", "");
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