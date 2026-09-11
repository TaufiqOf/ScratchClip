using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentIcons.Common;

namespace ScratchClip.Models.TextType;

public class PasswordTextType : ATextType
{
    private readonly List<string?> _mataData;
    private const double MinimumConfidence = 0.60;

    public PasswordTextType(string text, ObservableCollection<string> tags, List<string?> mataData) : base(text, tags)
    {
        _mataData = mataData;
        Icon = Icon.Key;
        Text = text;
    }

    public override string DisplayName => "Password";

    public double DetectionConfidence { get; private set; }

    public override bool IsMatch(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        var trimmed = text.Trim();
        
        // Passwords almost never contain internal whitespace
        if (trimmed.Any(char.IsWhiteSpace))
            return false;
        
        // Reject standard code syntax immediately
        if (LooksLikeCodeSnippet(trimmed))
            return false;
        
        var confidence = CalculatePasswordProbability(trimmed);
        
        if (confidence < MinimumConfidence)
            return false;
        
        DetectionConfidence = confidence;
        return true;
    }

    public override Task PopulateMetadataAsync(string text)
    {
        return Task.CompletedTask;
    }

    private static bool LooksLikeCodeSnippet(string input)
    {
        // 1. Method/Function call patterns: Foo(), Bar(arg), etc.
        if (Regex.IsMatch(input, @"^[A-Za-z_]\w*\s*\(.*?\)$"))
            return true;

        // 2. Member accesses ending with semicolon: Class.Method(); or Field.Property;
        if (input.EndsWith(';') || input.EndsWith('{') || input.EndsWith('}'))
            return true;

        // 3. Common programming keywords / PascalCase / CamelCase code structures
        if (Regex.IsMatch(input, @"\b(public|private|protected|internal|class|void|string|int|bool|return|using|namespace|import|function|def|let|const|var|if|else|for|while)\b", RegexOptions.IgnoreCase))
            return true;

        // 4. Typical dot-notation code identifier (e.g., Tags.Add, CalculateFingerprintScore)
        if (Regex.IsMatch(input, @"^[A-Za-z_]\w*(\.[A-Za-z_]\w*)+$"))
            return true;

        return false;
    }

    private static double CalculatePasswordProbability(string input)
    {
        var length = input.Length;

        if (length < 6 || length > 256)
            return 0.0;

        // 1. Character Set Diversity
        var hasUpper = input.Any(char.IsUpper);
        var hasLower = input.Any(char.IsLower);
        var hasDigit = input.Any(char.IsDigit);
        var hasSpecial = input.Any(c => !char.IsLetterOrDigit(c));

        var charTypeCount = 0;
        if (hasUpper) charTypeCount++;
        if (hasLower) charTypeCount++;
        if (hasDigit) charTypeCount++;
        if (hasSpecial) charTypeCount++;

        var diversityScore = charTypeCount / 4.0;

        // 2. Non-Alphanumeric & Symbol Ratio
        var nonAlphaCount = input.Count(c => !char.IsLetter(c));
        var nonAlphaRatio = (double)nonAlphaCount / length;

        // 3. Shannon Entropy
        var entropy = CalculateShannonEntropy(input);
        var normalizedEntropy = Math.Clamp(entropy / 4.5, 0.0, 1.0);

        // 4. Character Switch Transitions
        var transitions = 0;
        for (var i = 0; i < length - 1; i++)
        {
            if (GetCharCategory(input[i]) != GetCharCategory(input[i + 1]))
                transitions++;
        }
        var transitionRatio = (double)transitions / (length - 1);

        // 5. Pattern Penalties
        var penalty = 0.0;

        // CamelCase / PascalCase plain identifiers penalty (e.g., CalculateFingerprintScore)
        if (Regex.IsMatch(input, @"^[A-Z][a-z]+(?:[A-Z][a-z]+)+$"))
            penalty += 0.45;

        if (Regex.IsMatch(input, @"^[A-Z][a-z]+$", RegexOptions.CultureInvariant))
            penalty += 0.40;

        if (Regex.IsMatch(input, @"(.)\1{2,}"))
            penalty += 0.25;

        if (ContainsSequentialPattern(input))
            penalty += 0.30;

        // 6. Password Bonuses
        var bonus = 0.0;

        if (charTypeCount >= 4)
            bonus += 0.15;

        if (length >= 16)
            bonus += 0.10;

        if (nonAlphaRatio >= 0.30)
            bonus += 0.15;

        var baseScore = (normalizedEntropy * 0.30) + 
                        (diversityScore * 0.35) + 
                        (transitionRatio * 0.25) + 
                        (nonAlphaRatio * 0.10);

        var finalScore = baseScore - penalty + bonus;

        return Math.Clamp(finalScore, 0.0, 1.0);
    }

    private static double CalculateShannonEntropy(string input)
    {
        var map = new Dictionary<char, int>();
        foreach (var c in input)
        {
            if (!map.ContainsKey(c))
                map[c] = 0;
            map[c]++;
        }

        var result = 0.0;
        var len = (double)input.Length;

        foreach (var count in map.Values)
        {
            var p = count / len;
            result -= p * Math.Log2(p);
        }

        return result;
    }

    private static int GetCharCategory(char c)
    {
        if (char.IsDigit(c)) return 1;
        if (char.IsLower(c)) return 2;
        if (char.IsUpper(c)) return 3;
        return 4;
    }

    private static bool ContainsSequentialPattern(string input)
    {
        const string qwerty = "qwertyuiopasdfghjklzxcvbnm";
        const string digits = "0123456789";

        var lower = input.ToLowerInvariant();

        for (var i = 0; i <= lower.Length - 3; i++)
        {
            var sub = lower.Substring(i, 3);
            if (qwerty.Contains(sub) || digits.Contains(sub))
                return true;
        }

        return false;
    }
}