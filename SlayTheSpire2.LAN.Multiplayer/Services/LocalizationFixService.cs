using System.Text.Json;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;

namespace SlayTheSpire2.LAN.Multiplayer.Services
{
    /// <summary>
    /// The game only loads mod localization JSON that has been packed into a Godot .pck
    /// (resolved via res://&lt;mod id&gt;/localization/...). This mod ships loose JSON files
    /// instead (has_pck=false), so ModManager.GetModdedLocTables never finds them and every
    /// lookup of our keys throws a LocException. Work around it by merging our JSON directly
    /// into the live LocTable instances using the game's own LocTable.MergeWith.
    /// </summary>
    internal static class LocalizationFixService
    {
        private static readonly string ModDir =
            Path.GetDirectoryName(typeof(LocalizationFixService).Assembly.Location)!;

        public static void MergeAll()
        {
            if (LocManager.Instance == null)
                return;

            MergeAll(LocManager.Instance, LocManager.Instance.Language);
        }

        public static void MergeAll(LocManager locManager, string? language)
        {
            try
            {
                if (string.IsNullOrEmpty(language) || !MergeLanguage(locManager, language))
                {
                    MergeLanguage(locManager, "eng");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[LAN Multiplayer] LocalizationFixService.MergeAll failed: {ex}");
            }
        }

        private static bool MergeLanguage(LocManager locManager, string language)
        {
            var dir = Path.Combine(ModDir, "localization", language);
            if (!Directory.Exists(dir))
            {
                Log.Warn($"[LAN Multiplayer] Localization dir not found: {dir}");
                return false;
            }

            foreach (var file in Directory.GetFiles(dir, "*.json"))
            {
                var tableName = Path.GetFileNameWithoutExtension(file);
                try
                {
                    var table = locManager.GetTable(tableName);
                    var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file));
                    if (translations != null)
                    {
                        table.MergeWith(translations);
                        Log.Info($"[LAN Multiplayer] Merged {translations.Count} keys into table={tableName} from {file}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[LAN Multiplayer] Failed to merge localization file {file}: {ex}");
                }
            }

            return true;
        }
    }
}
