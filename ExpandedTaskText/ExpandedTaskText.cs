using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Json;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace ExpandedTaskText;

public record EttMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.cj.ett";
    public override string Name { get; init; } = "Expanded Task Text";
    public override string Author { get; init; } = "Cj";
    public override List<string>? Contributors { get; init; }
    public override Version Version { get; init; } = new("2.0.0");
    public override Range SptVersion { get; init; } = new("~4.0");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string License { get; init; } = "MIT";
    
    public static readonly string ResourcesDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Resources");
}

// Load after EVERYTHING so all custom quests exist
[Injectable(TypePriority = int.MaxValue)]
public class ExpandedTaskText(
    ISptLogger<ExpandedTaskText> logger,
    DatabaseService databaseService,
    LocaleService localeService,
    FileUtil fileUtil,
    JsonUtil jsonUtil
    ) : IOnLoad
{
    private List<QuestInfo>? _questInfos;
    private readonly Dictionary<MongoId, string> _questDescriptionCache = [];

    private const string CollectorRequired = "This quest is required for Collector\n";
    private const string LightkeeperRequired = "This quest is required for Lightkeeper\n";
    
    public async Task OnLoad()
    {
        var sw = Stopwatch.StartNew();
        logger.Info("[Expanded Task Text] Loading please wait...");
        
        var questInfoText = await fileUtil.ReadFileAsync(Path.Combine(EttMetadata.ResourcesDirectory, "QuestInfo.json"));
        _questInfos = jsonUtil.Deserialize<List<QuestInfo>>(questInfoText);
        
        await UpdateAllTaskText();
        
        logger.Success($"[Expanded Task Text] Completed loading in {(sw.ElapsedMilliseconds / 1000f):F2} seconds.");
    }

    private Task UpdateAllTaskText()
    {
        var locales = localeService.GetLocaleDb();
        foreach (var info in _questInfos ?? [])
        {
            if (!locales.TryGetValue($"{info.Id.ToString()} description", out var description))
            {
                logger.Error($"[Expanded Task Text] Could not find quest description for `{info.Id.ToString()}`");
                continue;
            }

            foreach (var (_, globalLocales) in databaseService.GetLocales().Global)
            {
                UpdateTaskText(info, globalLocales, description);
            }
        }
        
        return Task.CompletedTask;
    }
    
    private void UpdateTaskText(QuestInfo info, LazyLoad<Dictionary<string, string>> locales, string originalDescription)
    {
        if (!_questDescriptionCache.TryGetValue(info.Id, out var newDescription))
        {
            _questDescriptionCache[info.Id] = BuildNewDescription(info, originalDescription);
        }
        
        locales.AddTransformer(transformer =>
        {
            transformer![$"{info.Id.ToString()} description"] = newDescription;
            
            return transformer;
        });
    }

    private string BuildNewDescription(QuestInfo info, string originalDescription)
    {
        var sb = new StringBuilder();
        
        if (info.KappaRequired)
        {
            sb.Append(CollectorRequired);
        }

        if (info.LightkeeperRequired)
        {
            sb.Append(LightkeeperRequired);
        }

        sb.Append(GetKeyInfoForQuest(info));
        sb.Append("\n\n");
        sb.Append(GetNextQuests(info.Id));
        sb.Append("\n\n");
        
        sb.Append(originalDescription);
        
        return sb.ToString();
    }

    private string GetNextQuests(MongoId currentQuestId)
    {
        var quests = databaseService.GetQuests();
        var locales = localeService.GetLocaleDb();
        
        var result = new List<string>();
        
        foreach (var (qid, quest) in quests)
        {
            var availableForStart = quest.Conditions.AvailableForStart;
            
            if (availableForStart is null)
            {
                continue;
            }

            foreach (var condition in availableForStart)
            {
                if (condition.Target?.Item is null)
                {
                    continue;
                }
                
                if (condition.ConditionType == "Quest" && condition.Target.Item == currentQuestId.ToString())
                {
                    var nextQuestName = locales[$"{qid} name"];
                    result.Add($"\n\t{nextQuestName}");
                }
            }
        }
        
        var sb = new StringBuilder();
        
        sb.Append(result.Count > 0 ? "Leads to:": "Leads to: Nothing");
        sb.Append(string.Join(", ", result));
        
        return sb.ToString();
    }

    private string GetKeyInfoForQuest(QuestInfo info)
    {
        var result = new List<string>();
        
        foreach (var obj in info.QuestObjectives)
        {
            if (obj.RequiredKeys is null)
            {
                continue;
            }
            
            foreach (var list in obj.RequiredKeys)
            {
                if (list is null)
                {
                    continue;
                }
                
                foreach (var key in list)
                {
                    var keyName = GetLocale($"{key.Id.ToString()} Name");
                    if (!string.IsNullOrEmpty(keyName))
                    {
                        result.Add($"\n\t{keyName}");
                    }
                }
            }
        }
        
        var sb = new StringBuilder();

        if (result.Count > 0)
        {
            sb.Append("Requires Key(s):");
            sb.Append(string.Join(", ", result));
        }
        else
        {
            sb.Append("No keys required.");
        }
        
        return sb.ToString();
    }

    private string GetLocale(string key)
    {
        var locales = localeService.GetLocaleDb();

        if (!locales.TryGetValue(key, out var locale))
        {
            logger.Error($"[Expanded Task Text] Could not find locale for `{key}`]");
            
            return string.Empty;
        }
        
        return locale;
    }
}