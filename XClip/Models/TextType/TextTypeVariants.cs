using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentIcons.Common;
using TreeSitter;

namespace XClip.Models.TextType;

public class CodeTextType : ATextType
{
    private const double MinimumConfidence = 0.65;

    private static readonly LanguageDefinition[] Languages =
    [
        new(
            "C#",
            "libtree-sitter-c-sharp.so",
            "tree_sitter_c_sharp",
            [
                "using ",
                "namespace ",
                "public ",
                "private ",
                "protected ",
                "internal ",
                "class ",
                "interface ",
                "record ",
                "struct ",
                "enum ",
                "Console.",
                "async ",
                "await ",
                "var ",
                "string ",
                "int ",
                "bool ",
                "Task<",
                "=>"
            ]),

        new(
            "Python",
            "libtree-sitter-python.so",
            "tree_sitter_python",
            [
                "def ",
                "async def ",
                "import ",
                "from ",
                "elif ",
                "None",
                "True",
                "False",
                "self",
                "print(",
                "__init__",
                "__name__",
                "lambda ",
                "yield ",
                "with ",
                "except "
            ]),

        new(
            "JavaScript",
            "libtree-sitter-javascript.so",
            "tree_sitter_javascript",
            [
                "const ",
                "let ",
                "function ",
                "console.",
                "require(",
                "module.exports",
                "export ",
                "import ",
                "=>",
                "undefined",
                "Promise",
                "async ",
                "await ",
                "document.",
                "window."
            ]),

        new(
            "Java",
            "libtree-sitter-java.so",
            "tree_sitter_java",
            [
                "public class ",
                "private class ",
                "protected class ",
                "System.out.",
                "package ",
                "import java.",
                "implements ",
                "extends ",
                "public static void ",
                "private static ",
                "interface ",
                "new ",
                "throws ",
                "boolean ",
                "String "
            ]),

        new(
            "C",
            "libtree-sitter-c.so",
            "tree_sitter_c",
            [
                "#include ",
                "#define ",
                "int main(",
                "printf(",
                "scanf(",
                "malloc(",
                "free(",
                "NULL",
                "typedef ",
                "struct ",
                "sizeof("
            ]),

        new(
            "C++",
            "libtree-sitter-cpp.so",
            "tree_sitter_cpp",
            [
                "#include ",
                "#define ",
                "std::",
                "cout",
                "cin",
                "nullptr",
                "public:",
                "private:",
                "protected:",
                "template<",
                "class ",
                "namespace ",
                "vector<",
                "string ",
                "using namespace "
            ]),

        new(
            "Go",
            "libtree-sitter-go.so",
            "tree_sitter_go",
            [
                "package main",
                "package ",
                "import (",
                "import \"",
                "func ",
                "go ",
                "defer ",
                "chan ",
                "struct {",
                "interface {",
                "fmt.",
                ":=",
                "make(",
                "range "
            ]),

        new(
            "Rust",
            "libtree-sitter-rust.so",
            "tree_sitter_rust",
            [
                "fn ",
                "let mut ",
                "let ",
                "pub fn ",
                "impl ",
                "trait ",
                "struct ",
                "enum ",
                "use ",
                "mod ",
                "crate::",
                "println!(",
                "vec![",
                "match ",
                "Some(",
                "None",
                "Result<",
                "Option<"
            ])
    ];

    public CodeTextType(string text, ObservableCollection<string> tags) : base(text, tags)
    {
        Icon = Icon.CodeBlock;
        Text = text;
    }

    public override string DisplayName => "Code";

    public string? DetectedLanguage { get; private set; }

    public double DetectionConfidence { get; private set; }

    public override bool IsMatch(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim();

        // Reject plain scalar values like "40", "3.14", "true" early.
        if (Regex.IsMatch(trimmed, @"^(?:[+-]?\d+(?:\.\d+)?|true|false|null)$", RegexOptions.IgnoreCase))
            return false;

        // Very obvious programming constructs.
        if (HasStrongCodePattern(trimmed))
        {
            var result = DetectLanguage(trimmed);

            if (result is not null && result.Value.Confidence >= MinimumConfidence)
            {
                DetectedLanguage = result.Value.Language;
                DetectionConfidence = result.Value.Confidence;
                Tags.Add(DetectedLanguage);
                return true;
            }

            return false;
        }

        // General code heuristics.
        var keywordCount = Regex.Matches(
            trimmed,
            @"\b(class|interface|struct|enum|namespace|public|private|protected|internal|using|return|function|def|let|const|var|import|export|async|await|new|void|static|fn|func|package|impl|trait)\b",
            RegexOptions.IgnoreCase
        ).Count;

        var strongSymbolCount = Regex.Matches(
            trimmed,
            @"(=>|==|!=|<=|>=|\{|\}|\[|\]|;)"
        ).Count;

        var operatorCount = Regex.Matches(
            trimmed,
            @"(=>|==|!=|<=|>=|&&|\|\||\+\+|--|\+=|-=|\*=|/=)"
        ).Count;

        var isMultiline = trimmed.Contains('\n');

        var looksLikeCode =
            (isMultiline && strongSymbolCount >= 2 && keywordCount >= 1) ||
            (keywordCount >= 2 && (strongSymbolCount >= 1 || operatorCount >= 1));

        if (!looksLikeCode)
            return false;

        var result2 = DetectLanguage(trimmed);

        if (result2 is null || result2.Value.Confidence < MinimumConfidence)
            return false;

        DetectedLanguage = result2.Value.Language;
        DetectionConfidence = result2.Value.Confidence;
        Tags.Add(DetectedLanguage);
        return true;
    }

