using PrettyLisp.Parsing;
using System.Text.RegularExpressions;
using static PrettyLisp.Program;

namespace PrettyLisp.Evaluation
{
    public class Interpreter()
    {
        private static object DebugBreakpoint() => null!;
        public static ConsoleColor ConsoleColor { get; set; } = ConsoleColor.Gray;
        //private static void Error(uint line, string message)
        //    => throw new Exception("Error at line " + line + ": " + message);
        private static void Print(object obj)
        {
            if (obj is object[] array)
            {
                PrintArray(array);
                return;
            }
            Console.ForegroundColor = ConsoleColor;
            Console.Write(obj);
            Console.ResetColor();
        }
        private static void PrintArray(object[] objs)
        {
            Console.ForegroundColor = ConsoleColor;
            Console.Write("[");
            for (int i = 0; i < objs.Length; i++)
            {
                Console.Write(objs[i]);
                if (i < objs.Length - 1) Console.Write(", ");
            }
            Console.Write("]");
            Console.ResetColor();
        }
        private class RTVariable(object? value, bool isReadonly, ulong scope)
        {
            public static RTVariable Const(object? obj) => new(obj, true, 0);
            public ulong Scope { get; } = scope;
            private object? _value = value;
            public object? Value
            {
                get => _value;
                set
                {
                    if (IsReadonly) throw new InvalidOperationException("The variable is readonly");
                    _value = value;
                }
            }
            public readonly bool IsReadonly = isReadonly;
        }
        private class RTFunction(ExpressionNode expr, string[] args)
        {
            public ExpressionNode Expression { get; } = expr;
            public string[] Arguments { get; } = args;
        }
        // variable keywords
        public const string SetKeyword = "set";
        public const string DeclareKeyword = "declare";
        public const string GlobalKeyword = "global";
        public const string ReadonlyKeyword = "readonly";
        public const string ConstKeyword = "const";
        public const string DestroyKeyword = "destroy";
        // function keywords
        public const string DefineKeyword = "define";
        public const string ReturnKeyword = "return";
        // flow keywords
        public const string IfKeyword = "if";
        public const string IfElseKeyword = "ifelse";
        public const string WhileKeyword = "while";
        public const string ForKeyword = "for";
        // array keywords
        public const string SetAtKeyword = "set_at";
        public const string AppendKeyword = "append";
        public const string PopKeyword = "pop";
        public const string AtKeyword = "at";

        private ulong _scope = 1;

        private readonly Dictionary<string, RTVariable> _variables = new()
        {
            ["true"] = Const(true),
            ["false"] = Const(false),
            ["null"] = Const(null!),
            ["NaN"] = Const(double.NaN),
            ["inf"] = Const(double.PositiveInfinity),
            ["ninf"] = Const(double.NegativeInfinity),
        };
        private readonly Dictionary<string, (Func<object[], object?> Function, ulong Scope)> _functions = new()
        {
            ["+"] = (args => (double)args[0] + (double)args[1], 0),
            [".+"] = (args => "" + args[0] + args[1], 0),
            ["-"] = (args => (double)args[0] - (double)args[1], 0),
            ["*"] = (args => (double)args[0] * (double)args[1], 0),
            ["/"] = (args => (double)args[0] / (double)args[1], 0),
            ["%"] = (args => (double)args[0] % (double)args[1], 0),
            ["**"] = (args => Math.Pow((double)args[0], (double)args[1]), 0),
            ["=="] = (args => Equals(args[0], args[1]), 0),
            ["!="] = (args => !Equals(args[0], args[1]), 0),
            [".<"] = (args => (double)args[0] < (double)args[1], 0),
            [".>"] = (args => (double)args[0] > (double)args[1], 0),
            [".<="] = (args => (double)args[0] <= (double)args[1], 0),
            [".>="] = (args => (double)args[0] >= (double)args[1], 0),
            ["&&"] = (args => GetBool(args[0]) && GetBool(args[1]), 0),
            ["||"] = (args => GetBool(args[0]) || GetBool(args[1]), 0),
            ["!"] = (args => !GetBool(args[0]), 0),
            ["char"] = (args => ((char)(double)args[0]).ToString(), 0),
            ["length"] = (args => (double)GetArray(args[0]).Length, 0),

            ["debug"] = (args => DebugBreakpoint(), 0),
            ["parse_num"] = (args =>
            {
                return double.Parse((string)args[0]);
            }
            , 0),
            ["input"] = (args =>
            {
                foreach (var obj in args) Print(obj);
                return Console.ReadLine();
            }, 0),
            ["print"] = (args =>
            {
                foreach (var obj in args) Print(obj);
                return null;
            }, 0),
            ["println"] = (args =>
            {
                foreach (var obj in args) Print(obj);
                Console.WriteLine();
                return null;
            }, 0),
        };
        private static RTVariable Const(object value)
            => RTVariable.Const(value);

