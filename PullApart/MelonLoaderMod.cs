using CMS.UI;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace PullApart
{
    /// <summary>
    /// Information for MelonLoader.
    /// </summary>
    public static class BuildInfo
    {
        public const string Name = "Pull Apart";
        public const string Description = "Mod to automatically pull apart all groups in the player inventory.";
        public const string Author = "mannly82";
        public const string Company = "The Mann Design";
        public const string Version = "1.0.0";
        public const string DownloadLink = null;
        public const string MelonGameCompany = "Red Dot Games";
        public const string MelonGameName = "Car Mechanic Simulator 2021";
    }

    /// <summary>
    /// Create a "PullApart.cfg" file in the Mods folder.
    /// </summary>
    public class ConfigFile
    {
        /// <summary>
        /// Settings Category
        /// </summary>
        private const string SettingsCatName = "Settings";
        private readonly MelonPreferences_Category _settings;
        /// <summary>
        /// User setting for the key to pull apart all groups in the player inventory.
        /// </summary>
        public KeyCode PullApartGroups => _pullApartGroups.Value;
        private readonly MelonPreferences_Entry<KeyCode> _pullApartGroups;

        public ConfigFile()
        {
            _settings = MelonPreferences.CreateCategory(SettingsCatName);
            _settings.SetFilePath("Mods/PullApart.cfg");
            _pullApartGroups = _settings.CreateEntry(nameof(PullApartGroups), KeyCode.F4,
                description: "Press this Key to pull apart all the groups in your inventory.");
        }
    }

    public class PullApart : MelonMod
    {
        /// <summary>
        /// Reference to Settings file.
        /// </summary>
        private ConfigFile _configFile;

        public override void OnInitializeMelon()
        {
            // Tell the user that we're loading the Settings.
            MelonLogger.Msg("Loading Settings...");
            _configFile = new ConfigFile();
        }

        public override void OnUpdate()
        {
            // Check if the user pressed the PullApartGroups Key in Settings.
            if (Input.GetKeyDown(_configFile.PullApartGroups))
            {
                // Check if the user is currently using the Seach box.
                if (!CheckIfInputIsFocused())
                {
                    PullApartAllGroups();
                }
            }
        }

        /// <summary>
        /// If the user is using the Search box,
        /// the mod should do nothing.
        /// </summary>
        /// <returns>(bool) True if the Search/Input Field is being used.</returns>
        private bool CheckIfInputIsFocused()
        {
            var inputFields = UnityEngine.Object.FindObjectsOfType<InputField>();
            foreach (var inputField in inputFields)
            {
                if (inputField != null)
                {
                    if (inputField.isFocused)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Method to pull apart all the groups in the user's inventory.
        /// </summary>
        private void PullApartAllGroups()
        {
            // Check if a window is open and show the user a message to close it first.
            if (Singleton<WindowManager>.Instance.activeWindows.count > 0)
            {
                Singleton<UIManager>.Instance.ShowPopup(BuildInfo.Name, $"Please close any open windows first.", PopupType.Normal);
            }
            else
            {
                // Get a reference to the user inventory.
                var inventory = Singleton<Inventory>.Instance;
                if (inventory != null)
                {
                    // Get all the groups in the user's inventory.
                    var groups = inventory.GetGroups();
                    // Setup a temporary count of the inventory groups.
                    int groupCount = groups.Count;
                    if (groupCount > 0)
                    {
                        // Loop through each group and pull them apart.
                        for (int i = 0; i < groupCount; i++)
                        {
                            // Always get a reference to the first group.
                            var group = groups[0];
                            // Get a list of the items in the group.
                            var groupItems = group.ItemList;
                            // Get a count of the items in the group.
                            var itemCount = groupItems.Count;
                            if (itemCount > 0)
                            {
                                // Loop through each item and add it to the user's inventory.
                                // Also, de-increment the counter.
                                foreach (var item in groupItems)
                                {
                                    inventory.items.Add(item);
                                    itemCount--;
                                }
                            }
                            // If the counter is 0, all the items have been pulled apart,
                            // delete the group from the user's inventory.
                            if (itemCount == 0)
                            {
                                inventory.DeleteGroup(group.UID);
                            }
                        }
                    }
                    // If all the groups have been pulled apart, show the user the count of groups pulled apart.
                    if (groups.Count == 0)
                    {
                        Singleton<UIManager>.Instance.ShowPopup(BuildInfo.Name, $"{groupCount} Groups Pulled Apart.", PopupType.Normal);
                    }
                }
            }
        }
    }
}
