using SPTarkov.Server.Core.Models.Common;

namespace ExpandedTaskText.Models;

public record GunsmithInfo
{
    public required string QuestName { get; init; }
    public required List<MongoId> RequiredParts { get; init; }
}