        public void RunProgram(INode[] expr)
            => RunProgram(new ProgramNode(expr));

        public void RunProgram(INode expr)
        {
            foreach (var cmd in (INode[])expr.Value) Evaluate(cmd);
        }
        private static bool GetBool(object? obj)
        {
            if (obj is bool b) return b;
            if (obj is double d) return d != 0;
            if (obj is string str) return str != "";
            if (obj is object[] array) return array.Length != 0;
            if (obj == null) return false;
            return true;
        }

        private static object[] GetArray(object obj)
             => (obj is object[] array) ? array :
                (obj is string str) ? Array.ConvertAll(str.ToCharArray(), ch => (object)(double)ch) : [obj];

        private void DefineFunc(string name, ulong scope, RTFunction value)
        {
            LogEval(scope, "defined function " + name + $"({value.Arguments.Length})");
            _functions.Add(name, (new Func<object[], object?>(args => EvalRTFunc(value, args)), scope));
        }
        private void DeclareVar(string name, ulong scope, object value = null!, bool constant = false)
        {
            LogEval(scope, "declared variable " + name + " as '" + value + "'");
            _variables.Add(GetNewVarName(name, scope), new RTVariable(value, constant, scope));
        }

        private void SetVar(string name, object? value)
        {
            LogEval(_scope, "set variable " + name + " to '" + value + "'");
            _variables[FindVarName(name, _scope)].Value = value;
        }
        private static new bool Equals(object o1, object o2)
        {
            if (o1 is double d) return d == (double)o2;
            if (o1 is string str) return str.Equals((string)o2);
            if (o1 is bool b) return !(b ^ (bool)o2);
            return false;
        }
        private string GetNewVarName(string name, ulong scope)
        {
            return (scope == 0) ? name : $"{name}-{scope}";
        }
        private string FindVarName(string name, ulong scope)
        {
            // First check the current scope
            if (_variables.TryGetValue(GetNewVarName(name, scope), out var _))
                return $"{name}-{scope}";

            // Then check parent scopes
            for (ulong i = scope - 1; i > 0; i--)
            {
                if (_variables.TryGetValue(GetNewVarName(name, i), out var _))
                    return $"{name}-{i}";
            }

            // Finally check global scope
            if (_variables.TryGetValue(name, out var _))
                return name;

            throw new Exception($"Variable {name} does not exist");
        }

        private RTVariable GetVar(string name, ulong scope)
        {
            LogEval(scope, "accessed variable " + name);

            // First check the current scope
            if (_variables.TryGetValue(GetNewVarName(name, scope), out var currentScopeVar))
                return currentScopeVar;

            // Then check parent scopes
            for (ulong i = scope - 1; i > 0; i--)
            {
                if (_variables.TryGetValue(GetNewVarName(name, i), out var parentVar))
                    return parentVar;
            }

            // Finally check global scope
            if (_variables.TryGetValue(name, out var globalVar))
                return globalVar;

            throw new Exception($"Variable {name} does not exist");
        }

        public object? Evaluate(INode expr)
        {
            var result = Evaluate(expr, out _, out bool returned);
            if (returned) throw new Exception("Unexpected return");
            return result;
        }
        public object?[] EvaluateEach(INode[] expr)
        {
            var result = new object?[expr.Length];
            for (int i = 0; i < expr.Length; i++)
                result[i] = Evaluate(expr[i]);
            return result;
        }
        public object? Evaluate(INode expr, out object? returnVal, out bool returned)
        {
            ulong scope = _scope;
            _scope++;

            returnVal = null;
            returned = false;

            LogEval(scope, "evaluating " + expr.Name);

            object? result = expr.Type switch
            {
                NodeType.Array => EvalArray(expr),
                NodeType.Number => expr.Value,
                NodeType.String => expr.Value,
                NodeType.Variable => GetVar((string)expr.Value, scope).Value,
                NodeType.Function => EvalFunction(expr, scope, out returnVal, out returned),
                NodeType.Expression => EvalExpr(expr),
                NodeType.Program => EvaluateEach((INode[])expr.Value),
                _ => throw new Exception("Unexpected Node Type: " + expr.Type)
            };

            LogEval(scope, "evaluated: " + ((result is string str) ? $"\"{str.Replace("\n", "\\n")}\"" : (result ?? "null/void")));

