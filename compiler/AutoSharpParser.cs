using System;
using System.Collections.Generic;

internal class Node
{
    public string Label;
    public List<Node> Children = new List<Node>();
    public string DataType;

    public Node(string label, string dataType = null)
    {
        Label = label;
        DataType = dataType;
    }

    public Node(string label, string dataType = null, params Node[] children)
    {
        Label = label;
        DataType = dataType;
        Children.AddRange(children);
    }
}

internal class AutoSharpParser
{
    private List<(string Type, string Value)> _tokens;
    private int position = 0;
    private Dictionary<string, string> symbolTable = new Dictionary<string, string>();

    public AutoSharpParser(List<(string Type, string Value)> tokens)
    {
        _tokens = tokens;
    }

    private (string Type, string Value) Current => position < _tokens.Count ? _tokens[position] : ("EOF", "");
    private (string Type, string Value) Peek(int offset) => (position + offset) < _tokens.Count ? _tokens[position + offset] : ("EOF", "");

    private void Eat(string type)
    {
        if (Current.Type == type) position++;
        else throw new Exception($"Syntax Error: Expected {type} but found {Current.Type} ({Current.Value})");
    }

    private bool CheckTypeCompatibility(string varType, string exprType)
    {
        var typeMap = new Dictionary<string, string>
            {
                {"Gear","NUMBER"},
                {"Speed","NUMBER"},
                {"Distance","NUMBER"},
                {"Model","STRING"},
                {"Flag","BOOL"}
            };
        return typeMap.ContainsKey(varType) && typeMap[varType] == exprType;
    }

    // ===== Program → StatementList =====
    public Node ParseProgram()
    {
        var root = new Node("Program");
        var stmtList = new Node("StatementList");

        while (Current.Type != "EOF")
            stmtList.Children.Add(ParseStatement());

        root.Children.Add(stmtList);
        return root;
    }

    // ===== Statement → Decl | Assignment | Check | Block =====
    public Node ParseStatement()
    {
        if (Current.Type == "DATATYPE") return ParseDecl();
        if (Current.Type == "IDENTIFIER" && Peek(1).Type == "ASSIGN") return ParseAssignment();
        if (Current.Type == "CHECK") return ParseCheck();
        if (Current.Type == "LOOP") return ParseLoop();
        if (Current.Type == "SHIFT") return ParseShift(); // <- دعم Shift المستقل
        if (Current.Type == "LBRACE") return ParseBlock();
        if (Current.Type == "SEMICOLON") { Eat("SEMICOLON"); return new Node("EmptyStmt"); }

        throw new Exception($"Unexpected token in statement: {Current.Type} ({Current.Value})");
    }

    private Node ParseDecl()
    {
        string dtype = Current.Value;
        Eat("DATATYPE");
        string id = Current.Value;
        Eat("IDENTIFIER");

        Node exprNode = null;
        if (Current.Type == "ASSIGN")
        {
            Eat("ASSIGN");
            exprNode = ParseExpression();
            if (!CheckTypeCompatibility(dtype, exprNode.DataType))
                throw new Exception($"Type Error: cannot assign {exprNode.DataType} to {dtype}");
        }

        Eat("SEMICOLON");
        symbolTable[id] = dtype;

        var declNode = new Node("Decl", null,
            new Node($"DATATYPE ({dtype})"),
            new Node($"IDENTIFIER ({id})", dtype));

        if (exprNode != null)
            declNode.Children.Add(exprNode);

        return declNode;
    }

    private Node ParseAssignment()
    {
        string id = Current.Value;
        Eat("IDENTIFIER");
        Eat("ASSIGN");
        var expr = ParseExpression();

        if (!symbolTable.ContainsKey(id))
            throw new Exception($"Semantic Error: undefined variable '{id}'");

        string varType = symbolTable[id];
        if (!CheckTypeCompatibility(varType, expr.DataType))
            throw new Exception($"Type Error: cannot assign {expr.DataType} to {varType}");

        Eat("SEMICOLON");
        return new Node("Assignment", null,
            new Node($"IDENTIFIER ({id})", varType),
            expr);
    }

    private Node ParseCheck()
    {
        Eat("CHECK");
        Eat("LPAREN");
        var cond = ParseExpression();
        Eat("RPAREN");

        var thenBlock = ParseBlock(); // قراءة كل الـ block

        Node shiftPart = null;
        // لو الكلمة التالية Shift، نربطها بالـ Check
        if (Current.Type == "SHIFT")
        {
            Eat("SHIFT");
            shiftPart = ParseBlock();
        }

        var node = new Node("CheckStmt");
        node.Children.Add(cond);
        node.Children.Add(thenBlock);
        if (shiftPart != null) node.Children.Add(shiftPart);

        return node;
    }



