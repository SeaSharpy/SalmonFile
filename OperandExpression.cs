#nullable enable
using System;
using System.Collections.Generic;
namespace Salmon;

internal sealed class ExpressionParser
{
    private enum OpCode : byte
    {
        Constant,
        Counter,
        UnaryPlus,
        Negate,
        BitwiseNot,
        LogicalNot,
        Multiply,
        Divide,
        Remainder,
        Add,
        Subtract,
        ShiftLeft,
        ShiftRight,
        Less,
        LessOrEqual,
        Greater,
        GreaterOrEqual,
        Equal,
        NotEqual,
        BitwiseAnd,
        BitwiseXor,
        BitwiseOr,
        LogicalAnd,
        LogicalOr,
        Question,
        Conditional,
        Boolean,
        JumpIfZero,
        JumpIfNotZero,
        Jump,
        LoadCounterAndConstant,
        AddCounterAndConstant,
        AddConstant,
        SubtractConstant,
        MultiplyConstant,
        DivideConstant,
        RemainderConstant,
        ShiftLeftConstant,
        ShiftRightConstant,
        LessConstant,
        LessOrEqualConstant,
        GreaterConstant,
        GreaterOrEqualConstant,
        EqualConstant,
        NotEqualConstant,
        BitwiseAndConstant,
        BitwiseXorConstant,
        BitwiseOrConstant
    }
    private readonly struct Instruction
    {
        public readonly OpCode Operation;
        public readonly int Value;
        public readonly int Value2;
        public Instruction(OpCode operation, int value = 0, int value2 = 0)
        {
            Operation = operation;
            Value = value;
            Value2 = value2;
        }
    }
    private readonly string Expression;
    private readonly IReadOnlyDictionary<string, int>? CounterLookup;
    private readonly int[]? CounterValues;
    private readonly IReadOnlyList<int>? LoadedCounterIDs;
    private readonly List<string>? Errors;
    private readonly List<string>? Warnings;
    private readonly List<int> ReferencedCounterIDs = [];
    private Instruction[]? CompiledInstructions;
    private string? FirstError;
    private int LoadedCounterIndex;
    private bool Parsed;
    public ExpressionParser(string expression, IReadOnlyDictionary<string, int> counters, List<string>? errors, List<string>? warnings)
    {
        Expression = expression;
        CounterLookup = counters;
        Errors = errors;
        Warnings = warnings;
    }
    public ExpressionParser(string expression, int[] counters, IReadOnlyList<int> counterIDs)
    {
        Expression = expression;
        CounterValues = counters;
        LoadedCounterIDs = counterIDs;
    }
    public int[] CounterIDs
    {
        get
        {
            if (!Parsed)
                throw new InvalidOperationException("Parse must be called before reading counter IDs.");
            return ReferencedCounterIDs.ToArray();
        }
    }
    public void Parse()
    {
        if (Parsed)
            throw new InvalidOperationException("The expression has already been parsed.");
        Parsed = true;
        var operand = Expression;
        if (int.TryParse(operand, out var operandValue))
        {
            Complete([new Instruction(OpCode.Constant, operandValue)]);
            return;
        }
        if (TryGetSingleCounter(operand, out var singleCounterName))
        {
            Complete([CreateCounterInstruction(singleCounterName)]);
            return;
        }
        var output = new List<Instruction>();
        var operators = new List<(OpCode operation, bool parenthesis)>();
        var position = 0;
        var expectsValue = true;
        int GetPrecedence(OpCode operation) => operation switch
        {
            OpCode.UnaryPlus or OpCode.Negate or OpCode.BitwiseNot or OpCode.LogicalNot => 11,
            OpCode.Multiply or OpCode.Divide or OpCode.Remainder => 10,
            OpCode.Add or OpCode.Subtract => 9,
            OpCode.ShiftLeft or OpCode.ShiftRight => 8,
            OpCode.Less or OpCode.LessOrEqual or OpCode.Greater or OpCode.GreaterOrEqual => 7,
            OpCode.Equal or OpCode.NotEqual => 6,
            OpCode.BitwiseAnd => 5,
            OpCode.BitwiseXor => 4,
            OpCode.BitwiseOr => 3,
            OpCode.LogicalAnd => 2,
            OpCode.LogicalOr => 1,
            OpCode.Conditional => 0,
            _ => 0
        };
        bool IsUnary(OpCode operation) => operation is OpCode.UnaryPlus or OpCode.Negate or OpCode.BitwiseNot or OpCode.LogicalNot;
        void PushOperator(OpCode operation)
        {
            var precedence = GetPrecedence(operation);
            while (operators.Count != 0 && !operators[^1].parenthesis)
            {
                var top = operators[^1].operation;
                var topPrecedence = GetPrecedence(top);
                if (topPrecedence < precedence || IsUnary(operation) && topPrecedence == precedence)
                    break;
                output.Add(new Instruction(top));
                operators.RemoveAt(operators.Count - 1);
            }
            operators.Add((operation, false));
        }
        void SkipWhitespace()
        {
            while (position < operand.Length && char.IsWhiteSpace(operand[position]))
                position++;
        }
        bool Match(string token)
        {
            if (!operand.AsSpan(position).StartsWith(token, StringComparison.Ordinal))
                return false;
            position += token.Length;
            return true;
        }
        while (true)
        {
            SkipWhitespace();
            if (position == operand.Length)
                break;
            if (expectsValue)
            {
                if (Match("("))
                {
                    operators.Add((default, true));
                    continue;
                }
                if (Match("+"))
                {
                    PushOperator(OpCode.UnaryPlus);
                    continue;
                }
                if (Match("-"))
                {
                    PushOperator(OpCode.Negate);
                    continue;
                }
                if (Match("~"))
                {
                    PushOperator(OpCode.BitwiseNot);
                    continue;
                }
                if (Match("!"))
                {
                    PushOperator(OpCode.LogicalNot);
                    continue;
                }
                if (Match("$"))
                {
                    var start = position;
                    while (position < operand.Length && (char.IsLetterOrDigit(operand[position]) || operand[position] == '_'))
                        position++;
                    if (position == start)
                    {
                        AddError($"Expected a counter name at position {position} in operand expression '{operand}'.");
                        return;
                    }
                    var counterName = operand.Substring(start, position - start);
                    output.Add(CreateCounterInstruction(counterName));
                    expectsValue = false;
                    continue;
                }
                var numberStart = position;
                while (position < operand.Length && char.IsDigit(operand[position]))
                    position++;
                if (position == numberStart || !int.TryParse(operand.AsSpan(numberStart, position - numberStart), out var number))
                {
                    AddError($"Expected a number, counter, unary operator, or '(' at position {position} in operand expression '{operand}'.");
                    return;
                }
                output.Add(new Instruction(OpCode.Constant, number));
                expectsValue = false;
                continue;
            }
            if (Match(")"))
            {
                while (operators.Count != 0 && !operators[^1].parenthesis)
                {
                    if (operators[^1].operation == OpCode.Question)
                    {
                        AddError($"Expected ':' before ')' at position {position - 1} in operand expression '{operand}'.");
                        return;
                    }
                    output.Add(new Instruction(operators[^1].operation));
                    operators.RemoveAt(operators.Count - 1);
                }
                if (operators.Count == 0)
                {
                    AddError($"Unexpected ')' at position {position - 1} in operand expression '{operand}'.");
                    return;
                }
                operators.RemoveAt(operators.Count - 1);
                continue;
            }
            if (Match("?"))
            {
                while (operators.Count != 0 && !operators[^1].parenthesis && operators[^1].operation != OpCode.Question && GetPrecedence(operators[^1].operation) > 0)
                {
                    output.Add(new Instruction(operators[^1].operation));
                    operators.RemoveAt(operators.Count - 1);
                }
                operators.Add((OpCode.Question, false));
                expectsValue = true;
                continue;
            }
            if (Match(":"))
            {
                while (operators.Count != 0 && !operators[^1].parenthesis && operators[^1].operation != OpCode.Question)
                {
                    output.Add(new Instruction(operators[^1].operation));
                    operators.RemoveAt(operators.Count - 1);
                }
                if (operators.Count == 0 || operators[^1].parenthesis)
                {
                    AddError($"Unexpected ':' at position {position - 1} in operand expression '{operand}'.");
                    return;
                }
                operators[^1] = (OpCode.Conditional, false);
                expectsValue = true;
                continue;
            }
            OpCode operation;
            if (Match("<<"))
                operation = OpCode.ShiftLeft;
            else if (Match(">>"))
                operation = OpCode.ShiftRight;
            else if (Match("<="))
                operation = OpCode.LessOrEqual;
            else if (Match(">="))
                operation = OpCode.GreaterOrEqual;
            else if (Match("=="))
                operation = OpCode.Equal;
            else if (Match("!="))
                operation = OpCode.NotEqual;
            else if (Match("&&"))
                operation = OpCode.LogicalAnd;
            else if (Match("||"))
                operation = OpCode.LogicalOr;
            else if (Match("*"))
                operation = OpCode.Multiply;
            else if (Match("/"))
                operation = OpCode.Divide;
            else if (Match("%"))
                operation = OpCode.Remainder;
            else if (Match("+"))
                operation = OpCode.Add;
            else if (Match("-"))
                operation = OpCode.Subtract;
            else if (Match("<"))
                operation = OpCode.Less;
            else if (Match(">"))
                operation = OpCode.Greater;
            else if (Match("&"))
                operation = OpCode.BitwiseAnd;
            else if (Match("^"))
                operation = OpCode.BitwiseXor;
            else if (Match("|"))
                operation = OpCode.BitwiseOr;
            else
            {
                AddError($"Expected an operator at position {position} in operand expression '{operand}'.");
                return;
            }
            PushOperator(operation);
            expectsValue = true;
        }
        if (expectsValue)
        {
            AddError($"Operand expression '{operand}' ends before a value.");
            return;
        }
        while (operators.Count != 0)
        {
            if (operators[^1].parenthesis)
            {
                AddError($"Operand expression '{operand}' has an unmatched '('.");
                return;
            }
            if (operators[^1].operation == OpCode.Question)
            {
                AddError($"Operand expression '{operand}' has a '?' without a matching ':'.");
                return;
            }
            output.Add(new Instruction(operators[^1].operation));
            operators.RemoveAt(operators.Count - 1);
        }
        var expressions = new List<Action<List<Instruction>>>();
        foreach (var instruction in output)
        {
            if (instruction.Operation == OpCode.Constant)
            {
                var value = instruction.Value;
                expressions.Add(instructions => instructions.Add(new Instruction(OpCode.Constant, value)));
                continue;
            }
            if (instruction.Operation == OpCode.Counter)
            {
                var counter = instruction.Value;
                expressions.Add(instructions => instructions.Add(new Instruction(OpCode.Counter, counter)));
                continue;
            }
            if (IsUnary(instruction.Operation))
            {
                var value = expressions[^1];
                var operation = instruction.Operation;
                expressions[^1] = instructions =>
                {
                    value(instructions);
                    instructions.Add(new Instruction(operation));
                };
                continue;
            }
            if (instruction.Operation == OpCode.Conditional)
            {
                var whenFalse = expressions[^1];
                expressions.RemoveAt(expressions.Count - 1);
                var whenTrue = expressions[^1];
                expressions.RemoveAt(expressions.Count - 1);
                var condition = expressions[^1];
                expressions[^1] = instructions =>
                {
                    condition(instructions);
                    var falseJump = instructions.Count;
                    instructions.Add(default);
                    whenTrue(instructions);
                    var endJump = instructions.Count;
                    instructions.Add(default);
                    instructions[falseJump] = new Instruction(OpCode.JumpIfZero, instructions.Count);
                    whenFalse(instructions);
                    instructions[endJump] = new Instruction(OpCode.Jump, instructions.Count);
                };
                continue;
            }
            var right = expressions[^1];
            expressions.RemoveAt(expressions.Count - 1);
            var left = expressions[^1];
            var binaryOperation = instruction.Operation;
            if (binaryOperation == OpCode.LogicalAnd)
            {
                expressions[^1] = instructions =>
                {
                    left(instructions);
                    var falseJump = instructions.Count;
                    instructions.Add(default);
                    right(instructions);
                    instructions.Add(new Instruction(OpCode.Boolean));
                    var endJump = instructions.Count;
                    instructions.Add(default);
                    instructions[falseJump] = new Instruction(OpCode.JumpIfZero, instructions.Count);
                    instructions.Add(new Instruction(OpCode.Constant, 0));
                    instructions[endJump] = new Instruction(OpCode.Jump, instructions.Count);
                };
                continue;
            }
            if (binaryOperation == OpCode.LogicalOr)
            {
                expressions[^1] = instructions =>
                {
                    left(instructions);
                    var trueJump = instructions.Count;
                    instructions.Add(default);
                    right(instructions);
                    instructions.Add(new Instruction(OpCode.Boolean));
                    var endJump = instructions.Count;
                    instructions.Add(default);
                    instructions[trueJump] = new Instruction(OpCode.JumpIfNotZero, instructions.Count);
                    instructions.Add(new Instruction(OpCode.Constant, 1));
                    instructions[endJump] = new Instruction(OpCode.Jump, instructions.Count);
                };
                continue;
            }
            expressions[^1] = instructions =>
            {
                var leftStart = instructions.Count;
                left(instructions);
                var leftCount = instructions.Count - leftStart;
                var rightStart = instructions.Count;
                right(instructions);
                var rightCount = instructions.Count - rightStart;
                if (leftCount == 1 && rightCount == 1 && instructions[leftStart].Operation == OpCode.Counter && instructions[rightStart].Operation == OpCode.Constant && binaryOperation == OpCode.BitwiseAnd)
                {
                    var counter = instructions[leftStart].Value;
                    var constant = instructions[rightStart].Value;
                    instructions.RemoveRange(leftStart, 2);
                    instructions.Add(new Instruction(OpCode.LoadCounterAndConstant, counter, constant));
                    return;
                }
                if (rightCount == 1 && instructions[rightStart].Operation == OpCode.LoadCounterAndConstant && binaryOperation == OpCode.Add)
                {
                    var counter = instructions[rightStart].Value;
                    var constant = instructions[rightStart].Value2;
                    instructions.RemoveAt(rightStart);
                    instructions.Add(new Instruction(OpCode.AddCounterAndConstant, counter, constant));
                    return;
                }
                if (rightCount == 1 && instructions[rightStart].Operation == OpCode.Constant)
                {
                    var constantOperation = binaryOperation switch
                    {
                        OpCode.Add => OpCode.AddConstant,
                        OpCode.Subtract => OpCode.SubtractConstant,
                        OpCode.Multiply => OpCode.MultiplyConstant,
                        OpCode.Divide => OpCode.DivideConstant,
                        OpCode.Remainder => OpCode.RemainderConstant,
                        OpCode.ShiftLeft => OpCode.ShiftLeftConstant,
                        OpCode.ShiftRight => OpCode.ShiftRightConstant,
                        OpCode.Less => OpCode.LessConstant,
                        OpCode.LessOrEqual => OpCode.LessOrEqualConstant,
                        OpCode.Greater => OpCode.GreaterConstant,
                        OpCode.GreaterOrEqual => OpCode.GreaterOrEqualConstant,
                        OpCode.Equal => OpCode.EqualConstant,
                        OpCode.NotEqual => OpCode.NotEqualConstant,
                        OpCode.BitwiseAnd => OpCode.BitwiseAndConstant,
                        OpCode.BitwiseXor => OpCode.BitwiseXorConstant,
                        OpCode.BitwiseOr => OpCode.BitwiseOrConstant,
                        _ => OpCode.Constant
                    };
                    if (constantOperation != OpCode.Constant)
                    {
                        var constant = instructions[rightStart].Value;
                        instructions.RemoveAt(rightStart);
                        instructions.Add(new Instruction(constantOperation, constant));
                        return;
                    }
                }
                instructions.Add(new Instruction(binaryOperation));
            };
        }
        var compiled = new List<Instruction>();
        expressions[0](compiled);
        Complete(compiled.ToArray());
    }
    public Func<int> CreateGetter()
    {
        if (!Parsed)
            throw new InvalidOperationException("Parse must be called before creating a getter.");
        if (CompiledInstructions == null)
            throw new FormatException(FirstError ?? $"Operand expression '{Expression}' is invalid.");
        if (CounterValues == null)
            throw new InvalidOperationException("A getter can only be created by a runtime expression parser.");
        var compiledInstructions = CompiledInstructions;
        var counterValues = CounterValues;
        var stack = new int[compiledInstructions.Length];
        return () =>
        {
            var stackCount = 0;
            for (var instructionIndex = 0; instructionIndex < compiledInstructions.Length; instructionIndex++)
            {
                var instruction = compiledInstructions[instructionIndex];
                switch (instruction.Operation)
                {
                    case OpCode.Constant:
                        stack[stackCount++] = instruction.Value;
                        break;
                    case OpCode.Counter:
                        stack[stackCount++] = counterValues[instruction.Value];
                        break;
                    case OpCode.LoadCounterAndConstant:
                        stack[stackCount++] = counterValues[instruction.Value] & instruction.Value2;
                        break;
                    case OpCode.AddCounterAndConstant:
                        stack[stackCount - 1] = unchecked(stack[stackCount - 1] + (counterValues[instruction.Value] & instruction.Value2));
                        break;
                    case OpCode.AddConstant:
                        stack[stackCount - 1] = unchecked(stack[stackCount - 1] + instruction.Value);
                        break;
                    case OpCode.SubtractConstant:
                        stack[stackCount - 1] = unchecked(stack[stackCount - 1] - instruction.Value);
                        break;
                    case OpCode.MultiplyConstant:
                        stack[stackCount - 1] = unchecked(stack[stackCount - 1] * instruction.Value);
                        break;
                    case OpCode.DivideConstant:
                        stack[stackCount - 1] /= instruction.Value;
                        break;
                    case OpCode.RemainderConstant:
                        stack[stackCount - 1] %= instruction.Value;
                        break;
                    case OpCode.ShiftLeftConstant:
                        stack[stackCount - 1] <<= instruction.Value;
                        break;
                    case OpCode.ShiftRightConstant:
                        stack[stackCount - 1] >>= instruction.Value;
                        break;
                    case OpCode.LessConstant:
                        stack[stackCount - 1] = stack[stackCount - 1] < instruction.Value ? 1 : 0;
                        break;
                    case OpCode.LessOrEqualConstant:
                        stack[stackCount - 1] = stack[stackCount - 1] <= instruction.Value ? 1 : 0;
                        break;
                    case OpCode.GreaterConstant:
                        stack[stackCount - 1] = stack[stackCount - 1] > instruction.Value ? 1 : 0;
                        break;
                    case OpCode.GreaterOrEqualConstant:
                        stack[stackCount - 1] = stack[stackCount - 1] >= instruction.Value ? 1 : 0;
                        break;
                    case OpCode.EqualConstant:
                        stack[stackCount - 1] = stack[stackCount - 1] == instruction.Value ? 1 : 0;
                        break;
                    case OpCode.NotEqualConstant:
                        stack[stackCount - 1] = stack[stackCount - 1] != instruction.Value ? 1 : 0;
                        break;
                    case OpCode.BitwiseAndConstant:
                        stack[stackCount - 1] &= instruction.Value;
                        break;
                    case OpCode.BitwiseXorConstant:
                        stack[stackCount - 1] ^= instruction.Value;
                        break;
                    case OpCode.BitwiseOrConstant:
                        stack[stackCount - 1] |= instruction.Value;
                        break;
                    case OpCode.UnaryPlus:
                        break;
                    case OpCode.Negate:
                        stack[stackCount - 1] = unchecked(-stack[stackCount - 1]);
                        break;
                    case OpCode.BitwiseNot:
                        stack[stackCount - 1] = ~stack[stackCount - 1];
                        break;
                    case OpCode.LogicalNot:
                        stack[stackCount - 1] = stack[stackCount - 1] == 0 ? 1 : 0;
                        break;
                    case OpCode.Boolean:
                        stack[stackCount - 1] = stack[stackCount - 1] != 0 ? 1 : 0;
                        break;
                    case OpCode.JumpIfZero:
                        if (stack[--stackCount] == 0)
                            instructionIndex = instruction.Value - 1;
                        break;
                    case OpCode.JumpIfNotZero:
                        if (stack[--stackCount] != 0)
                            instructionIndex = instruction.Value - 1;
                        break;
                    case OpCode.Jump:
                        instructionIndex = instruction.Value - 1;
                        break;
                    default:
                        var right = stack[--stackCount];
                        var left = stack[stackCount - 1];
                        stack[stackCount - 1] = instruction.Operation switch
                        {
                            OpCode.Multiply => unchecked(left * right),
                            OpCode.Divide => left / right,
                            OpCode.Remainder => left % right,
                            OpCode.Add => unchecked(left + right),
                            OpCode.Subtract => unchecked(left - right),
                            OpCode.ShiftLeft => left << right,
                            OpCode.ShiftRight => left >> right,
                            OpCode.Less => left < right ? 1 : 0,
                            OpCode.LessOrEqual => left <= right ? 1 : 0,
                            OpCode.Greater => left > right ? 1 : 0,
                            OpCode.GreaterOrEqual => left >= right ? 1 : 0,
                            OpCode.Equal => left == right ? 1 : 0,
                            OpCode.NotEqual => left != right ? 1 : 0,
                            OpCode.BitwiseAnd => left & right,
                            OpCode.BitwiseXor => left ^ right,
                            OpCode.BitwiseOr => left | right,
                            _ => 0
                        };
                        break;
                }
            }
            return stack[0];
        };
    }
    private void AddError(string error)
    {
        FirstError ??= error;
        Errors?.Add(error);
    }
    private void AddWarning(string warning) => Warnings?.Add(warning);
    private Instruction CreateCounterInstruction(string counterName)
    {
        int counterID;
        if (CounterLookup != null)
        {
            if (!CounterLookup.TryGetValue(counterName, out counterID))
            {
                counterID = -1;
                AddWarning($"Operand expression '{Expression}' references nonexistent counter '{counterName}'.");
            }
            ReferencedCounterIDs.Add(counterID);
        }
        else
        {
            if (LoadedCounterIDs == null || LoadedCounterIndex >= LoadedCounterIDs.Count)
            {
                AddError($"Operand expression '{Expression}' does not have enough compiled counter IDs.");
                return new Instruction(OpCode.Constant, 0);
            }
            counterID = LoadedCounterIDs[LoadedCounterIndex++];
        }
        if (counterID < 0)
            return new Instruction(OpCode.Constant, 0);
        if (CounterValues != null && counterID >= CounterValues.Length)
        {
            AddError($"Operand expression '{Expression}' references invalid counter ID {counterID}.");
            return new Instruction(OpCode.Constant, 0);
        }
        return new Instruction(OpCode.Counter, counterID);
    }
    private void Complete(Instruction[] instructions)
    {
        if (LoadedCounterIDs != null && LoadedCounterIndex != LoadedCounterIDs.Count)
        {
            AddError($"Operand expression '{Expression}' has {LoadedCounterIDs.Count - LoadedCounterIndex} unused compiled counter IDs.");
            return;
        }
        if (FirstError == null)
            CompiledInstructions = instructions;
    }
    private static bool TryGetSingleCounter(string expression, out string counterName)
    {
        counterName = string.Empty;
        if (expression.Length <= 1 || expression[0] != '$')
            return false;
        for (var i = 1; i < expression.Length; i++)
            if (!char.IsLetterOrDigit(expression[i]) && expression[i] != '_')
                return false;
        counterName = expression.Substring(1);
        return true;
    }
}
