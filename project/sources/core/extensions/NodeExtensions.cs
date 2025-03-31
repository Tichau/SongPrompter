using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Godot;

namespace Core.Extensions;

public static class NodeExtensions
{
    public static void QueueFreeChildren(this Godot.Node node)
    {
        foreach (Godot.Node? child in node.GetChildren())
        {
            node.RemoveChild(child);
            child.QueueFree();
        }
    }

    public static void QueueFree<T>(this List<T> nodeList)
        where T : Node
    {
        foreach (T node in nodeList)
        {
            node.QueueFree();
        }

        nodeList.Clear();
    }

    public static void MakeSceneRoot(this Godot.Node node)
    {
        static void SetChildrenOwner(Node node, Node previousOwner, Node newOwner)
        {
            foreach (Godot.Node child in node.GetChildren())
            {
                if (child.Owner == previousOwner)
                {
                    child.Owner = newOwner;
                }

                SetChildrenOwner(child, previousOwner, newOwner);
            }
        }

        SetChildrenOwner(node, node.Owner, node);
        node.Owner = null;
    }

    public static void MapChildren<TGame, TUI>(this Control root, Func<TUI> controlCreator, IEnumerable<TGame> gameElements, Action<TGame, TUI> updateElement, Predicate<TGame>? displayPredicate = null)
        where TUI : Control
    {
        Debug.Assert(root != null);

        int index = 0;
        if (gameElements != null)
        {
            foreach (TGame? element in gameElements)
            {
                if (displayPredicate != null && !displayPredicate.Invoke(element))
                {
                    continue;
                }

                TUI? line = default;
                if (index < root.GetChildCount())
                {
                    line = (TUI)root.GetChild(index);
                }
                else
                {
                    line = controlCreator.Invoke();
                    root.AddChild(line);
                }

                updateElement.Invoke(element, line);
                index++;
            }
        }

        for (int remainingIndex = root.GetChildCount() - 1; remainingIndex >= index; remainingIndex--)
        {
            root.GetChild(remainingIndex).Free();
        }
    }

    public static IEnumerable<T> GetChildren<T>(this Godot.Node node, bool recursive = false)
        where T : Godot.Node
    {
        foreach (Godot.Node? child in node.GetChildren())
        {
            if (child is T castChild)
            {
                yield return castChild;
            }

            if (recursive)
            {
                foreach (T? subChild in child.GetChildren<T>())
                {
                    yield return subChild;
                }
            }
        }
    }

    public static Theme? GetTheme(this Control node)
    {
        // Get control theme.
        Theme? theme = node.Theme;
        while (theme == null)
        {
            if (node.GetParent() is Control parent)
            {
                node = parent;
            }
            else
            {
                break;
            }

            theme = node.Theme;
        }

        return theme;
    }

    /// <summary>
    /// Safely set script on existing instance.
    /// </summary>
    /// <typeparam name="T">Type of script to set.</typeparam>
    /// <param name="godotObject">Instance on which we want to set the script.</param>
    /// <returns>Returns the instance with script attached.</returns>
    /// Source: https://github.com/godotengine/godot/issues/31994
    public static T SafelySetScript<T>(this GodotObject godotObject)
        where T : GodotObject, new()
    {
        ulong godotObjectId = godotObject.GetInstanceId();

        // This call replaces the old C# instance with a new one. Old C# instance is disposed.
        godotObject.SetScript(new T().GetScript());

        // Get the new C# instance
        var nodeWithScript = GodotObject.InstanceFromId(godotObjectId) as T;
        Debug.Assert(nodeWithScript != null && GodotObject.IsInstanceValid(nodeWithScript));

        return nodeWithScript;
    }

    public static StringBuilder ToPrettyPrint(this Node node, bool recursively = false, string indent = "", StringBuilder? stringBuilder = null)
    {
        stringBuilder ??= new();

        if (stringBuilder.Length > 10000)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"STOP! pretty print to long");
            return stringBuilder;
        }

        stringBuilder.Append(node.Name);
        stringBuilder.AppendFormat(" (owner: {0}", node.Owner != null ? node.Owner.Name : "null");
        if (!string.IsNullOrEmpty(node.SceneFilePath))
        {
            stringBuilder.AppendFormat(", scene_path: {0}", node.SceneFilePath);
        }
        stringBuilder.Append(')');

        if (recursively)
        {
            indent = $"   {indent}";
            for (int index = 0; index < node.GetChildCount(); index++)
            {
                stringBuilder.AppendLine();
                stringBuilder.Append(indent);
                stringBuilder.Append("|--");
                node.GetChild(index).ToPrettyPrint(recursively, indent, stringBuilder);
            }
        }

        return stringBuilder;
    }
}
