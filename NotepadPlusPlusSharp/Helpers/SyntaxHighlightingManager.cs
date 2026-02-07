using System.IO;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Highlighting;

namespace NotepadPlusPlusSharp.Helpers;

public static class SyntaxHighlightingManager
{
    private static readonly Dictionary<string, string> ExtensionToLanguageMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".c", "C++" }, { ".h", "C++" }, { ".cpp", "C++" }, { ".cxx", "C++" },
        { ".cc", "C++" }, { ".hpp", "C++" }, { ".hxx", "C++" },
        { ".cs", "C#" },
        { ".vb", "VB" },
        { ".java", "Java" },
        { ".js", "JavaScript" }, { ".jsx", "JavaScript" }, { ".mjs", "JavaScript" },
        { ".ts", "JavaScript" }, { ".tsx", "JavaScript" },
        { ".py", "Python" }, { ".pyw", "Python" }, { ".pyx", "Python" },
        { ".html", "HTML" }, { ".htm", "HTML" }, { ".shtml", "HTML" },
        { ".vue", "HTML" }, { ".svelte", "HTML" },
        { ".xml", "XML" }, { ".xaml", "XML" }, { ".xsl", "XML" },
        { ".xslt", "XML" }, { ".xsd", "XML" }, { ".config", "XML" },
        { ".csproj", "XML" }, { ".vbproj", "XML" }, { ".fsproj", "XML" },
        { ".sln", "XML" }, { ".props", "XML" }, { ".targets", "XML" },
        { ".nuspec", "XML" }, { ".resx", "XML" }, { ".svg", "XML" },
        { ".css", "CSS" }, { ".scss", "CSS" }, { ".less", "CSS" }, { ".sass", "CSS" },
        { ".php", "PHP" }, { ".php3", "PHP" }, { ".php4", "PHP" }, { ".php5", "PHP" },
        { ".phtml", "PHP" },
        { ".sql", "TSQL" },
        { ".json", "JSON" }, { ".jsonc", "JSON" },
        { ".md", "MarkDown" }, { ".markdown", "MarkDown" }, { ".mdown", "MarkDown" },
        { ".ps1", "PowerShell" }, { ".psm1", "PowerShell" }, { ".psd1", "PowerShell" },
        { ".bat", "BAT" }, { ".cmd", "BAT" },
        { ".fs", "F#" }, { ".fsi", "F#" }, { ".fsx", "F#" },
        { ".yml", "XML" }, { ".yaml", "XML" },
        { ".ini", "INI" }, { ".cfg", "INI" }, { ".conf", "INI" },
        { ".patch", "Patch" }, { ".diff", "Patch" },
        { ".tex", "TeX" }, { ".latex", "TeX" }, { ".sty", "TeX" }, { ".cls", "TeX" },
        { ".asp", "ASP/XHTML" }, { ".aspx", "ASP/XHTML" },
        { ".boo", "Boo" },
        { ".atg", "Coco" },
    };

    private static readonly Dictionary<string, string> LanguageDisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        { "C++", "C / C++" },
        { "C#", "C#" },
        { "VB", "VB.NET" },
        { "Java", "Java" },
        { "JavaScript", "JavaScript / TypeScript" },
        { "Python", "Python" },
        { "HTML", "HTML" },
        { "XML", "XML / XAML" },
        { "CSS", "CSS / SCSS" },
        { "PHP", "PHP" },
        { "TSQL", "SQL" },
        { "JSON", "JSON" },
        { "MarkDown", "Markdown" },
        { "PowerShell", "PowerShell" },
        { "BAT", "Batch" },
        { "F#", "F#" },
        { "INI", "INI" },
        { "Patch", "Diff / Patch" },
        { "TeX", "TeX / LaTeX" },
        { "ASP/XHTML", "ASP.NET" },
        { "Boo", "Boo" },
        { "Coco", "Coco/R" },
    };

    public static string? DetectLanguageFromContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        var sample = content.Length > 4000 ? content[..4000] : content;
        var lines = sample.Split('\n', StringSplitOptions.None);
        var firstLine = lines.Length > 0 ? lines[0].Trim() : "";

        var shebangResult = DetectFromShebang(firstLine);
        if (shebangResult != null) return shebangResult;

        var headerResult = DetectFromHeader(firstLine, sample);
        if (headerResult != null) return headerResult;

        var scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        ScoreCSharp(sample, lines, scores);
        ScoreJava(sample, lines, scores);
        ScorePython(sample, lines, scores);
        ScoreJavaScript(sample, lines, scores);
        ScoreCpp(sample, lines, scores);
        ScoreHtml(sample, lines, scores);
        ScoreXml(sample, lines, scores);
        ScoreCss(sample, lines, scores);
        ScorePhp(sample, lines, scores);
        ScoreSql(sample, lines, scores);
        ScoreJson(sample, scores);
        ScoreMarkdown(sample, lines, scores);
        ScorePowerShell(sample, lines, scores);
        ScoreBatch(sample, lines, scores);
        ScoreRust(sample, lines, scores);
        ScoreGo(sample, lines, scores);
        ScoreFSharp(sample, lines, scores);
        ScoreTex(sample, lines, scores);
        ScoreIni(sample, lines, scores);
        ScoreVb(sample, lines, scores);
        ScorePatch(sample, lines, scores);

        if (scores.Count == 0)
            return null;

        var best = scores.OrderByDescending(kv => kv.Value).First();

        if (best.Value < 3)
            return null;

        return best.Key;
    }

    private static string? DetectFromShebang(string firstLine)
    {
        if (!firstLine.StartsWith("#!"))
            return null;

        var shebang = firstLine.ToLowerInvariant();

        if (shebang.Contains("python")) return "Python";
        if (shebang.Contains("node") || shebang.Contains("deno") || shebang.Contains("bun")) return "JavaScript";
        if (shebang.Contains("bash") || shebang.Contains("/sh")) return "BAT";
        if (shebang.Contains("ruby")) return null;
        if (shebang.Contains("perl")) return null;
        if (shebang.Contains("php")) return "PHP";
        if (shebang.Contains("pwsh") || shebang.Contains("powershell")) return "PowerShell";

        return null;
    }

    private static string? DetectFromHeader(string firstLine, string sample)
    {
        if (firstLine.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
            return "XML";

        if (firstLine.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
            firstLine.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
            return "HTML";

        if (firstLine.StartsWith("<?php", StringComparison.OrdinalIgnoreCase))
            return "PHP";

        if (firstLine.StartsWith("\\documentclass") || firstLine.StartsWith("\\usepackage"))
            return "TeX";

        if (firstLine.StartsWith("diff --git") || firstLine.StartsWith("---") && sample.Contains("\n+++"))
            return "Patch";

        return null;
    }

    private static void ScoreCSharp(string sample, string[] lines, Dictionary<string, int> scores)
    {
        int s = 0;
        if (Regex.IsMatch(sample, @"\busing\s+System\b")) s += 5;
        if (Regex.IsMatch(sample, @"\busing\s+[\w.]+;\s*$", RegexOptions.Multiline)) s += 3;
        if (Regex.IsMatch(sample, @"\bnamespace\s+\w+")) s += 4;
        if (Regex.IsMatch(sample, @"\b(public|private|protected|internal)\s+(class|struct|interface|enum|record)\s+\w+")) s += 4;
        if (Regex.IsMatch(sample, @"\b(async\s+)?Task\b")) s += 2;
        if (Regex.IsMatch(sample, @"\bvar\s+\w+\s*=")) s += 1;
        if (Regex.IsMatch(sample, @"\bstring\b") && Regex.IsMatch(sample, @"\bint\b")) s += 2;
        if (Regex.IsMatch(sample, @"\bnew\s+\w+\s*\(")) s += 1;
        if (Regex.IsMatch(sample, @"\b(get|set)\s*[{;]")) s += 2;
        if (Regex.IsMatch(sample, @"\bLinq\b|\bIEnumerable\b|\bList<")) s += 3;
        if (Regex.IsMatch(sample, @"=>\s*\{?")) s += 1;
        if (sample.Contains("[HttpGet]") || sample.Contains("[HttpPost]") || sample.Contains("[ApiController]")) s += 4;
        if (sample.Contains("Console.Write")) s += 3;
        if (s > 0) scores["C#"] = s;
    }

    private static void ScoreJava(string sample, string[] lines, Dictionary<string, int> scores)
    {
        int s = 0;
        if (Regex.IsMatch(sample, @"\bimport\s+java\.")) s += 5;
        if (Regex.IsMatch(sample, @"\bimport\s+javax\.")) s += 5;
        if (Regex.IsMatch(sample, @"\bimport\s+org\.")) s += 3;
        if (Regex.IsMatch(sample, @"\bpublic\s+class\s+\w+")) s += 2;
        if (Regex.IsMatch(sample, @"\bpublic\s+static\s+void\s+main\s*\(")) s += 5;
        if (Regex.IsMatch(sample, @"\bSystem\.out\.print")) s += 4;
        if (Regex.IsMatch(sample, @"\bpackage\s+[\w.]+;")) s += 4;
        if (Regex.IsMatch(sample, @"\b@Override\b")) s += 3;
        if (Regex.IsMatch(sample, @"\bextends\s+\w+")) s += 2;
        if (Regex.IsMatch(sample, @"\bimplements\s+\w+")) s += 2;
        if (s > 0) scores["Java"] = s;
    }

    private static void ScorePython(string sample, string[] lines, Dictionary<string, int> scores)
    {
        int s = 0;
        if (Regex.IsMatch(sample, @"^\s*def\s+\w+\s*\(", RegexOptions.Multiline)) s += 3;
        if (Regex.IsMatch(sample, @"^\s*class\s+\w+.*:", RegexOptions.Multiline)) s += 3;
        if (Regex.IsMatch(sample, @"^\s*import\s+\w+", RegexOptions.Multiline)) s += 2;
        if (Regex.IsMatch(sample, @"^\s*from\s+\w+\s+import\s+", RegexOptions.Multiline)) s += 4;
        if (Regex.IsMatch(sample, @"\bprint\s*\(")) s += 2;
        if (Regex.IsMatch(sample, @"\bif\s+.*:\s*$", RegexOptions.Multiline)) s += 2;
        if (Regex.IsMatch(sample, @"\bfor\s+\w+\s+in\s+")) s += 3;
        if (Regex.IsMatch(sample, @"\belif\b")) s += 4;
        if (Regex.IsMatch(sample, @"\b__init__\b")) s += 5;
        if (Regex.IsMatch(sample, @"\bself\.\w+")) s += 4;
        if (Regex.IsMatch(sample, @"^\s*@\w+", RegexOptions.Multiline)) s += 1;
        if (Regex.IsMatch(sample, @"\bTrue\b|\bFalse\b|\bNone\b")) s += 2;
        if (sample.Contains("\"\"\"") || sample.Contains("'''")) s += 3;
        if (Regex.IsMatch(sample, @"\blambda\s+\w+\s*:")) s += 3;
        if (s > 0) scores["Python"] = s;
    }

    private static void ScoreJavaScript(string sample, string[] lines, Dictionary<string, int> scores)
    {
        int s = 0;
        if (Regex.IsMatch(sample, @"\b(const|let)\s+\w+\s*=")) s += 2;
        if (Regex.IsMatch(sample, @"\bfunction\s+\w+\s*\(")) s += 2;
        if (Regex.IsMatch(sample, @"\b(export\s+)?(default\s+)?function\b")) s += 3;
        if (Regex.IsMatch(sample, @"\b(export\s+)?(default\s+)?class\b")) s += 2;
        if (Regex.IsMatch(sample, @"\bconsole\.(log|warn|error)\s*\(")) s += 4;
        if (Regex.IsMatch(sample, @"\b(import|export)\s+.*\s+from\s+['""]")) s += 5;
        if (Regex.IsMatch(sample, @"\brequire\s*\(['""]")) s += 4;
        if (Regex.IsMatch(sample, @"\bmodule\.exports\b")) s += 5;
        if (Regex.IsMatch(sample, @"=>\s*[\{(]?")) s += 2;
        if (Regex.IsMatch(sample, @"\b(async|await)\b")) s += 2;
        if (Regex.IsMatch(sample, @"\bdocument\.(getElementById|querySelector|createElement)\b")) s += 5;
        if (Regex.IsMatch(sample, @"\bwindow\.\w+")) s += 3;
        if (Regex.IsMatch(sample, @"\b(useState|useEffect|useRef|useCallback)\b")) s += 5;
        if (Regex.IsMatch(sample, @"\b(React|Vue|Angular)\b")) s += 4;
        if (Regex.IsMatch(sample, @"\bnew\s+Promise\b")) s += 3;
        if (Regex.IsMatch(sample, @"\b(interface|type)\s+\w+\s*[{=]")) s += 3;
        if (Regex.IsMatch(sample, @":\s*(string|number|boolean|any)\b")) s += 3;
        if (sample.Contains("===") || sample.Contains("!==")) s += 2;
        if (sample.Contains("undefined")) s += 1;
        if (s > 0) scores["JavaScript"] = s;
    }

    private static void ScoreCpp(string sample, string[] lines, Dictionary<string, int> scores)
    {
        int s = 0;
        if (Regex.IsMatch(sample, @"#include\s*[<""]")) s += 5;
        if (Regex.IsMatch(sample, @"\bstd::\w+")) s += 5;
        if (Regex.IsMatch(sample, @"\bcout\s*<<")) s += 4;
        if (Regex.IsMatch(sample, @"\bcin\s*>>")) s += 4;
        if (Regex.IsMatch(sample, @"\bint\s+main\s*\(")) s += 4;
        if (Regex.IsMatch(sample, @"\bvoid\s+\w+\s*\(")) s += 1;
        if (Regex.IsMatch(sample, @"\b(nullptr|NULL)\b")) s += 3;
        if (Regex.IsMatch(sample, @"\btemplate\s*<")) s += 4;
        if (Regex.IsMatch(sample, @"\bvector\s*<")) s += 3;
        if (Regex.IsMatch(sample, @"\bprintf\s*\(")) s += 3;
        if (Regex.IsMatch(sample, @"\bscanf\s*\(")) s += 3;
        if (Regex.IsMatch(sample, @"\b(struct|typedef)\s+\w+")) s += 2;
        if (Regex.IsMatch(sample, @"\bclass\s+\w+\s*:\s*(public|private|protected)\b")) s += 4;
        if (Regex.IsMatch(sample, @"\b(unique_ptr|shared_ptr|make_unique|make_shared)\b")) s += 5;
        if (Regex.IsMatch(sample, @"#define\s+\w+")) s += 3;
        if (Regex.IsMatch(sample, @"#ifndef\s+\w+")) s += 3;
        if (sample.Contains("->")) s += 1;
        if (sample.Contains("::")) s += 2;
        if (Regex.IsMatch(sample, @"\busing\s+System\b")) s -= 10;
        if (s > 0) scores["C++"] = s;
    }

    private static void ScoreHtml(string sample, string[] lines, Dictionary<string, int> scores)
    {
        int s = 0;
        if (Regex.IsMatch(sample, @"<html[\s>]", RegexOptions.IgnoreCase)) s += 5;
        if (Regex.IsMatch(sample, @"<head[\s>]", RegexOptions.IgnoreCase)) s += 4;
        if (Regex.IsMatch(sample, @"<body[\s>]", RegexOptions.IgnoreCase)) s += 4;
        if (Regex.IsMatch(sample, @"<div[\s>]", RegexOptions.IgnoreCase)) s += 3;
        if (Regex.IsMatch(sample, @"<(p|span|a|h[1-6]|ul|li|table|form|input|button)[\s>]", RegexOptions.IgnoreCase)) s += 2;
        if (Regex.IsMatch(sample, @"<script[\s>]", RegexOptions.IgnoreCase)) s += 3;
        if (Regex.IsMatch(sample, @"<style[\s>]", RegexOptions.IgnoreCase)) s += 3;
        if (Regex.IsMatch(sample, @"<link\s+.*rel=", RegexOptions.IgnoreCase)) s += 3;
        if (Regex.IsMatch(sample, @"class=""[^""]*""")) s += 2;
        if (Regex.IsMatch(sample, @"<!DOCTYPE\s+html", RegexOptions.IgnoreCase)) s += 5;
        if (s > 0) scores["HTML"] = s;
    }

    private static void ScoreXml(string sample, string[] lines, Dictionary<string, int> scores)
    {
        int s = 0;
        if (Regex.IsMatch(sample, @"<\?xml\s+")) s += 5;
        if (Regex.IsMatch(sample, @"xmlns[:=]")) s += 4;
        int tagCount = Regex.Matches(sample, @"<\w+[\s/>]").Count;
        if (tagCount >= 3) s += 2;
        if (Regex.IsMatch(sample, @"<!\[CDATA\[")) s += 4;
        if (Regex.IsMatch(sample, @"<html[\s>]", RegexOptions.IgnoreCase)) s -= 10;
        if (s > 0) scores["XML"] = s;
    }

    private static void ScoreCss(string sample, string[] lines, Dictionary<string, int> scores)
    {
        int s = 0;
        if (Regex.IsMatch(sample, @"[.#][\w-]+\s*\{")) s += 3;
        if (Regex.IsMatch(sample, @"\b(margin|padding|border|display|color|background|font-size|width|height)\s*:")) s += 3;
        if (Regex.IsMatch(sample, @"@media\s*\(")) s += 5;
        if (Regex.IsMatch(sample, @"@import\s+")) s += 3;
        if (Regex.IsMatch(sample, @"@keyframes\s+\w+")) s += 5;
        if (Regex.IsMatch(sample, @"\b(flex|grid|block|inline|none)\s*;")) s += 2;
        if (Regex.IsMatch(sample, @":\s*(hover|focus|active|visited)\s*\{")) s += 4;
        if (Regex.IsMatch(sample, @"\b(px|em|rem|%|vh|vw)\s*[;}]")) s += 2;
        if (Regex.IsMatch(sample, @"\$[\w-]+\s*:")) s += 3;
        if (Regex.IsMatch(sample, @"@mixin\s+\w+")) s += 5;
        if (s > 0) scores["CSS"] = s;
    }

    private static void ScorePhp(string sample, string[] lines, Dictionary<string, int> scores)
    {
        int s = 0;
        if (sample.Contains("<?php")) s += 10;
        if (Regex.IsMatch(sample, @"\$\w+\s*=")) s += 3;
        if (Regex.IsMatch(sample, @"\becho\s+")) s += 3;
        if (Regex.IsMatch(sample, @"\bfunction\s+\w+\s*\(.*\$")) s += 4;
        if (Regex.IsMatch(sample, @"->[\w]+\s*\(")) s += 2;
        if (Regex.IsMatch(sample, @"\barray\s*\(")) s += 3;
        if (Regex.IsMatch(sample, @"\b(public|private|protected)\s+function\b")) s += 5;
        if (Regex.IsMatch(sample, @"\bnew\s+\w+\s*\(")) s += 1;
        if (sample.Contains("$_GET") || sample.Contains("$_POST") || sample.Contains("$_SESSION")) s += 5;
        if (s > 0) scores["PHP"] = s;
    }

    private static void ScoreSql(string sample, string[] lines, Dictionary<string, int> scores)
    {
        int s = 0;
        if (Regex.IsMatch(sample, @"\bSELECT\b.*\bFROM\b", RegexOptions.IgnoreCase | RegexOptions.Singleline)) s += 5;
        if (Regex.IsMatch(sample, @"\bCREATE\s+(TABLE|VIEW|INDEX|DATABASE|PROCEDURE|FUNCTION)\b", RegexOptions.IgnoreCase)) s += 5;
        if (Regex.IsMatch(sample, @"\b(INSERT\s+INTO|UPDATE\s+\w+\s+SET|DELETE\s+FROM)\b", RegexOptions.IgnoreCase)) s += 5;
        if (Regex.IsMatch(sample, @"\bALTER\s+TABLE\b", RegexOptions.IgnoreCase)) s += 4;
        if (Regex.IsMatch(sample, @"\bWHERE\b", RegexOptions.IgnoreCase)) s += 2;
        if (Regex.IsMatch(sample, @"\b(INNER|LEFT|RIGHT|OUTER)\s+JOIN\b", RegexOptions.IgnoreCase)) s += 4;
        if (Regex.IsMatch(sample, @"\bGROUP\s+BY\b", RegexOptions.IgnoreCase)) s += 3;
        if (Regex.IsMatch(sample, @"\bORDER\s+BY\b", RegexOptions.IgnoreCase)) s += 3;
        if (Regex.IsMatch(sample, @"\b(VARCHAR|INT|BIGINT|NVARCHAR|DATETIME|BIT|DECIMAL)\b", RegexOptions.IgnoreCase)) s += 3;
        if (Regex.IsMatch(sample, @"\bBEGIN\b.*\bEND\b", RegexOptions.IgnoreCase | RegexOptions.Singleline)) s += 2;
        if (s > 0) scores["TSQL"] = s;
    }

    private static void ScoreJson(string sample, Dictionary<string, int> scores)
    {
        var trimmed = sample.TrimStart();
        int s = 0;
        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
        {
            if (Regex.IsMatch(trimmed, @"^\s*\{\s*""[\w]+""")) s += 5;
            if (Regex.IsMatch(trimmed, @"^\s*\[\s*(\{|""|[0-9]|true|false|null)")) s += 4;
            if (Regex.IsMatch(sample, @"""[\w]+""\s*:\s*")) s += 3;
            if (Regex.IsMatch(sample, @"\bfunction\b|\bclass\b|\bimport\b|\bvar\b"))
                s -= 10;
        }
        if (s > 0) scores["JSON"] = s;
    }

    private static void ScoreMarkdown(string sample, string[] lines, Dictionary<string, int> scores)
    {
        int s = 0;
        int headingCount = lines.Count(l => Regex.IsMatch(l.TrimStart(), @"^#{1,6}\s+\w"));
        if (headingCount >= 1) s += 2;
        if (headingCount >= 3) s += 3;
        if (Regex.IsMatch(sample, @"^\s*[-*+]\s+\w", RegexOptions.Multiline)) s += 1;
        if (Regex.IsMatch(sample, @"\[.+\]\(.+\)")) s += 3;
        if (Regex.IsMatch(sample, @"```\w*\n")) s += 4;
        if (Regex.IsMatch(sample, @"\*\*.+\*\*")) s += 2;
        if (Regex.IsMatch(sample, @"!\[.+\]\(.+\)")) s += 3;
        if (Regex.IsMatch(sample, @"^\s*>\s+", RegexOptions.Multiline)) s += 2;
        if (Regex.IsMatch(sample, @"\bfunction\b|\bclass\b|\bimport\b") &&
            headingCount == 0) s -= 5;
        if (s > 0) scores["MarkDown"] = s;
    }

    private static void ScorePowerShell(string sample, string[] lines, Dictionary<string, int> scores)
    {
        int s = 0;
        if (Regex.IsMatch(sample, @"\$PSVersionTable\b")) s += 5;
        if (Regex.IsMatch(sample, @"\bfunction\s+\w+-\w+")) s += 5;
        if (Regex.IsMatch(sample, @"\bGet-\w+|Set-\w+|New-\w+|Remove-\w+|Import-\w+")) s += 5;
        if (Regex.IsMatch(sample, @"\bWrite-Host\b|\bWrite-Output\b")) s += 5;
        if (Regex.IsMatch(sample, @"\[Parameter\(")) s += 5;
        if (Regex.IsMatch(sample, @"\bparam\s*\(", RegexOptions.IgnoreCase)) s += 3;
        if (Regex.IsMatch(sample, @"\$_\b")) s += 3;
        if (Regex.IsMatch(sample, @"\|\s*(Where-Object|ForEach-Object|Select-Object)")) s += 5;
        if (Regex.IsMatch(sample, @"\$\w+\s*=") && !sample.Contains("<?php")) s += 1;
        if (Regex.IsMatch(sample, @"\b-eq\b|-ne\b|-lt\b|-gt\b|-like\b|-match\b")) s += 4;
        if (Regex.IsMatch(sample, @"\bCmdletBinding\b")) s += 5;
        if (s > 0) scores["PowerShell"] = s;
    }

    private static void ScoreBatch(string sample, string[] lines, Dictionary<string, int> scores)
    {
        int s = 0;
        if (Regex.IsMatch(sample, @"^@echo\s+off", RegexOptions.IgnoreCase | RegexOptions.Multiline)) s += 10;
        if (Regex.IsMatch(sample, @"\becho\s+", RegexOptions.IgnoreCase) && sample.Contains("%")) s += 3;
        if (Regex.IsMatch(sample, @"\bset\s+\w+=", RegexOptions.IgnoreCase)) s += 3;
        if (Regex.IsMatch(sample, @"\bgoto\s+:\w+", RegexOptions.IgnoreCase)) s += 5;
        if (Regex.IsMatch(sample, @"^:\w+", RegexOptions.Multiline)) s += 3;
        if (Regex.IsMatch(sample, @"%\w+%")) s += 3;
        if (Regex.IsMatch(sample, @"\bif\s+(not\s+)?exist\b", RegexOptions.IgnoreCase)) s += 4;
        if (Regex.IsMatch(sample, @"\bfor\s+/[fdlr]\b", RegexOptions.IgnoreCase)) s += 5;
        if (Regex.IsMatch(sample, @"\bREM\s+", RegexOptions.IgnoreCase)) s += 2;
        if (s > 0) scores["BAT"] = s;
    }

    private static void ScoreRust(string sample, string[] lines, Dictionary<string, int> scores)
    {
        int s = 0;
        if (Regex.IsMatch(sample, @"\bfn\s+\w+\s*\(")) s += 3;
        if (Regex.IsMatch(sample, @"\blet\s+mut\s+\w+")) s += 5;
        if (Regex.IsMatch(sample, @"\blet\s+\w+\s*:\s*\w+")) s += 3;
        if (Regex.IsMatch(sample, @"\bimpl\s+\w+")) s += 5;
        if (Regex.IsMatch(sample, @"\bpub\s+(fn|struct|enum|mod|trait)\b")) s += 5;
        if (Regex.IsMatch(sample, @"\bmatch\s+\w+\s*\{")) s += 4;
        if (Regex.IsMatch(sample, @"\b(Vec|Option|Result|String|Box)<")) s += 5;
        if (Regex.IsMatch(sample, @"println!\s*\(")) s += 5;
        if (Regex.IsMatch(sample, @"\buse\s+std::")) s += 5;
        if (Regex.IsMatch(sample, @"\buse\s+\w+::")) s += 3;
        if (Regex.IsMatch(sample, @"&\w+|&mut\s+\w+")) s += 2;
        if (Regex.IsMatch(sample, @"#\[derive\(")) s += 5;
        if (Regex.IsMatch(sample, @"\b(i32|u32|i64|u64|f64|f32|usize|isize|bool)\b")) s += 3;
        if (s > 0) scores["C++"] = scores.GetValueOrDefault("C++") + s;
    }

    private static void ScoreGo(string sample, string[] lines, Dictionary<string, int> scores)
    {
        int s = 0;
        if (Regex.IsMatch(sample, @"^package\s+\w+", RegexOptions.Multiline)) s += 5;
        if (Regex.IsMatch(sample, @"\bfunc\s+\w+\s*\(")) s += 3;
        if (Regex.IsMatch(sample, @"\bfunc\s+\(\w+\s+\*?\w+\)\s+\w+")) s += 5;
        if (Regex.IsMatch(sample, @"\bfmt\.Print")) s += 5;
        if (Regex.IsMatch(sample, @"\bimport\s*\(")) s += 4;
        if (Regex.IsMatch(sample, @":=")) s += 2;
        if (Regex.IsMatch(sample, @"\bgo\s+\w+\(")) s += 4;
        if (Regex.IsMatch(sample, @"\bchan\s+\w+")) s += 5;
        if (Regex.IsMatch(sample, @"\bdefer\s+")) s += 4;
        if (Regex.IsMatch(sample, @"\btype\s+\w+\s+struct\b")) s += 5;
        if (Regex.IsMatch(sample, @"\btype\s+\w+\s+interface\b")) s += 5;
        if (Regex.IsMatch(sample, @"\bif\s+err\s*!=\s*nil\b")) s += 5;
        if (s > 0) scores["C++"] = scores.GetValueOrDefault("C++") + s;
    }

    private static void ScoreFSharp(string sample, string[] lines, Dictionary<string, int> scores)
    {
        int s = 0;
        if (Regex.IsMatch(sample, @"\blet\s+\w+\s*=")) s += 2;
        if (Regex.IsMatch(sample, @"\blet\s+(rec\s+)?\w+\s+\w+\s*=")) s += 3;
        if (Regex.IsMatch(sample, @"\bmodule\s+\w+")) s += 3;
        if (Regex.IsMatch(sample, @"\|>\s*")) s += 4;
        if (Regex.IsMatch(sample, @"\bopen\s+\w+")) s += 3;
        if (Regex.IsMatch(sample, @"\bmatch\s+\w+\s+with\b")) s += 5;
        if (Regex.IsMatch(sample, @"\bprintfn\s+""")) s += 5;
        if (Regex.IsMatch(sample, @"\btype\s+\w+\s*=")) s += 2;
        if (s > 0) scores["F#"] = s;
    }

    private static void ScoreTex(string sample, string[] lines, Dictionary<string, int> scores)
    {
        int s = 0;
        if (Regex.IsMatch(sample, @"\\documentclass\b")) s += 10;
        if (Regex.IsMatch(sample, @"\\begin\{document\}")) s += 10;
        if (Regex.IsMatch(sample, @"\\(section|subsection|chapter|title|author)\b")) s += 4;
        if (Regex.IsMatch(sample, @"\\(textbf|textit|emph|cite|ref)\b")) s += 3;
        if (Regex.IsMatch(sample, @"\\usepackage\b")) s += 5;
        if (Regex.IsMatch(sample, @"\\begin\{\w+\}")) s += 3;
        if (s > 0) scores["TeX"] = s;
    }

    private static void ScoreIni(string sample, string[] lines, Dictionary<string, int> scores)
    {
        int s = 0;
        int sectionCount = lines.Count(l => Regex.IsMatch(l.Trim(), @"^\[\w+[\w\s]*\]$"));
        if (sectionCount >= 1) s += 3;
        if (sectionCount >= 3) s += 3;
        int kvCount = lines.Count(l => Regex.IsMatch(l.Trim(), @"^\w[\w\s]*\s*=\s*.+"));
        if (kvCount >= 3) s += 3;
        if (lines.Any(l => l.TrimStart().StartsWith(';'))) s += 2;
        if (Regex.IsMatch(sample, @"\bfunction\b|\bclass\b|\bimport\b|\bdef\b"))
            s -= 10;
        if (s > 0) scores["INI"] = s;
    }

    private static void ScoreVb(string sample, string[] lines, Dictionary<string, int> scores)
    {
        int s = 0;
        if (Regex.IsMatch(sample, @"\bModule\s+\w+", RegexOptions.IgnoreCase)) s += 4;
        if (Regex.IsMatch(sample, @"\bSub\s+\w+\s*\(", RegexOptions.IgnoreCase)) s += 3;
        if (Regex.IsMatch(sample, @"\bDim\s+\w+\s+As\b", RegexOptions.IgnoreCase)) s += 5;
        if (Regex.IsMatch(sample, @"\bEnd\s+(Sub|Function|If|Module|Class)\b", RegexOptions.IgnoreCase)) s += 4;
        if (Regex.IsMatch(sample, @"\bImports\s+System\b")) s += 5;
        if (Regex.IsMatch(sample, @"\bMsgBox\s*\(")) s += 4;
        if (s > 0) scores["VB"] = s;
    }

    private static void ScorePatch(string sample, string[] lines, Dictionary<string, int> scores)
    {
        int s = 0;
        if (Regex.IsMatch(sample, @"^diff --git\b", RegexOptions.Multiline)) s += 10;
        if (Regex.IsMatch(sample, @"^---\s+a/", RegexOptions.Multiline)) s += 5;
        if (Regex.IsMatch(sample, @"^\+\+\+\s+b/", RegexOptions.Multiline)) s += 5;
        if (Regex.IsMatch(sample, @"^@@\s+-\d+,\d+\s+\+\d+,\d+\s+@@", RegexOptions.Multiline)) s += 5;
        int plusLines = lines.Count(l => l.StartsWith('+') && !l.StartsWith("+++"));
        int minusLines = lines.Count(l => l.StartsWith('-') && !l.StartsWith("---"));
        if (plusLines > 3 && minusLines > 3) s += 3;
        if (s > 0) scores["Patch"] = s;
    }

    public static IHighlightingDefinition? GetHighlightingByExtension(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        if (string.IsNullOrEmpty(ext))
            return null;

        if (ExtensionToLanguageMap.TryGetValue(ext, out var languageName))
        {
            return HighlightingManager.Instance.GetDefinition(languageName);
        }

        return HighlightingManager.Instance.GetDefinitionByExtension(ext);
    }

    public static IHighlightingDefinition? GetHighlightingByName(string languageName)
    {
        return HighlightingManager.Instance.GetDefinition(languageName);
    }

    public static string GetLanguageDisplayName(string? avalonEditName)
    {
        if (string.IsNullOrEmpty(avalonEditName))
            return "Normal Text";

        if (LanguageDisplayNames.TryGetValue(avalonEditName, out var displayName))
            return displayName;

        return avalonEditName;
    }

    public static string GetLanguageNameByExtension(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        if (string.IsNullOrEmpty(ext))
            return "Normal Text";

        if (ExtensionToLanguageMap.TryGetValue(ext, out var languageName))
            return GetLanguageDisplayName(languageName);

        var definition = HighlightingManager.Instance.GetDefinitionByExtension(ext);
        return definition != null ? GetLanguageDisplayName(definition.Name) : "Normal Text";
    }

    public static IReadOnlyList<(string DisplayName, string? AvalonEditName)> GetAllLanguages()
    {
        var languages = new List<(string DisplayName, string? AvalonEditName)>
        {
            ("Normal Text", null)
        };

        foreach (var def in HighlightingManager.Instance.HighlightingDefinitions.OrderBy(d => d.Name))
        {
            var displayName = GetLanguageDisplayName(def.Name);
            languages.Add((displayName, def.Name));
        }

        return languages;
    }
}
