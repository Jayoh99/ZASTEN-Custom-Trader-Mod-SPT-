using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services.Mod;
using SPTarkov.Server.Core.Utils;
using System.Reflection;
using WTTServerCommonLib.Services;
using Zasten;
using Path = System.IO.Path;

namespace Zasten;

// This record holds the various properties for your mod
public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.zasten.trader";
    public override string Name { get; init; } = "Zasten";
    public override string Author { get; init; } = "Jayoh";
    public override List<string>? Contributors { get; init; } = ["Jayoh"];
    public override SemanticVersioning.Version Version { get; init; } = new("2.1.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.8");
    public override List<string>? Incompatibilities { get; init; } = null;
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; } = "https://github.com/sp-tarkov/server-mod-examples";
    public override bool? IsBundleMod { get; init; } = false;
    public override string? License { get; init; } = "MIT";
}

/// <summary>
/// Feel free to use this as a base for your mod
/// </summary>
[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 2)]
public class AddTraderWithAssortJson(
    ModHelper modHelper,
    ImageRouter imageRouter,
    ConfigServer configServer,
    TimeUtil timeUtil,
    WTTServerCommonLib.WTTServerCommonLib wttCommon,
    AddCustomTraderHelper AddCustomTraderHelper // This is a custom class we add for this mod, we made it injectable so it can be accessed like other classes here
)
    : IOnLoad
{
    private readonly TraderConfig _traderConfig = configServer.GetConfig<TraderConfig>();
    private readonly RagfairConfig _ragfairConfig = configServer.GetConfig<RagfairConfig>();



    public async Task OnLoad()
    {

        // A path to the mods files we use below
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        // A relative path to the trader icon to show
        var traderImagePath = Path.Combine(pathToMod, "data/zasten.png");
        // The base json containing trader settings we will add to the server
        var traderBase = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "data/base.json");
        // Create a helper class and use it to register our traders image/icon + set its stock refresh time
        imageRouter.AddRoute(traderBase.Avatar.Replace(".png", ""), traderImagePath);
        AddCustomTraderHelper.SetTraderUpdateTime(_traderConfig, traderBase, timeUtil.GetHoursAsSeconds(1), timeUtil.GetHoursAsSeconds(2));
        // Add our trader to the config file, this lets it be seen by the flea market
        _ragfairConfig.Traders.TryAdd(traderBase.Id, true);
        // An 'assort' is the term used to describe the offers a trader sells, it has 3 parts to an assort
        AddCustomTraderHelper.AddTraderWithEmptyAssortToDb(traderBase);
        // Add localisation text for our trader to the database so it shows to people playing in different languages
        AddCustomTraderHelper.AddTraderToLocales(traderBase, "Zasten", "an underground black market trader with ties to smugglers and ex-contractors. He deals in hard-to-find weapons and gear, favoring trusted operators over clean money.");
        // Get the assort data from JSON
        var assort = modHelper.GetJsonDataFromFile<TraderAssort>(pathToMod, "data/assort.json");
        // Save the data we loaded above into the trader we've made
        AddCustomTraderHelper.OverwriteTraderAssort(traderBase.Id, assort);
        // Send back a success to the server to say our trader is good to go
        Assembly assembly = Assembly.GetExecutingAssembly();
        // Use WTT-CommonLib services
        await wttCommon.CustomQuestService.CreateCustomQuests(assembly);
        await wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly);
        await wttCommon.CustomWeaponPresetService.CreateCustomWeaponPresets(assembly);
    }
}
