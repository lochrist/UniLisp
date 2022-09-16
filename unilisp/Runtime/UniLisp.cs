using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UniLisp
{
    // Port from http://norvig.com/lispy2.html
    public enum LispType
    {
        Nil,
        EoF,
        Symbol,
        Number,
        Boolean,
        List,
        String,
        Procedure,
        Native
    }

    public struct LispValue
    {
        public LispType type;
        public int intValue;
        public float floatValue;
        public List<LispValue> listValue;
        public object objValue;

        public static LispValue Nil = new LispValue() { type = LispType.Nil };
        public static LispValue False = Create(false);
        public static LispValue True = Create(true);
        public static LispValue EoF = new LispValue() { type = LispType.EoF };

        public static LispValue Create(string v)
        {
            return new LispValue() { type = LispType.String, objValue = v };
        }

        public static LispValue Create(bool v)
        {
            return new LispValue() { type = LispType.Boolean, intValue = v ? 1 : 0 };
        }

        public static LispValue Create(float v)
        {
            return new LispValue() { type = LispType.Number, floatValue = v };
        }

        public static LispValue Create(int v)
        {
            return new LispValue() { type = LispType.Number, floatValue = v };
        }

        public static LispValue CreateSymbol(string v)
        {
            return new LispValue() { type = LispType.Symbol, objValue = v, intValue = v.GetHashCode() };
        }

        public static LispValue Create(Procedure proc)
        {
            return new LispValue() { type = LispType.Procedure, objValue = proc };
        }

        public static LispValue Create(IEnumerable<LispValue> v)
        {
            return new LispValue() { type = LispType.List, listValue = v.ToList() };
        }

        public static LispValue Create(List<LispValue> v)
        {
            return new LispValue() { type = LispType.List, listValue = v };
        }

        public static LispValue Create(params LispValue[] v)
        {
            return new LispValue() { type = LispType.List, listValue = v.ToList() };
        }

        public static LispValue CreateList(LispValue v, IEnumerable<LispValue> rest)
        {
            return Create(new[] { v }.Concat(rest));
        }

        public static LispValue CreateList(LispValue v1, LispValue v2, IEnumerable<LispValue> rest)
        {
            return Create(new[] { v1, v2 }.Concat(rest));
        }

        public static LispValue CreateList(LispValue v1, LispValue v2, LispValue v3, IEnumerable<LispValue> rest)
        {
            return Create(new[] { v1, v2, v3 }.Concat(rest));
        }

        public static string Stringigy(LispValue obj)
        {
            switch (obj.type)
            {
                case LispType.Boolean:
                    return obj.intValue == 0 ? "#f" : "#t";
                case LispType.Symbol:
                    return (string)obj.objValue;
                case LispType.Number:
                    return obj.floatValue.ToString();
                case LispType.Nil:
                    return "nil";
                case LispType.EoF:
                    return "eof";
                case LispType.String:
                    return $"\"{(string)obj.objValue}\"";
                case LispType.Procedure:
                    var proc = (Procedure)obj.objValue;
                    return proc.IsCallable ? "Native function" : "function";
                case LispType.List:
                    var values = string.Join(' ', obj.listValue.Select(v => Stringigy(v)));
                    return $"({values})";
            }
            return "<Unhandled>";
        }

        public static object ToObject(LispValue obj)
        {
            switch (obj.type)
            {
                case LispType.Boolean:
                    return obj.intValue != 0;
                case LispType.Number:
                    return obj.floatValue;
                case LispType.List:
                    return obj.listValue.Select(v => ToObject(v)).ToArray();
            }
            return obj.objValue;
        }

        public static LispValue FromObject(object obj)
        {
            if (obj == null)
                return Nil;
            if (obj.GetType() == typeof(int))
            {
                return Create((int)obj);
            }
            else if (obj.GetType() == typeof(float))
            {
                return Create((float)obj);
            }
            else if (obj.GetType() == typeof(bool))
            {
                return Create((bool)obj);
            }
            else if (obj.GetType() == typeof(string))
            {
                return Create((string)obj);
            }
            return new LispValue() { objValue = obj, type = LispType.Native };
        }

        public override string ToString()
        {
            return Stringigy(this);
        }
    }

    public class InPort
    {
        TextReader m_Stream;
        Regex m_Tokenizer =  new Regex(@"\s*(,@|[('`,)]|""(?:[\\].|[^\\""])*""|;.*|[^\s('""`,;)]*)(.*)");
        string m_Line;
        public InPort(TextReader stream)
        {
            m_Stream = stream;
        }

        public char ReadChar()
        {
            return (char)m_Stream.Read();
        }

        public string NextToken()
        {
            while (true)
            {
                if (string.IsNullOrEmpty(m_Line))
                    m_Line = m_Stream.ReadLine();
                if (string.IsNullOrEmpty(m_Line))
                    return null;
                var m = m_Tokenizer.Match(m_Line);
                string token = null;
                if (m.Success)
                {
                    token = m.Groups[1].Value;
                    var newLine = m.Groups[2].Value;
                    if (newLine == m_Line)
                    {
                        throw new LispSyntaxException($"Cannot parse line: {m_Line}");
                    }
                    m_Line = m.Groups[2].Value;
                }
                else
                {
                    throw new LispSyntaxException($"Cannot parse line: {m_Line}");
                }

                if (!string.IsNullOrEmpty(token) && token[0] != ';')
                {
                    return token;
                }
            }
        }
    }

    public class OutPort
    {
        StreamWriter m_Stream;
        public OutPort(StreamWriter stream)
        {
            m_Stream = stream;
        }

        public void Write(string w)
        {
            m_Stream.Write(w);
        }
    }

    public class LispSyntaxException : System.Exception
    {
        public LispSyntaxException(string msg)
            : base(msg)
        {
        }
    }

    public class LispRuntimeException : System.Exception
    {
        public LispRuntimeException(string msg)
            : base(msg)
        {
        }
    }

    public class Env
    {
        Env m_Outer;
        Dictionary<string, LispValue> m_Values = new Dictionary<string, LispValue>();

        public IEnumerable<KeyValuePair<string, LispValue>> entries => m_Values;

        public Env()
        {
        }

        public Env(LispValue parameters, LispValue args, Env outer = null)
        {
            m_Outer = outer;
            if (parameters.type == LispType.List)
            {
                if (parameters.listValue.Count != args.listValue.Count)
                    throw new LispRuntimeException($"Missed match parameters in Env: {parameters} != {args}");
                for(var i = 0; i < parameters.listValue.Count; ++i)
                {
                    m_Values[parameters.listValue[i].ToString()] = args.listValue[i];
                }
            }
            else if (parameters.type == LispType.Symbol)
            {
                m_Values[parameters.ToString()] = args;
            }
        }

        public LispValue Find(string var)
        {
            if (m_Values.TryGetValue(var, out var value))
                return value;
            if (m_Outer == null)
                throw new LispRuntimeException($"Cannot find value for {var}");
            return m_Outer.Find(var);
        }

        public bool TryFind(string var, out LispValue symbolValue)
        {
            if (m_Values.TryGetValue(var, out symbolValue))
                return true;
            if (m_Outer == null)
                return false;
            return m_Outer.TryFind(var, out symbolValue);
        }

        public void Update(string binding, LispValue value)
        {
            m_Values[binding] = value;
        }
    }

    public class Procedure
    {
        public LispValue parameters;
        public LispValue expr;
        public Env env;
        public Func<LispContext, List<LispValue>, LispValue> func;
        public bool IsCallable => func != null;
        public bool lateParamsEval;

        public Procedure(LispValue parameters, LispValue expr, Env env)
        {
            this.parameters = parameters;
            this.expr = expr;
            this.env = env;
        }

        public Procedure(Func<LispContext, List<LispValue>, LispValue> f)
        {
            func = f;
        }

        public LispValue Invoke(LispContext ctx, List<LispValue> args)
        {
            if (IsCallable)
            {
                return func(ctx, args);
            }
            return ctx.Eval(expr, new Env(parameters, LispValue.Create(args), env));
        }
    }

    public class LispContext
    {
        Env m_GlobalEnv;

        Dictionary<string, LispValue> m_StringTable = new Dictionary<string, LispValue>();
        Dictionary<string, LispValue> m_MacroTable = new Dictionary<string, LispValue>();

        public delegate bool SymbolResolver(LispContext ctx, string symbolName, out LispValue value);

        Dictionary<string, SymbolResolver> m_SymbolResolvers = new Dictionary<string, SymbolResolver>();
        Dictionary<string, LispValue> m_Quotes;

        LispValue quote;
        LispValue @if;
        LispValue set;
        LispValue define;
        LispValue lambda;
        LispValue begin;
        LispValue definemacro;
        LispValue quasiquote;
        LispValue unquote;
        LispValue unquotesplicing;
        LispValue append;
        LispValue cons;

        // public IEnumerable<KeyValuePair<string, LispValue>> globalEntries => m_GlobalEnv.entries;
        public IEnumerable<KeyValuePair<string, LispValue>> globalEntries => m_GlobalEnv.entries;

        public LispContext()
        {
            m_GlobalEnv = new Env();

            @quote = GetSym("quote");
            @if = GetSym("if");
            @set = GetSym("set!");
            @define = GetSym("define");
            @lambda = GetSym("lambda");
            @begin = GetSym("begin");
            @definemacro = GetSym("define-macro");
            @quasiquote = GetSym("quasiquote");
            @unquote = GetSym("unquote");
            @unquotesplicing = GetSym("unquote-splicing");
            @append = GetSym("append");
            @cons = GetSym("cons");

            m_Quotes = new Dictionary<string, LispValue>()
            {
                {"'", @quote},
                {"`", quasiquote},
                {",", unquote},
                {",@", unquotesplicing}
            };

            InitGlobalEnv();
        }

        public LispValue Parse(string content)
        {
            var inPort = new InPort(new StringReader(content));
            return Parse(inPort);
        }

        public LispValue Parse(InPort inport)
        {
            var expr = ReadExpression(inport);
            var expandedExpr = Expand(expr, true);
            return expandedExpr;
        }

        public LispValue Eval(string content, Env env = null)
        {
            var expr = Parse(content);
            return Eval(expr, env);
        }

        public LispValue Eval(LispValue expr, Env env = null)
        {
            env = env ?? m_GlobalEnv;
            // While loop semi handles tail recursion
            while(true)
            {
                if (expr.type == LispType.Symbol)
                {
                    // variable reference
                    var symbolName = expr.ToString();
                    if (env.TryFind(symbolName, out var value))
                        return value;

                    if (TryResolveSymbol(symbolName, out value))
                    {
                        m_GlobalEnv.Update(symbolName, value);
                        return value;
                    }

                    throw new LispRuntimeException($"Cannot resolve symbol {symbolName}");
                }

                // constant literal
                if (expr.type != LispType.List)
                    return expr;

                var op = expr.listValue[0];
                if (EqSym(op, quote))
                {
                    // (quote exp)
                    return expr.listValue[1];
                }
                else if (EqSym(op, @if))
                {
                    // (if test conseq alt)
                    var testResult = Eval(expr.listValue[1], env);
                    expr = IsTruish(testResult) ? expr.listValue[2] : expr.listValue[3];
                }
                else if (EqSym(op, set))
                {
                    // (set! var exp)
                    var valueToSet = Eval(expr.listValue[2], env);
                    env.Update(expr.listValue[1].ToString(), valueToSet);
                    return LispValue.Nil;
                }
                else if (EqSym(op, define))
                {
                    // (define var exp)
                    var varName = expr.listValue[1].ToString();
                    var body = expr.listValue[2];
                    var bodyEval = Eval(body, env);
                    env.Update(varName, bodyEval);
                    return LispValue.Nil;
                }
                else if (EqSym(op, lambda))
                {
                    // (lambda(var *) exp)
                    var proc = new Procedure(expr.listValue[1], expr.listValue[2], env);
                    return LispValue.Create(proc);
                }
                else if (EqSym(op, begin))
                {
                    // (begin exp+)

                    // Execute until the element BEFORE last
                    for (var i = 1; i < expr.listValue.Count - 1; ++i)
                    {
                        var statement = expr.listValue[i];
                        Eval(statement, env);
                    }

                    // execute last element in the next loop to ensure proper tail recursion
                    expr = expr.listValue.Last();
                }
                else
                {
                    // (proc exp*)
                    op = Eval(op, env);
                    if (op.type != LispType.Procedure)
                        throw new LispRuntimeException($"Cannot invoke: {op}");
                    var proc = (Procedure)op.objValue;
                    var args = expr.listValue.Skip(1);
                    var evaluatedArguments = proc.lateParamsEval ? 
                        args.ToList() :
                        args.Select(v => Eval(v, env)).ToList();
                    if (proc.IsCallable)
                    {
                        return proc.Invoke(this, evaluatedArguments);
                    }
                    else
                    {
                        // this ensure tail recursion with the while statement above.
                        expr = proc.expr;
                        env = new Env(proc.parameters, LispValue.Create(evaluatedArguments), proc.env);
                    }
                }
            }
        }

        public LispValue LoadFile(string inFile)
        {
            return Eval(new InPort(new StreamReader(inFile)));
        }

        public LispValue Eval(InPort inport)
        {
            var x = Parse(inport);
            return Eval(x, m_GlobalEnv);
        }

        public void Repl(string prompt, InPort inport, OutPort outport)
        {
            while (true)
            {
                try
                {
                    if (!string.IsNullOrEmpty(prompt) && outport != null)
                        outport.Write(prompt);
                    var x = Parse(inport);
                    var val = Eval(x, m_GlobalEnv);
                    outport?.Write(LispValue.Stringigy(val));
                }
                catch (LispSyntaxException)
                {

                }
                catch (LispRuntimeException)
                {

                }
                catch (System.Exception)
                {

                }
            }
        }

        public LispValue ReadExpression(InPort inport)
        {
            var token = inport.NextToken();
            if (token == null)
                return LispValue.EoF;

            return ReadToken(inport, token);
        }

        #region Customization
        public LispValue RegisterProcedure(string name, Func<LispContext, List<LispValue>, LispValue> func, bool lateParamsEval = false)
        {
            var sym = GetSym(name);
            var proc = new Procedure(func) { lateParamsEval = lateParamsEval };
            var procValue = LispValue.Create(proc);
            m_GlobalEnv.Update(name, procValue);
            return procValue;
        }

        public LispValue RegisterMacro(string name, Func<LispContext, List<LispValue>, LispValue> func)
        {
            var sym = GetSym(name);
            var proc = new Procedure(func);
            var procValue = LispValue.Create(proc);
            m_MacroTable[name] = procValue;
            return procValue;
        }

        public void RegisterSymbolValueGetter(string id, SymbolResolver resolver)
        {
            m_SymbolResolvers[id] = resolver;
        }
        #endregion

        private LispValue GetSym(string s)
        {
            if (!m_StringTable.TryGetValue(s, out var sym))
            {
                sym = LispValue.CreateSymbol(s);
                m_StringTable.Add(s, sym);
            }
            return sym;
        }

        private LispValue ReadToken(InPort inport, string token)
        {
            if (token == "(")
            {
                var l = new List<LispValue>();
                while (true)
                {
                    var nextToken = inport.NextToken();
                    if (nextToken == ")")
                        return LispValue.Create(l);
                    l.Add(ReadToken(inport, nextToken));
                }
            }
            else if (token == ")")
            {
                throw new LispSyntaxException("Unexpected )");
            }
            else if (token == null)
            {
                throw new LispSyntaxException("Unexpected null in list");
            }
            else if (m_Quotes.TryGetValue(token, out var symb))
            {
                return LispValue.Create(symb, ReadExpression(inport));
            }
            else
            {
                return Atom(token);
            }
        }

        private LispValue Atom(string token)
        {
            if (token == "nil")
                return LispValue.Nil;
            if (token == "#t")
                return LispValue.Create(true);
            if (token == "#f")
                return LispValue.Create(false);
            if (token[0] == '"')
            {
                var escapedString = token.Substring(1, token.Length - 2);
                return LispValue.Create(escapedString);
            }

            if (float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out var tempf))
            {
                return LispValue.Create(tempf);
            }

            if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out var tempi))
            {
                return LispValue.Create(tempi);
            }

            return GetSym(token);
        }

        public LispValue Expand(LispValue expr, bool topLevel = false)
        {
            if (expr.type != LispType.List)
                return expr;

            if (expr.listValue.Count == 0)
                throw new LispSyntaxException("Expression list shouldn't be empty");

            if (EqSym(expr.listValue[0], @quote))
            {
                if (expr.listValue.Count != 2)
                    throw new LispSyntaxException($"Malformed quote {LispValue.Stringigy(expr)}");
                return expr;
            }

            if (EqSym(expr.listValue[0], @if))
            {
                if (expr.listValue.Count == 3)
                    expr.listValue.Add(LispValue.Nil);
                if (expr.listValue.Count != 4)
                    throw new LispSyntaxException($"Malformed if {LispValue.Stringigy(expr)}");
                expr.listValue[1] = Expand(expr.listValue[1]);
                expr.listValue[2] = Expand(expr.listValue[2]);
                expr.listValue[3] = Expand(expr.listValue[3]);
                return expr;
            }

            if (EqSym(expr.listValue[0], @set))
            {
                if (expr.listValue.Count != 3)
                    throw new LispSyntaxException($"Malformed set! {LispValue.Stringigy(expr)}");
                var variable = expr.listValue[1];
                expr.listValue[2] = Expand(expr.listValue[2]);
                return expr;
            }

            if (EqSym(expr.listValue[0], @define) || EqSym(expr.listValue[0], @definemacro))
            {
                if (expr.listValue.Count != 3)
                    throw new LispSyntaxException($"Malformed define {LispValue.Stringigy(expr)}");
                var def = expr.listValue[0];
                var v = expr.listValue[1];
                var body = expr.listValue.Skip(2);
                if (v.type == LispType.List)
                {
                    if (expr.listValue.Count < 2)
                        throw new LispSyntaxException($"Malformed define naming {LispValue.Stringigy(v)}");
                    var fname = v.listValue[0];
                    var args = LispValue.Create(v.listValue.Skip(1));
                    var lambdaDef = LispValue.CreateList(lambda, args, body);
                    var funcDef = LispValue.Create(def, fname, lambdaDef);
                    return Expand(funcDef);
                }
                else
                {
                    if (expr.listValue.Count != 3)
                        throw new LispSyntaxException($"Malformed define naming: {expr}");
                    if (v.type != LispType.Symbol)
                        throw new LispSyntaxException($"Can define only a symbol: {expr}");
                    var expanded = Expand(expr.listValue[2]);
                    if (EqSym(def, definemacro))
                    {
                        // Define a macro
                        if (!topLevel)
                            throw new LispSyntaxException($"define-macro only allowed at top level {expr}");
                        var proc = Eval(expanded);
                        if (proc.type != LispType.Procedure)
                            throw new LispSyntaxException($"define-macro doesn't expand to a procedure {expanded}");
                        m_MacroTable[v.ToString()] = proc;
                        return LispValue.Nil;
                    }
                    else
                    {
                        return LispValue.Create(define, v, expanded);
                    }
                }
            }

            if (EqSym(expr.listValue[0], @begin))
            {
                if (expr.listValue.Count == 1)
                    return LispValue.Nil;
                for (var i = 1; i < expr.listValue.Count; ++i)
                    expr.listValue[i] = Expand(expr.listValue[i], topLevel);
                return expr;
            }

            if (EqSym(expr.listValue[0], @lambda))
            {
                if (expr.listValue.Count < 3)
                    throw new LispSyntaxException($"Malformed lambda {LispValue.Stringigy(expr)}");

                var variables = expr.listValue[1];
                var isValidArgList = variables.type == LispType.Symbol || (variables.type == LispType.List && variables.listValue.All(v => v.type == LispType.Symbol));
                if (!isValidArgList)
                    throw new LispSyntaxException($"Illegal lambda arguments list {LispValue.Stringigy(variables)}");
                var bodyExpr = expr.listValue.Count == 3 ? expr.listValue[2] : LispValue.Create(new[] { begin }.Concat(expr.listValue.Skip(2)).ToList());
                var expandedBodyExpr = Expand(bodyExpr);
                return LispValue.Create(lambda, variables, expandedBodyExpr);
            }

            if (EqSym(expr.listValue[0], @quasiquote))
            {
                if (expr.listValue.Count != 2)
                    throw new LispSyntaxException($"Malformed quasiquote {LispValue.Stringigy(expr)}");
                return ExpandQuasiquote(expr.listValue[1]);
            }

            if (expr.listValue[0].type == LispType.Symbol && m_MacroTable.TryGetValue(expr.listValue[0].ToString(), out var macro))
            {
                var proc = (Procedure)macro.objValue;
                var args = expr.listValue.Skip(1).ToList();
                var evaluatedMacro = proc.Invoke(this, args);
                var expandedMacro = Expand(evaluatedMacro, topLevel);
                return expandedMacro;
            }

            var lv = expr.listValue.Select(v => Expand(v)).ToList();
            return LispValue.Create(lv);
        }

        public static bool IsTruish(LispValue v)
        {
            switch(v.type)
            {
                case LispType.Boolean:
                    return v.intValue > 0;
                case LispType.List:
                    return v.listValue.Count > 0;
                case LispType.Number:
                    return v.floatValue != 0f;
                case LispType.String:
                    return !string.IsNullOrEmpty(v.ToString());
                case LispType.Procedure:
                    return true;
                case LispType.Symbol:
                    return true;
            }
            return false;
        }

        private bool TryResolveSymbol(string name, out LispValue value)
        {
            foreach(var resolver in m_SymbolResolvers.Values)
            {
                if (resolver(this, name, out value))
                {
                    return true;
                }
            }
            value = new LispValue();
            return false;
        }

        private static bool IsPair(LispValue v)
        {
            return v.type == LispType.List && v.listValue.Count > 0;
        }

        private static LispValue Cons(LispValue v1, LispValue v2)
        {
            return LispValue.Create(v1, v2);
        }

        private LispValue ExpandQuasiquote(LispValue expr)
        {
            // Expand `x => 'x; `,x => x; `(,@x y) => (append x y)
            if (!IsPair(expr))
            {
                return LispValue.Create(quote, expr);
            }

            if (EqSym(expr.listValue[0], unquotesplicing))
                throw new LispSyntaxException($"Cannot splice expression: {expr}");

            if (EqSym(expr.listValue[0], unquote))
            {
                if (expr.listValue.Count != 2)
                    throw new LispSyntaxException($"Badly formed unqote {expr}");
                return expr.listValue[1];
            }

            if (IsPair(expr.listValue[0]) && EqSym(expr.listValue[0].listValue[0], unquotesplicing))
            {
                if (expr.listValue[0].listValue.Count != 2)
                    throw new LispSyntaxException($"Badly formed unquote-splicing {expr.listValue[0]}");
                var cdr = LispValue.Create(expr.listValue.Skip(1).ToList());
                var expandedCdr = ExpandQuasiquote(cdr);
                return LispValue.Create(append, expr.listValue[0].listValue[1], expandedCdr);
            }

            {
                var expandedCar = ExpandQuasiquote(expr.listValue[0]);
                var cdr = LispValue.Create(expr.listValue.Skip(1).ToList());
                var expandedCdr = ExpandQuasiquote(cdr);
                return LispValue.Create(cons, expandedCar, expandedCdr);
            }
        }

        private bool EqSym(LispValue v1, LispValue v2)
        {
            return v1.type == LispType.Symbol && v2.type == LispType.Symbol && v1.intValue == v2.intValue;
        }

        private LispValue LetMacro(LispContext ctx, List<LispValue> args)
        {
            if (args.Count < 2)
                throw new LispSyntaxException($"Not enough params for let");
            var bindings = args[0];
            if (bindings.type != LispType.List)
                throw new LispSyntaxException($"let bindings must be a lis {bindings}");
            var body = args.Skip(1);
            if (!bindings.listValue.All(b => b.type == LispType.List && b.listValue.Count == 2))
                throw new LispSyntaxException($"Wrong let bindings format: {bindings}");
            var vars = LispValue.Create(bindings.listValue.Select(b => b.listValue[0]).ToList());
            var expandedValues = bindings.listValue.Select(b => Expand(b.listValue[1]));
            var expandedBodyStatements = body.Select(statement => Expand(statement));
            return LispValue.CreateList(
                LispValue.CreateList(lambda, vars, expandedBodyStatements),
                expandedValues
                );
        }

        private void InitGlobalEnv()
        {
            RegisterProcedure("+", CoreFunctionBindings.Add);
            RegisterProcedure("-", CoreFunctionBindings.Sub);
            RegisterProcedure("*", CoreFunctionBindings.Mult);
            RegisterProcedure("/", CoreFunctionBindings.Div);

            RegisterProcedure(">", CoreFunctionBindings.Greater);
            RegisterProcedure(">=", CoreFunctionBindings.GreaterOrEqual);
            RegisterProcedure("<", CoreFunctionBindings.Lower);
            RegisterProcedure("<=", CoreFunctionBindings.LowerOrEqual);
            RegisterProcedure("=", CoreFunctionBindings.Equal);

            RegisterProcedure("sqrt", CoreFunctionBindings.UnaryMathFunction(Mathf.Sqrt));
            RegisterProcedure("cos", CoreFunctionBindings.UnaryMathFunction(Mathf.Cos));
            RegisterProcedure("sin", CoreFunctionBindings.UnaryMathFunction(Mathf.Sin));
            RegisterProcedure("tan", CoreFunctionBindings.UnaryMathFunction(Mathf.Tan));
            RegisterProcedure("abs", CoreFunctionBindings.UnaryMathFunction(Mathf.Abs));
            RegisterProcedure("sign", CoreFunctionBindings.UnaryMathFunction(Mathf.Sign));

            RegisterProcedure("equal?", CoreFunctionBindings.Equal);
            RegisterProcedure("eq?", CoreFunctionBindings.Eq);

            RegisterProcedure("number?", CoreFunctionBindings.IsNumber);
            RegisterProcedure("list?", CoreFunctionBindings.IsList);
            RegisterProcedure("symbol?", CoreFunctionBindings.IsSymbol);
            RegisterProcedure("string?", CoreFunctionBindings.IsString);
            RegisterProcedure("boolean?", CoreFunctionBindings.IsBoolean);
            RegisterProcedure("null?", CoreFunctionBindings.IsNull);

            RegisterProcedure("car", CoreFunctionBindings.Car);
            RegisterProcedure("first", CoreFunctionBindings.Car);
            RegisterProcedure("cdr", CoreFunctionBindings.Cdr);
            RegisterProcedure("rest", CoreFunctionBindings.Cdr);
            RegisterProcedure("list", CoreFunctionBindings.List);
            RegisterProcedure("cons", CoreFunctionBindings.Cons);
            RegisterProcedure("append", CoreFunctionBindings.Append);
            RegisterProcedure("concat", CoreFunctionBindings.Append);
            RegisterProcedure("length", CoreFunctionBindings.Length);

            RegisterProcedure("eval", CoreFunctionBindings.Eval);
            RegisterProcedure("map", CoreFunctionBindings.Map);
            RegisterProcedure("apply", CoreFunctionBindings.Apply);
            RegisterProcedure("while", CoreFunctionBindings.While, true);

            RegisterProcedure("#", CoreFunctionBindings.GetNativeFunction);

            RegisterMacro("let", LetMacro);

            RegisterSymbolValueGetter(nameof(CoreFunctionBindings.ResolveNativeFunctionSymbol), CoreFunctionBindings.ResolveNativeFunctionSymbol);

            var initCode = @"(begin
(define-macro and (lambda args 
   (if (null? args) #t
       (if (= (length args) 1) (car args)
           `(if ,(car args) (and ,@(cdr args)) #f)))))
)
";
            Eval(initCode);

        }
    }
}


