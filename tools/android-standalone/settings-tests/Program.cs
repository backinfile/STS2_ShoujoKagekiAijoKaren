using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Saves;

var options = new JsonSerializerOptions();
options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
var settings = JsonSerializer.Deserialize<SettingsSave>(File.ReadAllText(args[0], Encoding.UTF8), options)
    ?? throw new Exception("Settings were not deserialized");
if (settings.SchemaVersion != 5 || settings.FpsLimit != 60 || settings.Language != "zhs"
    || settings.ModSettings is null || !settings.ModSettings.PlayerAgreedToModLoading
    || settings.ModSettings.ModList.Count != 0 || settings.VolumeMaster <= 0
    || settings.WindowSize.X <= 0 || settings.WindowSize.Y <= 0)
    throw new Exception("Unexpected initial game settings");
Console.WriteLine("PASS: v0.107.1 SettingsSave schema 5, enabled Mods, empty mod_list, language, audio and window defaults");