    private static bool HasStrongCodePattern(string text)
    {
        return Regex.IsMatch(
            text,
            """
            ^\s*
            (
                (public|private|protected|internal)\s+
                    (class|interface|struct|enum|record)\b
                |
                (if|for|foreach|while|switch|try|catch)\s*\(
                |
                (using|namespace)\s+[\w.]+
                |
                (def|func|function|fn)\s+\w+
                |
                (package|import)\s+[\w."]
                |
                #\s*(include|define)\b
            )
            """,
            RegexOptions.IgnoreCase |
            RegexOptions.Multiline |
            RegexOptions.IgnorePatternWhitespace
        );
    }

    public override Task PopulateMetadataAsync(string text)
    {
        return Task.CompletedTask;
    }

    private static DetectionResult? DetectLanguage(string source)
    {
        var candidates = new List<DetectionResult>();

        foreach (var definition in Languages)
        {
            try
            {
                var result = DetectLanguageWithParser(source, definition);

                if (result is not null)
                    candidates.Add(result.Value);
            }
            catch (Exception ex)
            {
                // A missing/broken grammar should not prevent
                // the remaining languages from being tested.
                Console.WriteLine(
                    $"Tree-sitter failed for {definition.Name}: {ex.Message}"
                );
            }
        }

        return candidates
            .OrderByDescending(x => x.Confidence)
            .FirstOrDefault();
    }

    private static DetectionResult? DetectLanguageWithParser(
        string source,
        LanguageDefinition definition)
    {
        using var language = new Language(
            definition.NativeLibrary,
            definition.FunctionName
        );

        using var parser = new Parser(language);
        using var tree = parser.Parse(source);

        var root = tree.RootNode;

        if (root.Children.Count == 0)
            return null;

        var syntaxScore = CalculateSyntaxScore(root);

        var fingerprintScore =
            CalculateFingerprintScore(source, definition.Keywords);

        // Parser acceptance alone is too permissive for short/plain text.
        if (fingerprintScore <= 0)
            return null;

        /*
         * Syntax parsing is the strongest signal.
         *
         * 70% syntax validity
         * 30% language-specific fingerprints
         */
        var confidence =
            syntaxScore * 0.70 +
            fingerprintScore * 0.30;

        return new DetectionResult(
            definition.Name,
            confidence,
            syntaxScore,
            fingerprintScore
        );
    }

    private static double CalculateSyntaxScore(Node root)
    {
        var totalNodes = 0;
        var errorNodes = 0;
        var missingNodes = 0;

        CountSyntaxNodes(
            root,
            ref totalNodes,
            ref errorNodes,
            ref missingNodes
        );

        if (totalNodes == 0)
            return 0;

        /*
         * Error nodes are strong evidence that the grammar
         * is not correct for this source.
         *
         * Missing nodes are also evidence, but slightly weaker.
         */
        var errorRatio = (double)errorNodes / totalNodes;
        var missingRatio = (double)missingNodes / totalNodes;

        var score =
            1.0 -
            (errorRatio * 1.5) -
            (missingRatio * 0.75);

        return Math.Clamp(score, 0, 1);
    }

    private static void CountSyntaxNodes(
        Node node,
        ref int totalNodes,
        ref int errorNodes,
        ref int missingNodes)
    {
        totalNodes++;

        if (node.IsError)
            errorNodes++;

        if (node.IsMissing)
            missingNodes++;

        for (var i = 0; i < node.Children.Count; i++)
        {
            CountSyntaxNodes(
                node.Children[i],
                ref totalNodes,
                ref errorNodes,
                ref missingNodes
            );
        }
    }

    private static double CalculateFingerprintScore(
        string source,
        IReadOnlyList<string> keywords)
    {
        if (keywords.Count == 0)
            return 0;

        var matches = 0;

        foreach (var keyword in keywords)
        {
            // Only count keywords that are likely in code context
            // (surrounded by word boundaries, not mid-sentence)
            var pattern = $@"\b{Regex.Escape(keyword.Trim())}\b";
            if (Regex.IsMatch(source, pattern, RegexOptions.IgnoreCase))
            {
                matches++;
            }
        }

        return (double)matches / keywords.Count;
    }

    private readonly record struct DetectionResult(
        string Language,
        double Confidence,
        double SyntaxScore,
        double FingerprintScore
    );

    private sealed record LanguageDefinition(
        string Name,
        string NativeLibrary,
        string FunctionName,
        IReadOnlyList<string> Keywords
    );
}