using System.Collections.ObjectModel;
using ScratchClip.Models;

namespace ScratchClip.Helper;

public static class AutoTagSettings
{
    static AutoTagSettings()
    {
        Tags = new ObservableCollection<AutoTag>(DefaultTags);
    }
    public static ObservableCollection<AutoTag> Tags { get; set; }

    public static ObservableCollection<AutoTag> DefaultTags { get; set; } =
    [
        // Email address
        new AutoTag
        {
            TagName = "EMAIL",
            Regex = @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b"
        },

        // Phone number
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

        // IPv6 address
        new AutoTag
        {
            TagName = "IPV6",
            Regex = @"(?i)(?<![0-9a-f:])(?:[0-9a-f]{1,4}:){2,7}[0-9a-f]{1,4}(?![0-9a-f:])"
        },

        // Date
        new AutoTag
        {
            TagName = "DATE",
            Regex = @"\b(?:\d{1,2}[/-]\d{1,2}[/-]\d{2,4}|\d{4}[/-]\d{1,2}[/-]\d{1,2})\b"
        },

        // Date with day of the week
        new AutoTag
        {
            TagName = "DATE",
            Regex =
                @"\b(?:(?:Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday)(?:,\s*|\s+the\s+)?(?:(?:\d{1,2})(?:st|nd|rd|th)?\s+(?:of\s+)?(?:January|February|March|April|May|June|July|August|September|October|November|December)|(?:January|February|March|April|May|June|July|August|September|October|November|December)\s+\d{1,2}(?:st|nd|rd|th)?)(?:,?\s+\d{4})?|(?:(?:\d{1,2})(?:st|nd|rd|th)?\s+(?:of\s+)?(?:January|February|March|April|May|June|July|August|September|October|November|December))(?:\s+\d{4})?|(?:January|February|March|April|May|June|July|August|September|October|November|December)\s+\d{1,2}(?:st|nd|rd|th)?(?:,?\s+\d{4})?)\b"
        },

        // Time
        new AutoTag
        {
            TagName = "TIME",
            Regex = @"\b(?:[01]?\d|2[0-3]):[0-5]\d(?:\s?[AP]M)?\b"
        },

        // Credit-card-like number
        new AutoTag
        {
            TagName = "CARD_NUMBER",
            Regex = @"(?<!\d)(?:\d{4}[- ]?){3}\d{4}(?!\d)"
        },

        // UUID / GUID
        new AutoTag
        {
            TagName = "UUID",
            Regex = @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-5][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}\b"
        },

        // GitHub
        new AutoTag
        {
            TagName = "GITHUB",
            Regex = @"(?<![\w@])(?:https?://)?(?:www\.)?github\.com(?:/[A-Za-z0-9_.-]+)*/*"
        },

// GitLab
        new AutoTag
        {
            TagName = "GITLAB",
            Regex = @"(?<![\w@])(?:https?://)?(?:www\.)?gitlab\.com(?:/[A-Za-z0-9_.-]+)*/*"
        },

// Reddit
        new AutoTag
        {
            TagName = "REDDIT",
            Regex = @"(?<![\w@])(?:https?://)?(?:www\.)?reddit\.com(?:/[A-Za-z0-9_.-]+)*/*"
        },

// Twitter / X
        new AutoTag
        {
            TagName = "X",
            Regex = @"(?<![\w@])(?:https?://)?(?:www\.)?(?:twitter\.com|x\.com)(?:/[A-Za-z0-9_.-]+)*/*"
        },

// Facebook
        new AutoTag
        {
            TagName = "FACEBOOK",
            Regex = @"(?<![\w@])(?:https?://)?(?:www\.)?facebook\.com(?:/[A-Za-z0-9_.-]+)*/*"
        },

// Instagram
        new AutoTag
        {
            TagName = "INSTAGRAM",
            Regex = @"(?<![\w@])(?:https?://)?(?:www\.)?instagram\.com(?:/[A-Za-z0-9_.-]+)*/*"
        },
        // Example User Email
        new AutoTag
        {
            Enabled = false,
            TagName = "USER1_EMAIL",
            Regex = @"(?<!\S)user1@example\.com(?!\S)"
        },
        // Example User Email
        new AutoTag
        {
            Enabled = false,
            TagName = "CHAT",
            Regex = @"^\[\d{1,2}:\d{2},\s*\d{1,2}/\d{1,2}/\d{4}\]\s*[^:]+:"
        },
        // Example User Email
        new AutoTag
        {
            Enabled = false,
            TagName = "CHAT_FROM_USER",
            Regex = @"^(?mi)^\[\d{2}:\d{2},\s*\d{1,2}/\d{1,2}/\d{4}\]\s*.*(?:User).*$"
        },
    ];

}