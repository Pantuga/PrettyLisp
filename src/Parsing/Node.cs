using System.Text;

namespace PrettyLisp.Parsing
{
    public enum NodeType
    {
        None,
        Program,
        Array,
        Number,
        String,
        Function,
        Variable,
        Expression
    }
    public struct FunctionData(string name, INode[] args)
    {
        public string Name = name;
        public INode[] Arguments = args;
        public static explicit operator (string, INode[])(FunctionData f) => (f.Name, f.Arguments);
    }
    public interface INode
    {
        string Name { get; }
        public NodeType Type { get; }
        public uint Line { get; }
        public object Value { get; }
        public string ToString(string indent);
    }
    public class BaseNode<T>(T value, uint line, NodeType type = NodeType.None) : INode where T : notnull
    {
        public virtual string Name => Type + ": " + _value;
        public NodeType Type { get; } = type;
        protected T _value = value;
        public uint Line { get; } = line;
        public object Value => _value;
        public virtual string ToString(string indent)
            => $"{indent}{{\n" +
            $"{indent + "  "}Type: {Type}\n" +
            $"{indent + "  "}Value: {_value}\n" +
            $"{indent}}},";
        public override string ToString()
            => ToString("");
    }
    public class ArrayNode(INode[] value, uint line)
        : BaseNode<INode[]>(value, line, NodeType.Array), INode
    {
        public override string ToString(string indent)
        {
            StringBuilder sb = new();
            foreach (INode node in _value)
                sb.AppendLine(node.ToString(indent + "    "));

            string val = sb.Length != 0 ? $"[\n{sb}{indent + "  "}]" : "[]";
            return
                $"{indent}{{\n" +
                $"{indent + "  "}Type: {Type}\n" +
                $"{indent + "  "}Value: {val}\n" +
                $"{indent}}},";
        }
    }
    public class ExpressionNode(INode[] value, uint line)
        : BaseNode<INode[]>(value, line, NodeType.Expression), INode
    {
        public override string ToString(string indent)
        {
            StringBuilder sb = new();
            foreach (INode node in _value)
                sb.AppendLine(node.ToString(indent + "    "));

            string val = sb.Length != 0 ? $"[\n{sb}{indent + "  "}]" : "[]";
            return
                $"{indent}{{\n" +
                $"{indent + "  "}Type: {Type}\n" +
                $"{indent + "  "}Value: {val}\n" +
                $"{indent}}},";
        }
    }
    public class NumberNode(double value, uint line)
        : BaseNode<double>(value, line, NodeType.Number), INode
    { }
    public class StringNode(string value, uint line)
        : BaseNode<string>(value, line, NodeType.String), INode
    {
        public override string Name => $"{Type}: \"{_value.Replace("\n", "\\n")}\"";

        public override string ToString(string indent)
            => $"{indent}{{\n" +
            $"{indent + "  "}Type: {Type}\n" +
            $"{indent + "  "}Value: \"{_value.Replace("\n", "\\n")}\"\n" +
            $"{indent}}},";
    }
    public class VariableNode(string name, uint line)
        : BaseNode<string>(name, line, NodeType.Variable), INode
    { }

    public class FunctionNode(string name, INode[] args, uint line)
        : BaseNode<FunctionData>(new FunctionData(name, args), line, NodeType.Function), INode
    {
        public override string Name => $"{Type}: {_value.Name}({Program.ArrayToStr(_value.Arguments, n=>n.Name)})";
        public override string ToString(string indent)
        {
            StringBuilder sb = new();
            foreach (INode node in _value.Arguments)
                sb.AppendLine(node.ToString(indent + "    "));

            string args = sb.Length != 0 ? $"[\n{sb}{indent + "  "}]" : "[]";
            return
                $"{indent}{{\n" +
                $"{indent + "  "}Type: {Type}\n" +
                $"{indent + "  "}Name: {_value.Name}\n" +
                $"{indent + "  "}Arguments: {args}\n" +
                $"{indent}}},";
        }
    }
    public class ProgramNode(INode[] value)
        : BaseNode<INode[]>(value, 0, NodeType.Program), INode
    {
        public override string ToString(string indent)
        {
            StringBuilder sb = new();
            foreach (INode node in _value)
                sb.AppendLine(node.ToString(indent));
;
            return sb.Length != 0 ? $"{sb.ToString()[0..^1]}" : $"{indent}Empty Program";
        }
    }
}
