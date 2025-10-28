using System.Text.RegularExpressions;

namespace compiler
{
    internal class Program
    {
        internal class AutoSharpScanner
        {
            static List<(string Type, string Pattern)> tokenSpecs = new List<(string, string)> {
        ("KEYWORD", @"\b(Check|Shift|Loop|Cruise|Stop|Dash|Sense|Ignite|Drive|Gear|Speed|Flag|Model|Distance|Rpm)\b"),
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

            static void Main()
            {
                string filePath = "code.txt";
                if (!File.Exists(filePath))
                {
                    Console.WriteLine("file does not exist");
                    return;
                }

                string code = File.ReadAllText(filePath);
                var tokens = Scan(code);

                Console.WriteLine("🔍 AutoSharp Scanner Output:\n");
                foreach (var token in tokens)
                {
                    Console.WriteLine($"{token.Type,-12} : {token.Value}");
                }
            }
        }
    }
}
