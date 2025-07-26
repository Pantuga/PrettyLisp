using PrettyLisp.Evaluation;
using PrettyLisp.Lexing;
using PrettyLisp.Parsing;
using System.Text;

namespace PrettyLisp
{
    internal class Program
    {
        public static bool Debug = false;
        public static bool Repl = false;
        public static string ArrayToStr<T>(T[] array, Func<T, object> itemProcessing)
        {
            StringBuilder sb = new();
            for (int i = 0; i < array.Length; i++)
            {
                sb.Append(itemProcessing(array[i]));
                if (i < array.Length - 1) sb.Append(", ");
            }
            return sb.ToString();
        }
        public static void LogEval(ulong scope, string str, bool debugonly = true)
        {
            if (!Debug && debugonly) return;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(new string(' ', (int)scope) + scope + ": " + str);
            Console.ResetColor();

        }
        static void Log(object str, ConsoleColor color = ConsoleColor.White, bool debugonly = true)
        {
            if (!Debug && debugonly) return;
            Console.ForegroundColor = color;
            Console.WriteLine(str);
            Console.ResetColor();
        }
        static void LogArray<T>(T[] array, bool debugonly = true, string indent = "| ")
        {
            if (!Debug && debugonly) return;
            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (T item in array)
            {
                if (item == null)
                    Console.WriteLine(indent + "null");
                else if (item is string str)
                    Console.WriteLine(indent + '"' + str.Replace("\n", "\\n") + '"');
                else
                    Console.WriteLine(indent + item.ToString()!.Replace("\n", "\n" + indent));
            }
            Console.ResetColor();
        }
        static string ReadLines(string msg = ">>", string indent = "..")
        {
            Console.Write(msg);
            string inp = Console.ReadLine()!;

            if (inp.EndsWith('\\'))
                return inp[0..^1] + '\n' + ReadLines(indent);
            else
                return inp;
        }

        static void Main(string[] args)
        {
            string input;
            Interpreter interpreter = new();

            if (args.Length != 0)
            {
                try
                {
                    input = File.ReadAllText(args[0]);
                    Token[] tokenized = Lexer.Tokenize(input);
                    ProgramNode parsed = Parser.ParseProgram(tokenized);
                    interpreter.RunProgram(parsed);
                }
                catch (Exception e)
                {
                    Log("Error: " + e.Message, ConsoleColor.Red, false);
                }
                Console.ReadKey();
                return;
            }
            else
            {
                Interpreter.ConsoleColor = ConsoleColor.Green;
                Repl = true;
                while (true)
                {
                    input = ReadLines();

                    if (input == "") continue;
                    if (input == "exit") return;
                    if (input == "reset")
                    {
                        interpreter = new Interpreter();
                        Log("Interpreter Reset", ConsoleColor.Yellow, false);
                        continue;
                    }
                    if (input == "file") input = File.ReadAllText(Console.ReadLine()!);
                    if (input == "debug")
                    {
                        Debug = !Debug;
                        Log("Debug Mode " + (Debug ? "Enabled" : "Disabled"), ConsoleColor.Yellow, false);
                        continue;
                    }
                    try
                    {
                        Log("Lexing:");
                        Token[] tokenized = Lexer.Tokenize(input);
                        LogArray(tokenized);

                        Log("Parsing:");
                        ProgramNode parsed = Parser.ParseProgram(tokenized);
                        Log(parsed.ToString("| "), ConsoleColor.Yellow);

                        Log("Evaluating:");
                        foreach (var node in (INode[])parsed.Value)
                        {
                            Log($"Result for '{node.Name}': ", ConsoleColor.Blue);
                            Log(interpreter.Evaluate(node) ?? "null/void", ConsoleColor.Blue, false);
                        }
                        Log("Success!", ConsoleColor.Yellow);
                    }
                    catch (Exception e)
                    {
                        Log("Error: " + e.Message, ConsoleColor.Red, false);
                    }
                }
            }
        }
    }
}
