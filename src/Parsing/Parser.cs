using PrettyLisp.Lexing;

namespace PrettyLisp.Parsing
{
    public class Parser(Token[] code)
    {
        private readonly Token[] _tokens = code;
        private int _index = 0;
        private Token Peek => _tokens[_index];
        public bool Ended => _index > _tokens.Length - 1;
        private string ErrorMsg(string message)
            => "Error at line " + Peek.Line + ": " + message;
        public static ProgramNode ParseProgram(Token[] code)
            => new(new Parser(code).ParseThis());

        private void Expect(TokenType type)
        {
            if (Peek.Type != type)
                throw new Exception(ErrorMsg($"Unexpected Token Type: {Peek.Type}, expected {type}"));
        }
        public INode[] ParseThis()
        {
            List<INode> result = [];

            while (!Ended)
            {
                result.Add(ParseAtom());
                _index++;
            }
            return [.. result];
        }
        private INode ParseAtom()
        {
            return Peek.Type switch
            {
                TokenType.Identifier => new VariableNode(Peek.Value, Peek.Line),
                TokenType.StringLiteral => new StringNode(Peek.Value, Peek.Line),
                TokenType.IntLiteral => new NumberNode(double.Parse(Peek.Value), Peek.Line),
                TokenType.CharLiteral => new NumberNode(Peek.Value[0], Peek.Line),
                TokenType.OpenFunction => ParseFunction(),
                TokenType.OpenBinaryOp => ParseBinaryOp(),
                TokenType.OpenArray => ParseArray(),
                TokenType.OpenExpr => ParseExpr(),
                _ => throw new Exception(ErrorMsg("Unexpected Token Type: " + Peek.Type))
            };

        }
        private FunctionNode ParseFunction()
        {
            Expect(TokenType.OpenFunction);
            uint line = Peek.Line;

            _index++;
            Expect(TokenType.Identifier);
            string name = Peek.Value;
            List<INode> arguments = [];
            _index++;
            while (Peek.Type != TokenType.CloseFunction)
            {
                arguments.Add(ParseAtom());
                _index++;
            }
            return new FunctionNode(name, [.. arguments], line);
        }
        private FunctionNode ParseBinaryOp()
        {
            Expect(TokenType.OpenBinaryOp);
            uint line = Peek.Line;

            _index++;
            INode[] arguments = new INode[2];
            arguments[0] = ParseAtom();

            _index++;
            if (Peek.Type == TokenType.IntLiteral)
            {
                arguments[1] = ParseAtom();

                string operation;
                if (Peek.Value[0] == '-' || Peek.Value[0] == '+') operation = "+";
                else if (arguments[0].Type == NodeType.Variable) operation = "*";
                else throw new Exception(ErrorMsg("Invalid binary operation"));

                _index++;
                Expect(TokenType.CloseBinaryOp);
                return new FunctionNode(operation, arguments, line);
            }

            Expect(TokenType.Identifier);
            string name = Peek.Value;

            _index++;
            arguments[1] = ParseAtom();

            _index++;
            Expect(TokenType.CloseBinaryOp);

            return new FunctionNode(name, [.. arguments], line);
        }
        private ArrayNode ParseArray()
        {
            Expect(TokenType.OpenArray);
            uint line = Peek.Line;

            _index++;
            List<INode> result = [];
            while (Peek.Type != TokenType.CloseArray)
            {
                result.Add(ParseAtom());
                _index++;
            }
            return new ArrayNode([.. result], line);
        }
        private ExpressionNode ParseExpr()
        {
            Expect(TokenType.OpenExpr);
            uint line = Peek.Line;

            _index++;
            List<INode> result = [];
            while (Peek.Type != TokenType.CloseExpr)
            {
                result.Add(ParseAtom());
                _index++;
            }
            return new ExpressionNode([.. result], line);
        }
    }
}
