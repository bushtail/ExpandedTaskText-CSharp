using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace ExpandedTaskText.Models;

public record QuestInfo
{
    [JsonPropertyName("wikiLink")]
    public required string WikiLink { get; init; }
    
    [JsonPropertyName("id")]
    public required MongoId Id { get; init; }
    
    [JsonPropertyName("kappaRequired")]
    public required bool KappaRequired  { get; init; }
    
    [JsonPropertyName("lightkeeperRequired")]
    public required bool LightkeeperRequired  { get; init; }
    
    [JsonPropertyName("objectives")]
    public required List<QuestObjectives>  QuestObjectives { get; init; }
}

public record QuestObjectives
{
    [JsonPropertyName("id")]
    public MongoId Id { get; init; }
    
    [JsonPropertyName("requiredKeys")]
    public List<List<RequiredKeys>?>? RequiredKeys { get; init; }
}

public record RequiredKeys
{
    [JsonPropertyName("id")]
    public required MongoId Id { get; init; }
    
    [JsonPropertyName("name")]
    public required string Name { get; init; }
}