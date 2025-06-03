using static akUtil.Util;
using System.Collections.Generic;
using System.IO;
using System;

namespace akUtil
{
    /// <summary>
    /// Class to read a configuration file.
    /// 
    /// The file consists of segments.
    /// 
    /// Each segments starts with a segment name in [].
    /// Segments names have to be unique.
    /// 
    /// Within a segment, a list of key=value pairs follows.
    /// Keys have to be unique.
    /// 
    /// Keys and segment names are mapped to lowercase.
    /// 
    /// Comment lines with ';', '#' or '//' at the beginning are ignored.
    /// </summary>
    public class CfgMan
    {
        private readonly Dictionary<string, Dictionary<string, string>> cfg = [];
        //  the segment names are stored her in their original capitalization
        //  but the segment access works based on lowercase spelling
        private readonly List<string> originalSegmentNames = [];

        public CfgMan(string fileName)
        {
            ReadConfiguration(fileName);
        }

        public string GetString(string segment, string key, string defaultValue = "")
        {
            string value = defaultValue;

            ValidateKey(key, segment);
            if (cfg.TryGetValue(segment.ToLower(), out Dictionary<string, string> dic))
            {
                if (!dic.TryGetValue(key.ToLower(), out value))
                {
                    o4($"Value [{segment}] {key} is not configured. Taking default value '{defaultValue}'");
                    value = defaultValue;
                }
            }

            return value;
        }

        public void PutString(string segment, string key, string value)
        {
            ValidateKey(key, segment);
            if (!cfg.TryGetValue(segment.ToLower(), out Dictionary<string, string> dic))
            //  segment not yet defined
            {
                dic = [];
                originalSegmentNames.Add(segment.ToLower());
                cfg[segment.ToLower()] = dic;
            }
            //  we allow insert as well as update
            dic[key.ToLower()] = value;
        }

        public void PutBool(string segment, string key, bool value)
        {
            PutString(segment, key, value ? "true" : "false");
        }

        public void PutInt(string segment, string key, int value)
        {
            PutString(segment, key, $"{value}");
        }

        public bool GetBool(string segment, string key, bool defaultValue)
        {
            string s = GetString(segment, key, defaultValue ? "1" : "0").ToLower();

            bool value = ("~1~true~yes~ja~on".IndexOf(s) > 0);

            return value;
        }

        public int GetInt(string segment, string key, int defaultValue)
        {
            string s = GetString(segment, key, defaultValue + "");

            if (!int.TryParse(s, out int value))
            {
                o4($"Could not read '{s}' as int. Taking default value {key}={defaultValue}");
                value = defaultValue;
            }

            return value;
        }

        public IEnumerable<string> SegmentNames => originalSegmentNames;

        private void ReadConfiguration(string fileName)
        {
            string segment = null;
            int eq;
            int lineCount = 0;

            try
            {
                if (Exists(fileName))
                {
                    o4($"Reading configuration file '{fileName}'");
                    StreamReader rd = OpenReader(fileName);

                    while (!rd.EndOfStream)
                    {
                        string line = rd.ReadLine().Trim();
                        lineCount++;

                        if (line.StartsWith("[") && line.EndsWith("]"))
                        {
                            segment = line.Substring(1, line.Length - 2).Trim();
                            if (cfg.ContainsKey(segment.ToLower()))
                            {
                                Raise($"Duplicate segment [{segment}]");
                            }

                            originalSegmentNames.Add(segment);

                            cfg[segment.ToLower()] = [];
                        }
                        else if (line.Equals("") || line.StartsWith(";") ||
                                 line.StartsWith("#") || line.StartsWith("//"))
                        {
                            //  ignore comment and empty lines
                        }
                        else if ((eq = line.IndexOf('=')) > 0)
                        {
                            if (segment == null)
                            {
                                Raise("Segment header [] missing");
                            }
                            string key = line.Substring(0, eq).Trim().ToLower();
                            string value = line.Substring(eq + 1).Trim();

                            if (cfg[segment].ContainsKey(key))
                            {
                                Raise($"Duplicate key '{key}' in configuration segment [{segment}]");
                            }
                            PutString(segment, key, value);
                        }
                        else
                        {
                            Raise($"Unexpected configuration line");
                        }
                    }
                    rd.Close();
                    o4($"Lines read: {lineCount}");
                    o4($"Configuration segments: {originalSegmentNames.Count}");
                }
                else
                {
                    Raise($"Cannot open file");
                }
            }
            catch (Exception ex)
            {
                if (lineCount > 0)
                {
                    o($"Error in configuration file near line {lineCount}");
                }

                Fatal($"Failed to read configuration file {fileName}:\n{ex.Message}");
            }
        }

        private void ValidateKey(string key, string segment)
        {
            Check(!key.Equals(""), $"Empty configuration key in segment [{segment}]");
            Check(!key.Contains("="), $"Configuration key '{key}' in segment [{segment}] contains '='");
        }
    }
}
