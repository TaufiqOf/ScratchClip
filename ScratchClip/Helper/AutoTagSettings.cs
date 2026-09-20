using System.Collections.ObjectModel;
using ScratchClip.Models;

namespace ScratchClip.Helper;

public static class AutoTagSettings
{
    public static ObservableCollection<AutoTag> Tags { get; set; } =
    [
        new AutoTag
        {
            TagName = "EMAIL",
            Regex = @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b"
        }, // Phone number
        new AutoTag
        {
            TagName = "PHONE",
            Regex = @"(?<!\w)(?:\+?\d[\d\s().-]{7,}\d)(?!\w)"
        },

        // IPv4 address
        new AutoTag
        {
            TagName = "IP_ADDRESS",
            Regex = @"\b(?:(?:25[0-5]|2[0-4]\d|1?\d?\d)\.){3}(?:25[0-5]|2[0-4]\d|1?\d?\d)\b"
        },

        // IPv6
        new AutoTag
        {
            TagName = "IPV6",
            Regex = @"(?i)(?<![0-9a-f:])(?:[0-9a-f]{1,4}:){2,7}[0-9a-f]{1,4}(?![0-9a-f:])"
        },

        // Date
        new()
        {
            TagName = "DATE",
            Regex = @"\b(?:\d{1,2}[/-]\d{1,2}[/-]\d{2,4}|\d{4}[/-]\d{1,2}[/-]\d{1,2})\b"
        },

        // Time
        new()
        {
            TagName = "TIME",
            Regex = @"\b(?:[01]?\d|2[0-3]):[0-5]\d(?:\s?[AP]M)?\b"
        },

        // Credit-card-like number
        new()
        {
            TagName = "CARD_NUMBER",
            Regex = @"(?<!\d)(?:\d{4}[- ]?){3}\d{4}(?!\d)"
        },

        // UUID / GUID
        new()
        {
            TagName = "UUID",
            Regex = @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-5][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}\b"
        },

        // GitHub
        new()
        {
            TagName = "GITHUB",
            Regex = @"\b(?:https?://)?(?:www\.)?github\.com/[A-Za-z0-9_.-]+(?:/[A-Za-z0-9_.-]+)*\b"
        },

        // GitLab
        new()
        {
            TagName = "GITLAB",
            Regex = @"\b(?:https?://)?(?:www\.)?gitlab\.com/[A-Za-z0-9_.-]+(?:/[A-Za-z0-9_.-]+)*\b"
        },

        // Reddit
        new()
        {
            TagName = "REDDIT",
            Regex = @"\b(?:https?://)?(?:www\.)?reddit\.com/(?:r|u|user)/[A-Za-z0-9_-]+\b"
        },

        // Twitter / X
        new AutoTag
        {
            TagName = "SOCIAL",
            Regex = @"\b(?:https?://)?(?:www\.)?(?:twitter\.com|x\.com)(?:/[A-Za-z0-9_]+)?\b"
        },
        // Date with day of the week
        new AutoTag
        {
            TagName = "DATE",
            Regex =
                @"\b(?:(?:Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday)(?:,\s*|\s+the\s+)?(?:(?:\d{1,2})(?:st|nd|rd|th)?\s+(?:of\s+)?(?:January|February|March|April|May|June|July|August|September|October|November|December)|(?:January|February|March|April|May|June|July|August|September|October|November|December)\s+\d{1,2}(?:st|nd|rd|th)?)(?:,?\s+\d{4})?|(?:(?:\d{1,2})(?:st|nd|rd|th)?\s+(?:of\s+)?(?:January|February|March|April|May|June|July|August|September|October|November|December))(?:\s+\d{4})?|(?:January|February|March|April|May|June|July|August|September|October|November|December)\s+\d{1,2}(?:st|nd|rd|th)?(?:,?\s+\d{4})?)\b"
        }
    ];
}