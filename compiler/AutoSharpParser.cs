internal class Node
{
    public string Label;
    public List<Node> Children = new List<Node>();
    public string DataType; // NUMBER, STRING, BOOL, IDENTIFIER, etc.

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

    public AutoSharpParser(List<(string Type, string Value)> tokens)
    {
        _tokens = tokens;
    }

    private (string Type, string Value) Current
    {
        get
        {
            if (position < _tokens.Count)
                return _tokens[position];
            else
                return ("EOF", "");
        }
    }

    private void Eat(string type)
    {
        if (Current.Type == type)
            position++;
        else
            throw new Exception($"Syntax Error: Expected {type} but found {Current.Type} ({Current.Value})");
    }

    public Node ParseStatement()
    {
        if (Current.Type == "DATATYPE")//speed x ;
        {
            string typeName = Current.Value;


            var typeNode = new Node($"DATATYPE ({typeName})");
            Eat("DATATYPE");

            var idNode = new Node($"IDENTIFIER ({Current.Value})");
            Eat("IDENTIFIER");

            var assignNode = new Node($"ASSIGN ({Current.Value})");
            Eat("ASSIGN");

            var exprNode = ParseExpression();

            // Type checking: expression type must match variable type
            if (!CheckTypeCompatibility(typeName, exprNode.DataType))
                throw new Exception($"Type Error: cannot assign {exprNode.DataType} to {typeName}");

            Eat("SEMICOLON");

            var root = new Node("Assignment");
            root.Children.Add(typeNode);
            root.Children.Add(idNode);
            root.Children.Add(assignNode);
            root.Children.Add(exprNode);

            return root;
        }
        else
        {
            throw new Exception($"Unexpected token: {Current.Value}");
        }
    }

    private Node ParseExpression()
    {
        var node = ParseTerm();

        while (Current.Type == "OP" && (Current.Value == "+" || Current.Value == "-"))
        {
            string op = Current.Value;
            Eat("OP");
            var right = ParseTerm();//واقفين هنا 

            // Type checking
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

            if (Current.Value == "0"&&op=="/") {
                Console.WriteLine("Division by zero error");
                return null;

            }
            var right = ParseFactor();
            

            // Type checking
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
            var node = new Node($"IDENTIFIER ({Current.Value})", "IDENTIFIER");
            Eat("IDENTIFIER");
            return node;
        }
        else if (Current.Type == "KEYWORD" && (Current.Value == "true" || Current.Value == "false"))
        {
            var node = new Node($"BOOL ({Current.Value})", "BOOL");
            Eat("KEYWORD");
            return node;
        }
        else if (Current.Type == "LPAREN")
        {
            Eat("LPAREN");
            var expr = ParseExpression();
            Eat("RPAREN");
            return expr;
        }
        else
        {
            throw new Exception($"Unexpected token: {Current.Type} ({Current.Value})");
        }
    }

    private bool CheckTypeCompatibility(string varType, string exprType)
    {
        // map AutoSharp types to basic types
        var typeMap = new Dictionary<string, string>
            {
                {"Gear", "NUMBER"},
                {"Speed", "NUMBER"},
                {"Distance", "NUMBER"},
                {"Model", "STRING"},
                {"Flag", "BOOL"}
            };

        return typeMap.ContainsKey(varType) && typeMap[varType] == exprType;
    }

    public static void PrintTree(Node node, string indent = "", bool last = true)
    {
        Console.Write(indent);
        Console.Write(last ? "└─" : "├─");
        Console.WriteLine(node.Label);

        if (last)
        {
            indent = indent + "  ";  
        }
        else
        {
            indent = indent + "│ ";  
        }

        for (int i = 0; i < node.Children.Count; i++)
        {
            PrintTree(node.Children[i], indent, i == node.Children.Count - 1);
        }
    }
}