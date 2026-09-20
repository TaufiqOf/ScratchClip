using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentIcons.Common;
using ScratchClip.Helper;
using TreeSitter;

namespace ScratchClip.Models.TextType;

public class CodeTextType : ATextType
{
    public CodeTextType(
        string text,
        ObservableCollection<string> tags,
        List<string>? _) : base(text, tags)
    {
        Icon = Icon.CodeBlock;
        Text = text;
    }

    public override string DisplayName => "CODE";

    public override string SuggestedExtension => CodeDetectionConfig.Languages.FirstOrDefault(q=>q.Name==DetectedLanguage)?.Extension ?? ".txt";

    public string? DetectedLanguage { get; private set; }

    public double DetectionConfidence { get; private set; }

    public override bool IsMatch(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim();

        // Reject plain scalar values like "40", "3.14", "true".
        if (Regex.IsMatch(
                trimmed,
                CodeDetectionConfig.ScalarRegex,
                RegexOptions.IgnoreCase))
        {
            return false;
        }

        // Reject natural prose before checking code heuristics.
        if (LooksLikeNaturalProse(trimmed))
            return false;

        // Very obvious programming constructs.
        if (HasStrongCodePattern(trimmed))
        {
            var result = DetectLanguage(trimmed);

            if (result is not null &&
                result.Value.Confidence >= CodeDetectionConfig.MinimumConfidence)
            {
                SetDetectionResult(result.Value);
                return true;
            }

            return false;
        }

        // General code heuristics.
        var words = trimmed.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries);

        var keywordMatches = Regex.Matches(
            trimmed,
            CodeDetectionConfig.KeywordRegex,
            RegexOptions.IgnoreCase);

        var keywordCount = keywordMatches.Count;

        // Require keyword density relative to word count.
        // This prevents matching long paragraphs with only 1-2 keywords.
        var keywordDensity =
            words.Length > 0
                ? (double)keywordCount / words.Length
                : 0;

        var strongSymbolCount = Regex.Matches(
            trimmed,
            CodeDetectionConfig.StrongSymbolRegex).Count;

        var operatorCount = Regex.Matches(
            trimmed,
            CodeDetectionConfig.OperatorRegex).Count;

        var isMultiline = trimmed.Contains('\n');

        var looksLikeCode =
            (
                isMultiline &&
                strongSymbolCount >= CodeDetectionConfig.MinimumMultilineSymbols &&
                keywordCount >= CodeDetectionConfig.MinimumMultilineKeywords
            )
            ||
            (
                keywordCount >= CodeDetectionConfig.MinimumKeywordMatches &&
                keywordDensity >= CodeDetectionConfig.MinimumKeywordDensity &&
                (
                    strongSymbolCount >= CodeDetectionConfig.MinimumSymbolMatches ||
                    operatorCount >= CodeDetectionConfig.MinimumOperatorMatches
                )
            );

        if (!looksLikeCode)
            return false;

        var result2 = DetectLanguage(trimmed);

        if (result2 is null ||
            result2.Value.Confidence < CodeDetectionConfig.MinimumConfidence)
        {
            return false;
        }

        SetDetectionResult(result2.Value);

