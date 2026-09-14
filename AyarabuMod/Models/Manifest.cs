using System.Text.Json.Serialization;

namespace AyarabuMod;

/// <summary>可选的翻译哈希清单；缺失时仍可下载独立字典。</summary>
public sealed class Manifest
{
    /// <summary>人物名字典的规范化 MD5。</summary>
    [JsonPropertyName("names")]
    public string? Names { get; set; }

    /// <summary>七位剧情 ID 到字典哈希的映射。</summary>
    [JsonPropertyName("adventures")]
    public Dictionary<string, string>? Adventures { get; set; }
}
