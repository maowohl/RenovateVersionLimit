using System.Text.Json.Serialization;

public class RenovateFile
{
    [JsonPropertyName("$schema")]
    public string Schema { get; set; } = "https://docs.renovatebot.com/renovate-schema.json";
    [JsonPropertyName("packageRules")]
    public List<PackageRule> PackageRules { get; set; } = new();

    public class PackageRule
    {
        [JsonPropertyName("matchPackageNames")]
        public List<string> MatchPackageNames { get; set; } = new();
        [JsonPropertyName("allowedVersions")]
        public string AllowedVersions { get; set; } = string.Empty;
    }
}