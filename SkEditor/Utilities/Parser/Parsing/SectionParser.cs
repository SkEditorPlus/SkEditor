using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SkEditor.API;

namespace SkEditor.Parser;

public static class SectionParser
{ 
    
    public static List<Node> Parse(string[] lines, bool debug = false)
    {
        List<Node> nodes = new();
        Stack<(SectionNode node, int indent)> sectionStack = new();
        Regex simpleNodeRegex = new Regex(@"^(\s*)(.+?)\s*:\s*([^:]+)$");
        Regex sectionNodeRegex = new Regex(@"^(\s*)(.+)\s*:$");

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
            {
                if (debug) SkEditorAPI.Logs.Debug($"--- Skipping empty line {i + 1}");
                continue;
            }

            if (debug) SkEditorAPI.Logs.Debug($"--- Processing line {i + 1}: {line}");

            var simpleMatch = simpleNodeRegex.Match(line.TrimEnd());
            if (simpleMatch.Success)
            {
                var indent = simpleMatch.Groups[1].Value.Length;
                var key = simpleMatch.Groups[2].Value;
                var value = simpleMatch.Groups[3].Value;

                var simpleNode = new SimpleNode(key, i + 1, value);
                AddNodeToCorrectParent(nodes, sectionStack, simpleNode, indent, debug, i + 1);
                continue;
            }

            var sectionMatch = sectionNodeRegex.Match(line.TrimEnd());
            if (sectionMatch.Success)
            {
                var indent = sectionMatch.Groups[1].Value.Length;
                var key = sectionMatch.Groups[2].Value;
                var sectionNode = new SectionNode(key, i + 1);
                AddNodeToCorrectParent(nodes, sectionStack, sectionNode, indent, debug, i + 1);
                sectionStack.Push((sectionNode, indent));
                continue;
            }

            // Treat as EffectNode
            var effectNode = new EffectNode(trimmedLine, i + 1);
            AddNodeToCorrectParent(nodes, sectionStack, effectNode, line.TakeWhile(char.IsWhiteSpace).Count(), debug, i + 1);
        }
        
        // Now we indent all nodes correctly
        foreach (var node in nodes)
        {
            node.Indent = 0;
            if (node.IsSection)
            {
                IndentChildren((SectionNode) node, 0);
            }
        }
        
        void DebugNode(Node node, int indent)
        {
            SkEditorAPI.Logs.Debug($"{new string(' ', indent * 2)}{node.GetType().Name}: {node.Key}");
            if (node is SectionNode sectionNode)
            {
                foreach (var child in sectionNode)
                {
                    DebugNode(child, indent + 1);
                }
            }
        }
        
        if (!debug)
        {
            SkEditorAPI.Logs.Debug("--- Parsed nodes:");
            foreach (var node in nodes)
            {
                DebugNode(node, 0);
            }
        }
        
        // And we set up parents of each node
        foreach (var node in nodes)
            SetupParent(node);

        return nodes;
    }

    private static void AddNodeToCorrectParent(List<Node> nodes, Stack<(SectionNode node, int indent)> sectionStack, Node newNode, int indent, bool debug, int lineNumber)
    {
        while (sectionStack.Count > 0 && sectionStack.Peek().indent >= indent)
        {
            sectionStack.Pop();
        }

        if (sectionStack.Count > 0)
        {
            sectionStack.Peek().node.AddChild(newNode);
            if (debug) SkEditorAPI.Logs.Debug($"Added {newNode.GetType().Name}: {newNode.Key} with indent {indent} (Line {lineNumber}) to parent {sectionStack.Peek().node.Key}");
        }
        else
        {
            nodes.Add(newNode);
            if (debug) SkEditorAPI.Logs.Debug($"Added {newNode.GetType().Name}: {newNode.Key} with indent {indent} (Line {lineNumber}) to root");
        }
    }
    
    private static void IndentChildren(SectionNode node, int indent)
    {
        foreach (var child in node.Children)
        {
            child.Indent = indent + 1;
            if (child.IsSection)
            {
                IndentChildren((SectionNode) child, indent + 1);
            }
        }
    }

    private static void SetupParent(Node node)
    {
        if (node is SectionNode sectionNode)
        {
            foreach (var child in sectionNode)
            {
                child.Parent = sectionNode;
                SetupParent(child);
            }
        }
    }
    
}