            _scope--;
            if (expr.Type != NodeType.Number && expr.Type != NodeType.String && expr.Type != NodeType.Variable)
            {
                var varsToRemove = _variables.Where(x => x.Value.Scope > _scope).ToList();
                foreach (var pair in varsToRemove)
                {
                    _variables.Remove(pair.Key);
                    LogEval(_scope, "removed variable " + pair.Key);

                }

                var funcsToRemove = _functions.Where(x => x.Value.Scope > _scope).ToList();
                foreach (var pair in funcsToRemove)
                {
                    _functions.Remove(pair.Key);
                    LogEval(_scope, "removed function " + pair.Key);
                }
            }
            return result;
        }
        private object? EvalFunction(INode expr, ulong scope, out object? returnVal, out bool returned)
        {
            var name = ((FunctionData)expr.Value).Name;
            var args = ((FunctionData)expr.Value).Arguments;
            returnVal = null;
            returned = false;
            object evalr(int idx, ref object? returnVal, out bool returned) => Evaluate(args[idx], out returnVal, out returned)!;
            object eval(int idx) => Evaluate(args[idx])!;

            switch (name)
            {
                case "=":
                case SetKeyword:
                    SetVar((string)((VariableNode)args[0]).Value, eval(1)!);
                    return null;

                case ":=":
                case DeclareKeyword:
                    if (args.Length > 1)
                        DeclareVar((string)((VariableNode)args[0]).Value, scope, eval(1)!);
                    else
                        DeclareVar((string)((VariableNode)args[0]).Value, scope);
                    return null;

                case "::=":
                case GlobalKeyword:
                    if (args.Length > 1)
                        DeclareVar((string)((VariableNode)args[0]).Value, 0, eval(1)!);
                    else
                        DeclareVar((string)((VariableNode)args[0]).Value, 0);
                    return null;

                case ".=":
                case ReadonlyKeyword:
                    DeclareVar((string)((VariableNode)args[0]).Value, scope, eval(1)!, true);
                    return null;

                case "..=":
                case ConstKeyword:
                    DeclareVar((string)((VariableNode)args[0]).Value, 0, eval(1)!, true);
                    return null;

                case DestroyKeyword:
                    string varName = (string)((VariableNode)args[0]).Value;
                    Regex varIdx = new($"{varName}(-[0-9]+)?");
                    var varsToRm = _variables.Where(v => varIdx.IsMatch(v.Key));
                    foreach (var v in varsToRm) _variables.Remove(v.Key);
                    return null;

                case IfKeyword:
                    if (GetBool(eval(0))) evalr(1, ref returnVal, out returned);
                    return null;

                case IfElseKeyword:
                    if (GetBool(eval(0))) evalr(1, ref returnVal, out returned);
                    else evalr(2, ref returnVal, out returned);
                    return null;

                case WhileKeyword:
                    while (GetBool(eval(0))) evalr(1, ref returnVal, out returned);
                    return null;

                case ForKeyword:
                    eval(0);
                    while (GetBool(eval(1)))
                    {
                        evalr(3, ref returnVal, out returned);
                        eval(2);
                    }
                    return null;

                case AppendKeyword:
                    return (object[])[.. GetArray(eval(0)!), eval(1)!];

                case PopKeyword:
                    return GetArray(eval(0)!)[..^1];

                case SetAtKeyword:
                    object[] array = GetArray(eval(0));
                    array[(int)(double)eval(1)] = eval(2);
                    SetVar((string)((VariableNode)args[0]).Value, array);
                    return null;

                case DefineKeyword:
                    string funcName = (string)((VariableNode)args[0]).Value;
                    string[] funcArgs = Array.ConvertAll(GetArray(eval(1)), o => o.ToString()!);
                    ExpressionNode funcExpr = (ExpressionNode)args[2];

                    DefineFunc(funcName, scope, new RTFunction(funcExpr, funcArgs));
                    return null;

                case ReturnKeyword:
                    returnVal = eval(0);
                    returned = true;
                    return returnVal;

                case AtKeyword:
                    int index = Convert.ToInt32(eval(1)!);
                    if (index >= 0)
                        return GetArray(eval(0)!)[index];
                    else
                        return GetArray(eval(0)!)[^-index];

                default:
                    object[] evaledArgs = new object[args.Length];
                    for (int i = 0; i < args.Length; i++) evaledArgs[i] = eval(i)!;
                    return _functions[name].Function(evaledArgs);
            }
        }
        private object?[] EvalArray(INode expr)
        {
            INode[] array = (INode[])expr.Value;
            object?[] result = new object[array.Length];

            for (int i = 0; i < array.Length; i++)
                result[i] = Evaluate(array[i]);

            return [.. result];
        }
        private object? EvalRTFunc(RTFunction function, object?[] argValues)
        {
            for (int i = 0; i < function.Arguments.Length; i++)
                DeclareVar(function.Arguments[i], _scope, argValues[i]!);

            return EvalExpr(function.Expression);
        }
        private object? EvalExpr(INode expr)
        {
            object? result = null;
            foreach (var node in (INode[])((ExpressionNode)expr).Value)
            {
                Evaluate(node, out result, out bool returned);
                if (returned) break;
            }
            return result;
        }
    }
}