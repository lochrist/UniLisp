using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UniLisp;
using UnityEngine;
using UnityEngine.TestTools;

public class UniLispTests
{
    // A Test behaves as an ordinary method
    [Test]
    public void UniLispTestsSimplePasses()
    {
        Regex m_Tokenizer = new Regex(@"\s*(,@|[('`,)]|""(?:[\\].|[^\\""])*""|;.*|[^\s('""`,;)]*)(.*)");

        var g = m_Tokenizer.Match("hello we are well");
        var g1 = g.Groups[0];
        var g2 = g.Groups[1];

        // Use the Assert class to test conditions

        var mi = typeof(UnityEngine.Debug).GetMethods();


    }

    public static class GetMethodInfoUtil
    {
        // No cast necessary
        public static MethodInfo GetMethodInfo(Action action) => action.Method;
        public static MethodInfo GetMethodInfo<T>(Action<T> action) => action.Method;
        public static MethodInfo GetMethodInfo<T, U>(Action<T, U> action) => action.Method;
        public static MethodInfo GetMethodInfo<TResult>(Func<TResult> fun) => fun.Method;
        public static MethodInfo GetMethodInfo<T, TResult>(Func<T, TResult> fun) => fun.Method;
        public static MethodInfo GetMethodInfo<T, U, TResult>(Func<T, U, TResult> fun) => fun.Method;

        // Cast necessary
        public static MethodInfo GetMethodInfo(Delegate del) => del.Method;
    }

    public static float Add(float f1, float f2)
    {
        return f1 + f2;
    }

    [Test]
    public void DelegateDynamicCalling()
    {
        // var d = Delegate.CreateDelegate(typeof(UniLispTests), UniLispTests.Add);
        var mi = GetMethodInfoUtil.GetMethodInfo<float, float, float>(Add);
        var d = Delegate.CreateDelegate(typeof(Func<float, float, float>), mi);
        var result = d.DynamicInvoke(34, 45);

        var r2 = d.DynamicInvoke(new object[] { 34, 45 } );

        // Use the Assert class to test conditions
    }

    public class StringifyTestCase
    {
        public LispValue value;
        public string expr;
        public string expected;
        public bool shouldThrow;

        public StringifyTestCase(string expr, string expected = null)
        {
            this.expr = expr;
            this.expected = expected ?? expr;
        }

        public StringifyTestCase(LispValue value, string expected)
        {
            this.value = value;
            this.expected = expected;
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(expr))
            {
                if (expr != expected)
                    return $"{expr} => {expected}";
                return expr;
            }
                