        return true;
    }

    public override Task PopulateMetadataAsync(string text)
    {
        return Task.CompletedTask;
    }

    public override Task UpdateTagsAsync(string text)
    {
        if (!string.IsNullOrEmpty(DetectedLanguage))
            Tags.Add(DetectedLanguage);

        return Task.CompletedTask;
    }

    private void SetDetectionResult(DetectionResult result)
    {
        DetectedLanguage = result.Language;
        DetectionConfidence = result.Confidence;
    }

    private static bool LooksLikeNaturalProse(string text)
    {
        // Check for standard sentence structure ending with punctuation.
        var sentenceCount =
            Regex.Matches(
                text,
                CodeDetectionConfig.SentenceRegex).Count;

        // High space-to-symbol ratio.
        var letterOrSpaceCount =
            text.Count(c =>
                char.IsLetter(c) ||
                char.IsWhiteSpace(c));

        var symbolCount =
            text.Count(c =>
                !char.IsLetterOrDigit(c) &&
                !char.IsWhiteSpace(c));

        var totalChars = text.Length;

        var letterSpaceRatio =
            totalChars > 0
                ? (double)letterOrSpaceCount / totalChars
                : 0;

        // Count characters commonly used by programming languages.
        var codePunctuationCount =
            text.Count(c =>
                c is ';'
                    or '{'
                    or '}'
                    or '='
                    or '<'
                    or '>'
                    or '['
                    or ']');

        // Multi-sentence paragraph with high percentage of letters/spaces
        // and almost no code punctuation.
        if (
            sentenceCount >= CodeDetectionConfig.ProseSentenceCount &&
            letterSpaceRatio > CodeDetectionConfig.ProseLetterSpaceRatio &&
            codePunctuationCount < CodeDetectionConfig.ProseMaxCodePunctuation)
        {
            return true;
        }

        // Long paragraph with no meaningful code structure.
        var words =
            text.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries);

        if (
            words.Length > CodeDetectionConfig.LongProseWordCount &&
            sentenceCount >= CodeDetectionConfig.MinimumProseSentences &&
            codePunctuationCount == 0 &&
            symbolCount < CodeDetectionConfig.LongProseMaxSymbols)
        {
            return true;
        }

        return false;
    }

    private static bool HasStrongCodePattern(string text)
    {
        return Regex.IsMatch(
            text,
            CodeDetectionConfig.StrongCodePatternRegex,
            RegexOptions.IgnoreCase |
            RegexOptions.Multiline |
            RegexOptions.IgnorePatternWhitespace);
    }

    private static DetectionResult? DetectLanguage(string source)
    {
        var candidates = new List<DetectionResult>();

        foreach (var definition in CodeDetectionConfig.Languages)
        {
            try
            {
                var result =
                    DetectLanguageWithParser(
                        source,
                        definition);

                if (result is not null)
                    candidates.Add(result.Value);
            }
            catch (Exception ex)
            {
                // A missing/broken grammar should not prevent
                // the remaining languages from being tested.
                Console.WriteLine(
                    $"Tree-sitter failed for {definition.Name}: {ex.Message}");
            }
        }

        return candidates
            .OrderByDescending(x => x.Confidence)
            .FirstOrDefault();
    }

    private static DetectionResult? DetectLanguageWithParser(
        string source,
        CodeDetectionConfig.LanguageDefinition definition)
    {
        using var language = new Language(
            definition.NativeLibrary,
            definition.FunctionName);

        using var parser = new Parser(language);

        using var tree = parser.Parse(source);

        if (tree == null)
            return null;

        var root = tree.RootNode;

        if (root.Children.Count == 0)
            return null;

        var syntaxScore =
            CalculateSyntaxScore(root);

        var fingerprintScore =
            CalculateFingerprintScore(
                source,
                definition.Keywords);

        // Parser acceptance alone is too permissive
        // for short/plain text.
        if (fingerprintScore <= 0)
            return null;

        /*
         * Syntax parsing is the strongest signal.
         *
         * Default:
         * 70% syntax validity
         * 30% language-specific fingerprints
         */
        var confidence =
            syntaxScore * CodeDetectionConfig.SyntaxWeight +
            fingerprintScore * CodeDetectionConfig.FingerprintWeight;

        return new DetectionResult(
            definition.Name,
            confidence);
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
            ref missingNodes);

        if (totalNodes == 0)
            return 0;

        /*
         * Error nodes are strong evidence that the grammar
         * is not correct for this source.
         *
         * Missing nodes are also evidence, but slightly weaker.
         */
        var errorRatio =
            (double)errorNodes / totalNodes;

        var missingRatio =
            (double)missingNodes / totalNodes;

        var score =
            1.0
            - errorRatio * CodeDetectionConfig.ErrorNodePenalty
            - missingRatio * CodeDetectionConfig.MissingNodePenalty;

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
                ref missingNodes);
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

            // Plain identifier/keyword.
            if (Regex.IsMatch(
                    trimmed,
                    CodeDetectionConfig.PlainIdentifierRegex))
            {
                matched = Regex.IsMatch(
                    source,
                    $@"\b{Regex.Escape(trimmed)}\b",
                    RegexOptions.IgnoreCase);
            }
            else
            {
                // Literal code fragment such as:
                //
                // "using "
                // "Console."
                // "Task<"
                // "=>"
                //
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
        double Confidence);
}


// ============================================================
// CENTRAL CONFIGURATION
// ============================================================