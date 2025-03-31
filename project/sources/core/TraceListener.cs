namespace Core;

using System.Diagnostics;
using System.Text;

using Godot;

internal class TraceListener : System.Diagnostics.TraceListener
{
    private readonly string projectResourcePath;
    private readonly StringBuilder stringBuilder = new();
    private readonly bool hideInternalCalls;
    private readonly bool relativePath;

    public TraceListener(bool hideInternalCalls = false, bool relativePath = false)
    {
        this.projectResourcePath = ProjectSettings.GlobalizePath("res://");
        if (OS.HasFeature("windows"))
        {
            this.projectResourcePath = this.projectResourcePath.Replace('/', '\\');
        }

        this.hideInternalCalls = hideInternalCalls;
        this.relativePath = relativePath;
    }

    public override void Write(string? message)
    {
        GD.PrintRaw(message);
    }

    public override void WriteLine(string? message)
    {
        GD.Print(message);
    }

    public override void Fail(string? message, string? detailMessage)
    {
        this.stringBuilder.Clear();
        if (string.IsNullOrEmpty(message))
        {
            this.stringBuilder.AppendLine("Assertion failed.");
        }
        else
        {
            this.stringBuilder.Append("Assertion failed: ").AppendLine(message);
        }

        if (!string.IsNullOrEmpty(detailMessage))
        {
            this.stringBuilder.Append("  Details: ").AppendLine(detailMessage);
        }

        try
        {
            // Ignore the top of the stacktrace (internal c# calls in trace listener) so the assert is at the top of the stacktrace.
            var stackTrace = new StackTrace(3, true);

            bool lastIsInternalCall = false;
            foreach (StackFrame stackFrame in stackTrace.GetFrames())
            {
                string? fileName = stackFrame.GetFileName();

                if (this.hideInternalCalls)
                {
                    // Don't display godot internal calls (C# and native). Keep source generator calls.
                    if (string.IsNullOrEmpty(fileName) || !fileName.StartsWith(this.projectResourcePath))
                    {
                        if (!lastIsInternalCall)
                        {
                            this.stringBuilder.AppendLine("    <internal calls>");
                            lastIsInternalCall = true;
                        }

                        continue;
                    }
                }

                lastIsInternalCall = false;

                System.Reflection.MethodBase? method = stackFrame.GetMethod();
                if (method != null)
                {
                    this.stringBuilder
                        .Append("    at ")
                        .Append(method.DeclaringType!.Namespace)
                        .Append('.')
                        .Append(method.DeclaringType.Name)
                        .Append('.')
                        .Append(method.Name)
                        .Append('(');

                    System.Reflection.ParameterInfo[] parameterInfos = method.GetParameters();
                    for (int i = 0; i < parameterInfos.Length; i++)
                    {
                        this.stringBuilder.Append(parameterInfos[i].ParameterType.Name);
                        this.stringBuilder.Append(' ');
                        this.stringBuilder.Append(parameterInfos[i].Name);
                        if (i < parameterInfos.Length - 1)
                        {
                            this.stringBuilder.Append(", ");
                        }
                    }

                    this.stringBuilder.Append(')');
                }

                if (!string.IsNullOrEmpty(fileName))
                {
                    if (this.relativePath)
                    {
                        fileName = ProjectSettings.LocalizePath(fileName.Replace('\\', '/'));
                    }

                    int lineNumber = stackFrame.GetFileLineNumber();
                    this.stringBuilder.Append(" in ").Append(fileName).Append(':').Append(lineNumber);
                }

                this.stringBuilder.AppendLine();
            }
        }
        catch
        {
            this.stringBuilder.AppendLine("    stacktrace failed to be displayed.");
        }

        GD.PrintErr(this.stringBuilder.ToString());
    }
}
