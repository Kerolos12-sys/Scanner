using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace compiler
{
    public class AutoSharpScanner
    {
        static List<(string Type, string Pattern)> tokenSpecs = new List<(string, string)>
        {
            ("WHITESPACE", @"\s+"),
            ("DATATYPE", @"\b(Gear|Speed|Model|Flag|Distance)\b"),
            ("CHECK", @"\bCheck\b"),
            ("SHIFT", @"\bShift\b"),
            ("LOOP", @"\bLoop\b"),
            ("BOOL", @"\b(true|false)\b"),
            ("NUMBER",  @"\b\d+(\.\d+)?\b"),
            ("STRING", @"""[^""]*"""),
            ("ASSIGN", @"="),
            ("SEMICOLON", @";"),
            ("LPAREN", @"\("),
            ("RPAREN", @"\)"),
            ("LBRACE", @"\{"),
            ("RBRACE", @"\}"),
            ("OP", @"[+\-*/<>!]"),
            ("IDENTIFIER", @"\b[A-Za-z_][A-Za-z0-9_]*\b"),
            ("UNKNOWN", @".")
        };

        public static List<(string Type, string Value)> Scan(string input)
        {
            var tokens = new List<(string, string)>();
            string combinedPattern = string.Join("|", tokenSpecs.ConvertAll(
                t => $"(?<{t.Type}>{t.Pattern})"
            ));

            foreach (Match m in Regex.Matches(input, combinedPattern))
            {
                string type = null;
                foreach (var t in tokenSpecs)
                {
                    if (m.Groups[t.Type].Success)
                    {
                        type = t.Type;
                        break;
                    }
                }

                if (type == "WHITESPACE") continue;
                tokens.Add((type, m.Value));
            }

            tokens.Add(("EOF", ""));
            return tokens;
        }
    }
}
