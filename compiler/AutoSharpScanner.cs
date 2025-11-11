using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace compiler
{
    public class AutoSharpScanner
    {
        static List<(string Type, string Pattern)> tokenSpecs = new List<(string, string)>
        {
            ("DATATYPE", @"\b(Gear|Speed|Model|Flag|Distance)\b"), // AutoSharp keywords
            ("KEYWORD", @"\b(Check|Shift|Loop|Cruise|Stop|Dash|Sense|Ignite|Drive)\b"),
            ("NUMBER",  @"\b\d+(\.\d+)?\b"),
            ("IDENTIFIER", @"\b[A-Za-z_][A-Za-z0-9_]*\b"),
            ("STRING", @"""[^""]*"""),
            ("ASSIGN", @"="),
            ("OP", @"[+\-*/<>!]"),
            ("LPAREN", @"\("),
            ("RPAREN", @"\)"),
            ("LBRACE", @"\{"),
            ("RBRACE", @"\}"),
            ("SEMICOLON", @";"),
            ("WHITESPACE", @"\s+"),
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
                string type = "";
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

            return tokens;
        }






    }
}
