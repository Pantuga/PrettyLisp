namespace PrettyLisp.Lexing
{
    public enum TokenType
    {
        OpenFunction,
        CloseFunction,

        OpenArray,
        CloseArray,

        OpenBinaryOp,
        CloseBinaryOp,

        OpenExpr,
        CloseExpr,

        Identifier,
        StringLiteral,
        IntLiteral,
        CharLiteral

    }
    public struct Token(TokenType type, string value, uint line)
    {
        public uint Line = line;
        public TokenType Type = type;
        public string Value = value;

        public readonly override string ToString()
        {
            string quote = Type == TokenType.StringLiteral ? "\"" : "";

            return $"{Line}:({Type}) {quote}{Value}{quote}";
        }
    }
}