    private Node ParseShift()
    {
        Eat("SHIFT");
        var shiftBlock = ParseBlock();
        var node = new Node("ShiftStmt");
        node.Children.Add(shiftBlock);
        return node;
    }





    private Node ParseLoop()
    {
        Eat("LOOP");          // نقرأ كلمة Loop
        Eat("LPAREN");         // (
        var cond = ParseExpression(); // شرط اللوب
        Eat("RPAREN");         // )

        var loopBlock = ParseBlock(); // البلوك اللي جوه اللوب

        var node = new Node("LoopStmt");
        node.Children.Add(cond);
        node.Children.Add(loopBlock);

        return node;
    }





    private Node ParseBlock()
    {
        Eat("LBRACE");
        var block = new Node("Block");
        var stmts = new Node("StmtList");

        while (Current.Type != "RBRACE" && Current.Type != "EOF")
            stmts.Children.Add(ParseStatement());

        Eat("RBRACE");
        block.Children.Add(stmts);
        return block;
    }

    private Node ParseExpression()
    {
        var node = ParseTerm();
        while (Current.Type == "OP" && (Current.Value == "+" || Current.Value == "-" || Current.Value == ">" || Current.Value == "<"))
        {
            string op = Current.Value;
            Eat("OP");
            var right = ParseTerm();
            if (node.DataType != right.DataType)
                throw new Exception($"Type Error: cannot apply '{op}' between {node.DataType} and {right.DataType}");
            node = new Node("Expression", node.DataType, node, new Node($"OP ({op})"), right);
        }
        return node;
    }

    private Node ParseTerm()
    {
        var node = ParseFactor();
        while (Current.Type == "OP" && (Current.Value == "*" || Current.Value == "/"))
        {
            string op = Current.Value;
            Eat("OP");
            var right = ParseFactor();
            if (op == "/" && right.Label.StartsWith("NUMBER (0"))
                throw new Exception("Division by zero error");
            if (node.DataType != right.DataType)
                throw new Exception($"Type Error: cannot apply '{op}' between {node.DataType} and {right.DataType}");
            node = new Node("Term", node.DataType, node, new Node($"OP ({op})"), right);
        }
        return node;
    }

    private Node ParseFactor()
    {
        if (Current.Type == "NUMBER")
        {
            var node = new Node($"NUMBER ({Current.Value})", "NUMBER");
            Eat("NUMBER");
            return node;
        }
        else if (Current.Type == "STRING")
        {
            var node = new Node($"STRING ({Current.Value})", "STRING");
            Eat("STRING");
            return node;
        }
        else if (Current.Type == "IDENTIFIER")
        {
            string id = Current.Value;
            Eat("IDENTIFIER");
            if (!symbolTable.ContainsKey(id))
                throw new Exception($"Semantic Error: undefined variable '{id}'");
            return new Node($"IDENTIFIER ({id})", MapType(symbolTable[id]));
        }
        else if (Current.Type == "BOOL")
        {
            var node = new Node($"BOOL ({Current.Value})", "BOOL");
            Eat("BOOL");
            return node;
        }
        else if (Current.Type == "LPAREN")
        {
            Eat("LPAREN");
            var expr = ParseExpression();
            Eat("RPAREN");
            return expr;
        }

        throw new Exception($"Unexpected token: {Current.Type} ({Current.Value})");
    }

    private string MapType(string varType)
    {
        var typeMap = new Dictionary<string, string>
            {
                {"Gear","NUMBER"},
                {"Speed","NUMBER"},
                {"Distance","NUMBER"},
                {"Model","STRING"},
                {"Flag","BOOL"}
            };
        return typeMap.ContainsKey(varType) ? typeMap[varType] : varType;
    }

    public static void PrintTree(Node node, string indent = "", bool last = true)
    {
        Console.Write(indent);
        Console.Write(last ? "└─" : "├─");
        Console.WriteLine(node.Label + (node.DataType != null ? $" : {node.DataType}" : ""));
        indent += last ? "  " : "│ ";

        foreach (var child in node.Children)
            PrintTree(child, indent, child == node.Children[^1]);
    }
}
