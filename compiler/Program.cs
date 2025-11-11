using compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace AutoSharpCompiler
{
    internal class Program
    {
        static void Main()
        {
            string filePath = "code.txt";
            if (!File.Exists(filePath))
            {
                Console.WriteLine("file does not exist");
                return;
            }

            string code = File.ReadAllText(filePath);

            var tokens = AutoSharpScanner.Scan(code);

            Console.WriteLine("Tokens:\n");
            foreach (var token in tokens)
            {
                Console.WriteLine($"{token.Type,-12} : {token.Value}");
            }

            Console.WriteLine("\n Parse Tree:\n");

            try
            {
                
                var parser = new AutoSharpParser(tokens);
                var tree = parser.ParseProgram();   // مهم جداً
                AutoSharpParser.PrintTree(tree);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {ex.Message}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
