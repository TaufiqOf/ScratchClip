using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentIcons.Common;
using TreeSitter;

namespace ScratchClip.Models.TextType;

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

    public CodeTextType(string text, ObservableCollection<string> tags, List<string?> mataData) : base(text, tags)
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

        // Prose Guard: Reject natural prose paragraphs before checking heuristics
        if (LooksLikeNaturalProse(trimmed))
            return false;

        // Very obvious programming constructs.
        if (HasStrongCodePattern(trimmed))
        {
            var result = DetectLanguage(trimmed);

            if (result is not null && result.Value.Confidence >= MinimumConfidence)
            {
                DetectedLanguage = result.Value.Language;
                DetectionConfidence = result.Value.Confidence;
                return true;
            }

            return false;
        }

        // General code heuristics.
        var words = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        
        var keywordMatches = Regex.Matches(
            trimmed,
            @"\b(class|interface|struct|enum|namespace|public|private|protected|internal|using|return|function|def|let|const|var|import|export|async|await|new|void|static|fn|func|package|impl|trait)\b",
            RegexOptions.IgnoreCase
        );

        var keywordCount = keywordMatches.Count;

        // Require keyword density relative to word count (prevents matching long paragraphs with 1-2 keywords)
        var keywordDensity = words.Length > 0 ? (double)keywordCount / words.Length : 0;

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
            (keywordCount >= 2 && keywordDensity >= 0.08 && (strongSymbolCount >= 1 || operatorCount >= 1));

        if (!looksLikeCode)
            return false;

        var result2 = DetectLanguage(trimmed);

        if (result2 is null || result2.Value.Confidence < MinimumConfidence)
            return false;

        DetectedLanguage = result2.Value.Language;
        DetectionConfidence = result2.Value.Confidence;
        return true;
    }

    private static bool LooksLikeNaturalProse(string text)
    {
        // 1. Check for standard sentence structure ending with punctuation
        var sentenceCount = Regex.Matches(text, @"\b[A-Z][^.!?]*[.!?]").Count;
        
        // 2. High space-to-symbol ratio (prose has mostly letters and spaces, few syntax characters)
        var letterOrSpaceCount = text.Count(c => char.IsLetter(c) || char.IsWhiteSpace(c));
        var symbolCount = text.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));

        var totalChars = text.Length;
        var letterSpaceRatio = (double)letterOrSpaceCount / totalChars;

        // 3. Absolute lack of mandatory code punctuation
        var codePunctuationCount = text.Count(c => c is ';' or '{' or '}' or '=' or '<' or '>' or '[' or ']');

        // If it's a multi-sentence paragraph with >92% letters/spaces and almost zero code punctuation, it's prose.
        if (sentenceCount >= 2 && letterSpaceRatio > 0.90 && codePunctuationCount < 2)
            return true;

        // If single paragraph prose with high word count and no code structural markers
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 15 && sentenceCount >= 1 && codePunctuationCount == 0 && symbolCount < 5)
            return true;

        return false;
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
        if (!string.IsNullOrEmpty(DetectedLanguage))
        {
            Tags.Add(DetectedLanguage);
        }
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
        if(tree == null)
            return null;
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
            var trimmed = keyword.Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            bool matched;

            // Plain identifier/keyword
            if (Regex.IsMatch(trimmed, @"^\w+$"))
            {
                matched = Regex.IsMatch(
                    source,
                    $@"\b{Regex.Escape(trimmed)}\b",
                    RegexOptions.IgnoreCase);
            }
            else
            {
                // Literal code fragment such as:
                // "using "
                // "Console."
                // "Task<"
                // "=>"
                matched = source.Contains(
                    trimmed,
                    StringComparison.OrdinalIgnoreCase);
            }

            if (matched)
                matches++;
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