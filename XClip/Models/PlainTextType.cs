using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace XClip.Models;

public class PlainTextType : ATextType
{
    public override string DisplayName => "Plain Text";

    public override bool IsMatch(string text)
    {
        return true;
    }

    public override Task PopulateMetadataAsync(string text)
    {
        return Task.CompletedTask;
    }
}

public partial class WebsiteTextType : ATextType
{
    public WebsiteTextType(string text)
    {
        Text = text;
    }

    public string Text { get; set; }

    public override string DisplayName => "Website";

    static WebsiteTextType()
    {
        MetadataClient.DefaultRequestHeaders.UserAgent.ParseAdd("XClip/1.0 (+https://localhost)");
    }

    private static readonly HttpClient MetadataClient = new()
    {
        Timeout = TimeSpan.FromSeconds(4)
    };

    public override bool IsMatch(string text)
    {
        return TryGetWebsiteUri(text, out _);
    }

    public override async Task PopulateMetadataAsync(string text)
    {
        await PopulateWebsiteMetadataAsync();
    }

    public bool IsWebSite
    {
        get;
        private set
        {
            if (value == field) return;
            field = value;
            if (!value)
            {
                WebsiteTitle = string.Empty;
                WebsiteDescription = string.Empty;
                WebsiteHost = string.Empty;
            }
            else
            {
            }

            OnPropertyChanged();
        }
    }

    public string WebsiteTitle
    {
        get;
        set
        {
            if (value == field) return;
            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    public string WebsiteDescription
    {
        get;
        set
        {
            if (value == field) return;
            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    public string WebsiteHost
    {
        get;
        set
        {
            if (value == field) return;
            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    [RelayCommand]
    private void OpenWebsite()
    {
        if (!TryGetWebsiteUri(Text, out var uri))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri.ToString(),
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore open failures; clipboard entry still remains usable as text.
        }
    }

    public async Task PopulateWebsiteMetadataAsync()
    {
        if (!TryGetWebsiteUri(Text, out var uri))
        {
            IsWebSite = false;
            WebsiteTitle = string.Empty;
            WebsiteDescription = string.Empty;
            WebsiteHost = string.Empty;
            return;
        }

        IsWebSite = true;
        WebsiteHost = uri.Host;

        try
        {
            var html = await MetadataClient.GetStringAsync(uri);

            var title = ExtractTitle(html);
            if (!string.IsNullOrWhiteSpace(title))
                WebsiteTitle = title;

            var description = ExtractDescription(html);
            if (!string.IsNullOrWhiteSpace(description))
                WebsiteDescription = description;
        }
        catch
        {
            // Keep URL-only display when metadata fetch fails.
        }
    }

    private static string ExtractTitle(string html)
    {
        var match = Regex.Match(html, "<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!match.Success)
            return string.Empty;

        return NormalizeHtmlText(match.Groups[1].Value);
    }

    private static string ExtractDescription(string html)
    {
        var metaTags = Regex.Matches(html, "<meta\\s+[^>]*>", RegexOptions.IgnoreCase);

        foreach (Match tag in metaTags)
        {
            var name = GetAttribute(tag.Value, "name");
            var property = GetAttribute(tag.Value, "property");
            var content = GetAttribute(tag.Value, "content");

            if (string.IsNullOrWhiteSpace(content))
                continue;

            if (string.Equals(name, "description", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(property, "og:description", StringComparison.OrdinalIgnoreCase))
                return NormalizeHtmlText(content);
        }

        return string.Empty;
    }

    private static string GetAttribute(string tag, string attributeName)
    {
        var match = Regex.Match(
            tag,
            $"{attributeName}\\s*=\\s*(['\"])(.*?)\\1",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        return match.Success ? match.Groups[2].Value : string.Empty;
    }

    private static string NormalizeHtmlText(string value)
    {
        var decoded = WebUtility.HtmlDecode(value);
        return Regex.Replace(decoded, "\\s+", " ").Trim();
    }

    private static bool TryGetWebsiteUri(string? text, out Uri uri)
    {
        uri = null!;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (!Uri.TryCreate(text.Trim(), UriKind.Absolute, out var parsed))
            return false;

        if (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps)
            return false;

        uri = parsed;
        return true;
    }
}