using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SkEditor.Parser;

public abstract class Node
{
    public int Line { get; set; }
    public string Key { get; set; }
    
    public int Indent { get; set; }
    
    public Element? Element { get; set; }
    public SectionNode? Parent { get; set; }

    protected Node(string key, int line)
    {
        Key = key;
        Line = line;
        Indent = -1; // not yet parsed
        Parent = null; // not yet parsed
    }
    
    public bool IsSimple => this is SimpleNode;
    public bool IsSection => this is SectionNode;
    public bool IsEffect => this is EffectNode;
    
    public abstract void Print(int indent = 0);

    public Node? GetPreviousNode()
    {
        var index = Parent.Children.IndexOf(this);
        if (index == 0)
            return null;
        
        return Parent.Children[index - 1];
    }
    
    public Node GetNextNode()
    {
        var index = Parent.Children.IndexOf(this);
        if (index == Parent.Children.Count - 1)
            return Parent;
        
        return Parent.Children[index + 1];
    }
    
    public bool IsTopLevel => Indent == 0;

    public Node GetStructureNode()
    {
        Node FindParent (Node node)
        {
            if (node.Parent == null)
                return node;
            
            return FindParent(node.Parent);
        }
        
        return FindParent(this);
    }
}

/// <summary>
/// Represent a simple node, which contains a key-value pair
/// </summary>
/// <param name="key">The key of the node</param>
/// <param name="value">The value of the node</param>
public class SimpleNode(string key, int line, string value) : Node(key, line)
{
    public string Value { get; set; } = value;
    public override void Print(int indent = 0)
    {
        Console.WriteLine($"{new string(' ', indent)}{Key}: {Value} [Simple, Line #{Line}]");
    }

    public string[] GetAsArray()
    {
        var regex = new Regex(@"\s*,\s*|\s+(and|or)\s+");
        return regex.Split(Value);
    }

}

/// <summary>
/// Represent a section node, which contains a list of children nodes
/// </summary>
/// <param name="key">The key of the section</param>
public class SectionNode(string key, int line) : Node(key, line), IEnumerable<Node>
{
    public List<Node> Children { get; set; } = new();

    public void AddChild(Node child)
    {
        Children.Add(child);
    }
    
    public override void Print(int indent = 0)
    {
        Console.WriteLine($"{new string(' ', indent)}{Key} [Section, Line #{Line}]");
        foreach (var child in Children)
        {
            child.Print(indent + 2);
        }
    }
    
    public Node? GetChild(string key)
    {
        return Children.Find(node => node.Key == key);
    }
    
    public SimpleNode? GetSimpleChild(string key)
    {
        return GetChild(key) as SimpleNode;
    }
    
    public SectionNode? GetSectionChild(string key)
    {
        return GetChild(key) as SectionNode;
    }

    public IEnumerator<Node> GetEnumerator()
    {
        return Children.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public Node FindLastNode()
    {
        if (Children.Count == 0)
            return this;
        
        var lastNode = Children[^1];
        return lastNode.IsSection ? ((SectionNode) lastNode).FindLastNode() : lastNode;
    }
}

/// <summary>
/// Neither a SimpleNode nor a SectionNode, but a node that represents an effect
/// (without any key-value pair)
/// </summary>
/// <param name="content">The content of the node</param>
public class EffectNode(string content, int line) : Node(content, line)
{
    public string Effect { get; set; } = content;
    
    public override void Print(int indent = 0)
    {
        Console.WriteLine($"{new string(' ', indent)}{Effect} [Effect, Line #{Line}]");
    }
}