            return $"{expected}";
        }
    }

    static readonly StringifyTestCase[] s_StringifyTestCases = new StringifyTestCase[]
    {
        new StringifyTestCase(LispValue.Nil, "nil"),
        new StringifyTestCase(LispValue.Create(4), "4"),
        new StringifyTestCase(LispValue.Create(4.5f), "4.5"),
        new StringifyTestCase(LispValue.Create("hello"), "\"hello\""),
        new StringifyTestCase(LispValue.Create(true), "#t"),
        new StringifyTestCase(LispValue.Create(false), "#f"),
        new StringifyTestCase(LispValue.CreateSymbol("ping"), "ping"),
        new StringifyTestCase(LispValue.Create(LispValue.Create(3), LispValue.Create("hello"), LispValue.Create(true)), "(3 \"hello\" #t)"),
        new StringifyTestCase(LispValue.Create(LispValue.Create(3), LispValue.Create(LispValue.Create("hello"), LispValue.Create("world"), LispValue.Create(69)), LispValue.Create(true)), "(3 (\"hello\" \"world\" 69) #t)"),
    };

    [Test]
    public void Stringify([ValueSource(nameof(s_StringifyTestCases))] StringifyTestCase t)
    {
        var ctx = new LispContext();
        var strValue = LispValue.Stringigy(t.value);
        Assert.AreEqual(t.expected, strValue);
    }

    public class NextTokenTestCase
    {
        public string[] expectedTokens;
        public string expr;

        public NextTokenTestCase(string expr, params string[] expectedTokens)
        {
            this.expr = expr;
            this.expectedTokens = expectedTokens;
        }

        public override string ToString()
        {
            return $"{expr}";
        }
    }

    static readonly NextTokenTestCase[] s_NextTokenTestCases = new NextTokenTestCase[]
    {
        new NextTokenTestCase("4", "4"),
        new NextTokenTestCase("(4 5)", "(", "4", "5", ")"),
        new NextTokenTestCase("\"hello\"", "\"hello\""),
        new NextTokenTestCase("'hello", "'", "hello"),
        new NextTokenTestCase("`hello", "`", "hello"),
        new NextTokenTestCase("`,hello", "`", ",", "hello"),
        new NextTokenTestCase("4 ; hello", "4", null),
    };

    [Test]
    public void NextToken([ValueSource(nameof(s_NextTokenTestCases))] NextTokenTestCase t)
    {
        var inport = new InPort(new StringReader(t.expr));
        for (var i = 0; i < t.expectedTokens.Length; ++i)
        {
            var expected = t.expectedTokens[i];
            var token = inport.NextToken();
            Assert.AreEqual(expected, token);
        }
    }

    static readonly StringifyTestCase[] s_ReadExpressionTestCases = new StringifyTestCase[]
    {
        new StringifyTestCase("nil", "nil"),
        new StringifyTestCase("4", "4"),
        new StringifyTestCase("4.5", "4.5"),
        new StringifyTestCase("\"hello\"", "\"hello\""),
        new StringifyTestCase("#t", "#t"),
        new StringifyTestCase("#f", "#f"),
        new StringifyTestCase("ping", "ping"),
        new StringifyTestCase("(3 \"hello\" #t)", "(3 \"hello\" #t)"),
        new StringifyTestCase("(3 (\"hello\" \"world\" 69) #t)", "(3 (\"hello\" \"world\" 69) #t)"),
        new StringifyTestCase("'ping", "(quote ping)"),
        new StringifyTestCase("`ping", "(quasiquote ping)"),
        new StringifyTestCase("(if #t 'ping `pong)", "(if #t (quote ping) (quasiquote pong))"),
        new StringifyTestCase("`(testing1 ,@L testing2)", "(quasiquote (testing1 (unquote-splicing L) testing2))"),
        new StringifyTestCase("`(testing1 ,L testing2)", "(quasiquote (testing1 (unquote L) testing2))"),
        new StringifyTestCase(")", ")") { shouldThrow = true },
    };

    [Test]
    public void ReadExpression([ValueSource(nameof(s_ReadExpressionTestCases))] StringifyTestCase t)
    {
        var ctx = new LispContext();
        var inport = new InPort(new StringReader(t.expr));

        if (t.shouldThrow)
        {
            Assert.Throws<LispSyntaxException>(() => ctx.ReadExpression(inport));
        }
        else
        {
            var expr = ctx.ReadExpression(inport);
            var exprStr = LispValue.Stringigy(expr);
            Assert.AreEqual(t.expected, exprStr);
        }
    }

    static readonly StringifyTestCase[] s_ParseTestCases = new StringifyTestCase[]
    {
        new StringifyTestCase("nil", "nil"),
        new StringifyTestCase("4", "4"),
        new StringifyTestCase("4.5", "4.5"),
        new StringifyTestCase("\"hello\"", "\"hello\""),
        new StringifyTestCase("#t", "#t"),
        new StringifyTestCase("#f", "#f"),
        new StringifyTestCase("ping", "ping"),
        new StringifyTestCase("(3 \"hello\" #t)", "(3 \"hello\" #t)"),
        new StringifyTestCase("(3 (\"hello\" \"world\" 69) #t)", "(3 (\"hello\" \"world\" 69) #t)"),
        new StringifyTestCase("'ping", "(quote ping)"),
        new StringifyTestCase("`ping", "(quote ping)"),
        new StringifyTestCase("(if #t 'ping `pong)", "(if #t (quote ping) (quote pong))"),
        new StringifyTestCase("`(testing1 ,@L testing2)", "(cons (quote testing1) (append L (cons (quote testing2) (quote ()))))"),
        new StringifyTestCase("`(testing1 ,L testing2)", "(cons (quote testing1) (cons L (cons (quote testing2) (quote ()))))"),

        new StringifyTestCase("()") { shouldThrow = true },
        new StringifyTestCase("(set! x)") { shouldThrow = true },
        new StringifyTestCase("(define 3 4)") { shouldThrow = true },
        new StringifyTestCase("(quote 3 4)") { shouldThrow = true },
        new StringifyTestCase("(if 1 2 3 4)") { shouldThrow = true },
        new StringifyTestCase("(lambda 3 3)") { shouldThrow = true },
        new StringifyTestCase("(lambda (x)") { shouldThrow = true },
        new StringifyTestCase("(if (= 1 2) (define-macro a 'a) (define-macro a 'b))") { shouldThrow = true },
        new StringifyTestCase("(define (twice x) (* x x)") { shouldThrow = true },

        new StringifyTestCase("(lambda (x) `(1 2 4))", "(lambda (x) (cons (quote 1) (cons (quote 2) (cons (quote 4) (quote ())))))"),

        new StringifyTestCase("(if #t 'ping 'pong)", "(if #t (quote ping) (quote pong))"),
        new StringifyTestCase("(if #t 'ping)", "(if #t (quote ping) nil)"),

        new StringifyTestCase("(begin)", "nil"),
        new StringifyTestCase("(begin 1 2 (if #t 'ping))", "(begin 1 2 (if #t (quote ping) nil))"),

        new StringifyTestCase(@"(begin 
1 2 (if #t 'ping))", "(begin 1 2 (if #t (quote ping) nil))"),

        new StringifyTestCase("(define (twice x) (* x x))", "(define twice (lambda (x) (* x x)))"),

        new StringifyTestCase("(func 'ping 'pong)", "(func (quote ping) (quote pong))"),

        new StringifyTestCase("(and (> 2 1) (= 4 4))", "(if (> 2 1) (= 4 4) #f)"),
    };

    [Test]
    public void ParseAndExpand([ValueSource(nameof(s_ParseTestCases))] StringifyTestCase t)
    {
        var ctx = new LispContext();
        var inport = new InPort(new StringReader(t.expr));

        if (t.shouldThrow)
        {
            Assert.Throws<LispSyntaxException>(() => ctx.Parse(inport));
        }
        else
        {
            var expr = ctx.Parse(inport);
            var exprStr = LispValue.Stringigy(expr);
            Assert.AreEqual(t.expected, exprStr);
        }
    }

    public class ExpandMacroTestCase
    {
        public LispValue value;
        public string macroDef;
        public string macroCall;
        public string expected;
        public bool shouldThrow;

        public ExpandMacroTestCase(string macroDef, string macroCall, string expected)
        {
            this.macroDef = macroDef;
            this.macroCall = macroCall;
            this.expected = expected;
        }

        public override string ToString()
        {
            return $"{macroCall} -> {expected}";
        }
    }

    static readonly ExpandMacroTestCase[] s_MacroExpandTestCases = new ExpandMacroTestCase[]
    {
        new ExpandMacroTestCase("(define-macro m (lambda args '(list args)))", "(m)", "(list args)"),
        new ExpandMacroTestCase("(define-macro m (lambda args '(1 2 3 4)))", "(m)", "(1 2 3 4)"),
        new ExpandMacroTestCase("(define-macro m (lambda args (if args \"this is true\" \"this is false\")))", "(m 'something)", "\"this is true\""),
        new ExpandMacroTestCase("(define-macro m (lambda args (if args \"this is true\" \"this is false\")))", "(m)", "\"this is false\""),
    };

    [Test]
    public void ExpandMacro([ValueSource(nameof(s_MacroExpandTestCases))] ExpandMacroTestCase t)
    {
        var ctx = new LispContext();
        ctx.Eval(t.macroDef);

        if (t.shouldThrow)
        {
            Assert.Throws<LispSyntaxException>(() => ctx.Parse(t.macroCall));
        }
        else
        {
            var expr = ctx.Parse(t.macroCall);
            var exprStr = LispValue.Stringigy(expr);
            Assert.AreEqual(t.expected, exprStr);
        }
    }

    static readonly StringifyTestCase[] s_EvalTestCases = new StringifyTestCase[]
    {
        new StringifyTestCase("nil", "nil"),
        new StringifyTestCase("4", "4"),
        new StringifyTestCase("4.5", "4.5"),
        new StringifyTestCase("\"hello\"", "\"hello\""),
        new StringifyTestCase("#t", "#t"),
        new StringifyTestCase("#f", "#f"),

        new StringifyTestCase("(if #t 'ping 'pong)", "ping"),
        new StringifyTestCase("(if #f 'ping 'pong)", "pong"),
        new StringifyTestCase("(if #f 'ping)", "nil"),

        new StringifyTestCase("(begin)", "nil"),
        new StringifyTestCase("(begin 1 2 (if #t 'ping))", "ping"),

        new StringifyTestCase("(define (twice x) (* 2 x))", "nil"),

        new StringifyTestCase("(set! x 45)", "nil"),
        new StringifyTestCase("(begin (set! x 45) x)", "45"),

        new StringifyTestCase("(begin (define (twice x) (* 2 x)) (twice 34))", "68"),

        new StringifyTestCase(@"(begin (define fib (lambda (n) (if (<= 2 n) (+ (fib (- n 1)) (fib (- n 2))) n ))) (fib 10))", "55"),

        new StringifyTestCase("(begin (begin (define v 34) (set! v (+ v 8)))  v)", "42"),
    };

    static void EvalTest(StringifyTestCase t)
    {
        var ctx = new LispContext();
        var inport = new InPort(new StringReader(t.expr));

        if (t.shouldThrow)
        {
            Assert.Throws<LispRuntimeException>(() => ctx.Eval(inport));
        }
        else
        {
            var result = ctx.Eval(inport);
            var resultStr = result.ToString();
            Assert.AreEqual(t.expected, resultStr);
        }
    }

    [Test]
    public void Eval([ValueSource(nameof(s_EvalTestCases))] StringifyTestCase t)
    {
        EvalTest(t);
    }

    static readonly StringifyTestCase[] s_EvalCoreLibTestCases = new StringifyTestCase[]
    {
        new StringifyTestCase("(symbol? 1)", "#f"),
        new StringifyTestCase("(symbol? 'sym)", "#t"),

        new StringifyTestCase("(string? 1)", "#f"),
        new StringifyTestCase("(string? \"string\")", "#t"),

        new StringifyTestCase("(list? 1)", "#f"),
        new StringifyTestCase("(list? '(1 2 3))", "#t"),

        new StringifyTestCase("(boolean? 1)", "#f"),
        new StringifyTestCase("(boolean? #f)", "#t"),

        new StringifyTestCase("(number? 1)", "#t"),
        new StringifyTestCase("(number? #f)", "#f"),

        new StringifyTestCase("(+)") { shouldThrow = true },
        new StringifyTestCase("(+ 1)") { shouldThrow = true },
        new StringifyTestCase("(+ #t 1)") { shouldThrow = true },

        new StringifyTestCase("(+ 1 2 3)", "6"),
        new StringifyTestCase("(- 12 1 2 3)", "6"),
        new StringifyTestCase("(* 1 2 3)", "6"),
        new StringifyTestCase("(/ 12 2 3)", "2"),

        new StringifyTestCase("(>)") { shouldThrow = true },
        new StringifyTestCase("(> 1)") { shouldThrow = true },
        new StringifyTestCase("(> 1 2)", "#f"),
        new StringifyTestCase("(> 2 1)", "#t"),
        new StringifyTestCase("(> 2 2)", "#f"),

        new StringifyTestCase("(<)") { shouldThrow = true },
        new StringifyTestCase("(< 1)") { shouldThrow = true },
        new StringifyTestCase("(< 1 2)", "#t"),
        new StringifyTestCase("(< 2 1)", "#f"),
        new StringifyTestCase("(< 2 2)", "#f"),

        new StringifyTestCase("(<=)") { shouldThrow = true },
        new StringifyTestCase("(<= 1)") { shouldThrow = true },
        new StringifyTestCase("(<= 1 2)", "#t"),
        new StringifyTestCase("(<= 2 1)", "#f"),
        new StringifyTestCase("(<= 2 2)", "#t"),

        new StringifyTestCase("(>=)") { shouldThrow = true },
        new StringifyTestCase("(>= 1)") { shouldThrow = true },
        new StringifyTestCase("(>= 1 2)", "#f"),
        new StringifyTestCase("(>= 2 1)", "#t"),
        new StringifyTestCase("(>= 2 2)", "#t"),

        new StringifyTestCase("(=)") { shouldThrow = true },
        new StringifyTestCase("(equal? 1)") { shouldThrow = true },
        new StringifyTestCase("(equal? 1 #f)") { shouldThrow = true },
        new StringifyTestCase("(= 1 2)", "#f"),
        new StringifyTestCase("(= 2 2)", "#t"),

        new StringifyTestCase("(eq?)") { shouldThrow = true },
        new StringifyTestCase("(eq? 1)") { shouldThrow = true },
        new StringifyTestCase("(eq? 1 #f)", "#f"),
        new StringifyTestCase("(eq? 1 1)", "#t"),
        new StringifyTestCase("(eq? 1 2)", "#f"),
        new StringifyTestCase("(eq? \"ping\" \"ping\")", "#f"),
        new StringifyTestCase("(eq? 'ping 'ping)", "#t"),
        new StringifyTestCase("(eq? 'ping 'pong)", "#f"),

        new StringifyTestCase("(list)", "()"),
        new StringifyTestCase("(list 1)", "(1)"),
        new StringifyTestCase("(list 1 #t)", "(1 #t)"),
        new StringifyTestCase("(list 1 #t \"pin\")", "(1 #t \"pin\")"),
        new StringifyTestCase("(list 1 '(#t 2) \"pin\")", "(1 (#t 2) \"pin\")"),

        new StringifyTestCase("(cons)") { shouldThrow = true },
        new StringifyTestCase("(cons 1)") { shouldThrow = true },
        new StringifyTestCase("(cons 1 2)") { shouldThrow = true },
        new StringifyTestCase("(cons 1 '(2))", "(1 2)"),

        new StringifyTestCase("(append)") { shouldThrow = true },
        new StringifyTestCase("(append 1)") { shouldThrow = true },
        new StringifyTestCase("(append '(1) 2)") { shouldThrow = true },
        new StringifyTestCase("(append '(1) '(2))", "(1 2)"),
        new StringifyTestCase("(append '(1) '(2) '(3 4 5))", "(1 2 3 4 5)"),

        new StringifyTestCase("(length)") { shouldThrow = true },
        new StringifyTestCase("(length 1)") { shouldThrow = true },
        new StringifyTestCase("(length '(1))", "1"),
        new StringifyTestCase("(length '(1 4 5))", "3"),

        new StringifyTestCase("(eval '(1 4 5))") { shouldThrow = true },
        new StringifyTestCase("(eval '(begin '(1 2 3)))", "(1 2 3)"),
        new StringifyTestCase("(eval (+ 1 4 5))", "10"),
        new StringifyTestCase("(eval \"ping\")", "\"ping\""),
        new StringifyTestCase("(eval '(+ 1 4 5))", "10"),

        new StringifyTestCase("(map)") { shouldThrow = true },
        new StringifyTestCase("(map 1)") { shouldThrow = true },
        new StringifyTestCase("(map + 1)") { shouldThrow = true },
        new StringifyTestCase("(begin (define (twice x) (* x x)) (map twice '(1 2 3 4)))", "(1 4 9 16)"),

        new StringifyTestCase("(apply)") { shouldThrow = true },
        new StringifyTestCase("(apply 1)") { shouldThrow = true },
        new StringifyTestCase("(apply + 1)") { shouldThrow = true },
        new StringifyTestCase("(apply + '(1 2 3))", "6"),

        new StringifyTestCase("(while)") { shouldThrow = true },
        new StringifyTestCase("(while 2)") { shouldThrow = true },
        new StringifyTestCase(@"(begin 
(set! i 0) 
(while (< i 3) 
    (set! i (+ i 1))
    i
))", "3"),

    };

    [Test]
    public void EvalCoreLib([ValueSource(nameof(s_EvalCoreLibTestCases))] StringifyTestCase t)
    {
        EvalTest(t);
    }

    static readonly StringifyTestCase[] s_NativeCallTestCases = new StringifyTestCase[]
    {
        new StringifyTestCase("(# \"UnityEngine.Debug.Log\" \"Hello!\")"),
        new StringifyTestCase("(set! logMe (get# \"UnityEngine.Debug.Log\")))"),
        new StringifyTestCase("(begin (set! logMe (get# \"UnityEngine.Debug.Log\")) (logMe \"Hello!\"))"),
    };

    [Test]
    public void EvalNativeCall([ValueSource(nameof(s_NativeCallTestCases))] StringifyTestCase t)
    {
        var ctx = new LispContext();
        var inport = new InPort(new StringReader(t.expr));

        if (t.shouldThrow)
        {
            Assert.Throws<LispRuntimeException>(() => ctx.Eval(inport));
        }
        else
        {
            var result = ctx.Eval(inport);
        }
    }
}
