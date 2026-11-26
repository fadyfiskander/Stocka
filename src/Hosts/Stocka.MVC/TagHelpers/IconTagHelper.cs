using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Stocka.MVC.TagHelpers;

[HtmlTargetElement("icon")]
public sealed partial class IconTagHelper : TagHelper
{
    private static readonly ConcurrentDictionary<string, string?> Cache = new();
    private readonly IWebHostEnvironment _env;

    public IconTagHelper(IWebHostEnvironment env) => _env = env;

    [HtmlAttributeName("name")]
    public string Name { get; set; } = "";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var inner = Cache.GetOrAdd(Name, Load);

        output.TagName = "svg";
        output.TagMode = TagMode.StartTagAndEndTag;

        var existing = output.Attributes["class"]?.Value?.ToString();
        output.Attributes.SetAttribute("class", string.IsNullOrWhiteSpace(existing) ? "icon" : $"icon {existing}");

        output.Attributes.SetAttribute("viewBox", "0 0 24 24");
        output.Attributes.SetAttribute("aria-hidden", "true");
        output.Attributes.SetAttribute("focusable", "false");

        if (inner is null)
            output.Attributes.SetAttribute("data-icon-missing", Name);
        else
            output.Content.SetHtmlContent(inner);
    }

    private string? Load(string name)
    {
        if (!NameRegex().IsMatch(name)) return null;

        var file = _env.WebRootFileProvider.GetFileInfo($"icons/{name}.svg");
        if (!file.Exists) return null;

        using var reader = new StreamReader(file.CreateReadStream());
        var m = InnerRegex().Match(reader.ReadToEnd());
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    [GeneratedRegex("^[a-z0-9-]+$")]
    private static partial Regex NameRegex();

    [GeneratedRegex(@"<svg[^>]*>(.*)</svg>", RegexOptions.Singleline)]
    private static partial Regex InnerRegex();
}
