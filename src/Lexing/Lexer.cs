using System.Security.AccessControl;
using System.Text;

namespace PrettyLisp.Lexing
{
    public class Lexer
    {
        public static char[] WhiteSpace { get; set; } = [.. "\n\r\t "];
        public static char Quote { get; set; } = '"';
        public static (char Start, char End) Comment { get; set; } = ('#', '$');
        public static char Escape { get; set; } = '\\';
        public static (char[] Opening, char[] Closing) Symbols { get; set; } = ([.. "([{<"], [.. ")]}>"]);
        public static char[] SpecialSymbols { get; set; } = [.. ":.<>+-*/=!%&|"];
        public static Dictionary<char, char> EscapeCodes { get; set; } = new()
        {
            { '0', '\0' },
            { 'n', '\n' },
            { 'r', '\r' },
            { 't', '\t' },
            { '!', (char)7 },
        };
        public static string Sanitize(string str)
            => str.Replace("\r\n", "\n") + '\0';

        public static Token[] Tokenize(string code)
        {
            code = Sanitize(code);
            List<Token> output = [];

            uint line = 0;
            void checkLine(char ch) { if (ch == '\n') line++; }

            for (int i = 0; i < code.Length; i++)
            {
                if (code[i] == '\0') break; // End of file

                // Comments
                if (code[i] == Comment.Start)
                {
                    while (
                        i < code.Length &&
                        code[i] != Comment.End &&
                        code[i] != '\n' &&
                        code[i] != '\0'
                    )
                    { i++; }

                    checkLine(code[i]);
                }

                // White Spaces
                else if (WhiteSpace.Contains(code[i]))
                {
                    while (WhiteSpace.Contains(code[i]))
                    {
                        checkLine(code[i]);
                        i++;
                    }
                    i--;
                }

                // Symbols
                else if (Symbols.Opening.Contains(code[i]))
                {
                    int idx = Array.FindIndex(Symbols.Opening, c => c == code[i]);
                    output.Add(new Token(TokenType.OpenFunction + (idx * 2), code[i].ToString(), line));
                }
                else if (Symbols.Closing.Contains(code[i]))
                {
                    int idx = Array.FindIndex(Symbols.Closing, c => c == code[i]);
                    output.Add(new Token(TokenType.CloseFunction + (idx * 2), code[i].ToString(), line));
                }


                // Number Literals
                else if
                (
                    char.IsDigit(code[i]) ||
                    (
                        i + 1 < code.Length &&
                        (code[i] == '-' || code[i] == '+') &&
                        char.IsDigit(code[i + 1])
                    )
                )
                {
                    StringBuilder sb = new();

                    while (i < code.Length && (char.IsDigit(code[i]) || code[i] == '.' || code[i] == 'e' || (sb.Length == 0)))
                    {
                        sb.Append(code[i] == '.' ? ',' : code[i]);
                        i++;
                    }
                    i--;

                    output.Add(new Token(TokenType.IntLiteral, sb.ToString(), line));
                }

                // Character (Number) Literals
                else if (code[i] == Escape)
                {
                    i++;
                    if (code[i] != '\0')
                        output.Add(new Token(TokenType.CharLiteral, code[i].ToString(), line));
                }

                // String Literals
                else if (code[i] == Quote)
                {
                    StringBuilder sb = new();

                    i++; // skip opening quote
                    while (i < code.Length && code[i] != Quote)
                    {
                        if (code[i] == '\0')
                            throw new Exception("Unclosed String Literal");

                        if (code[i] == Escape)
                        {
                            i++;
                            if (EscapeCodes.TryGetValue(code[i], out char ch))
                                sb.Append(ch);
                            else
                                sb.Append(code[i]);
                        }
                        else
                        {
                            sb.Append(code[i]);
                        }
                        i++;
                        checkLine(code[i]);
                    }

                    output.Add(new Token(TokenType.StringLiteral, sb.ToString(), line));
                }

                // Special Symbols (Identifiers)
                else if (SpecialSymbols.Contains(code[i]))
                {
                    StringBuilder sb = new();

                    while (i < code.Length && SpecialSymbols.Contains(code[i]))
                    {
                        sb.Append(code[i]);
                        i++;
                    }
                    i--;

                    output.Add(new Token(TokenType.Identifier, sb.ToString(), line));
                }

                // Words (Identifiers)
                else
                {
                    StringBuilder sb = new();
                    while (
                        i < code.Length &&
                        !WhiteSpace.Contains(code[i]) &&
                        !Symbols.Opening.Contains(code[i]) &&
                        !Symbols.Closing.Contains(code[i]) &&
                        !SpecialSymbols.Contains(code[i]) &&
                        code[i] != Quote &&
                        code[i] != '\0'
                    )
                    {
                        sb.Append(code[i]);
                        i++;
                    }
                    i--;
                    output.Add(new Token(TokenType.Identifier, sb.ToString(), line));
                }
            }

            return [.. output];
        }
    }
}
