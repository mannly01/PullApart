using CMS.UI;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.Text.RegularExpressions;
using MelonLoader;
using System;
using System.IO;
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
        public const string Version = "1.0.2";
        public const string DownloadLink = null;
        public const string MelonGameCompany = "Red Dot Games";
        public const string MelonGameName = "Car Mechanic Simulator 2021";
    }

    /// <summary>
    /// Create a "PullApart.cfg" file in the Mods folder.
    /// </summary>
    public class PullApartConfigFile
    {
        /// <summary>
        /// Settings Category
        /// </summary>
        private const string SettingsCatName = "PullApartSettings";
        private readonly MelonPreferences_Category _settings;
        /// <summary>
        /// User setting for the key to pull apart all groups in the player inventory.
        /// </summary>
        public KeyCode PullApartGroups => _pullApartGroups.Value;
        private readonly MelonPreferences_Entry<KeyCode> _pullApartGroups;
        /// <summary>
        /// User setting to automatically fix broken pulled apart parts from version 1.0.0.
        /// </summary>
        public bool FixBrokenParts => _fixBrokenParts.Value;
        private MelonPreferences_Entry<bool> _fixBrokenParts;

        public PullApartConfigFile()
        {
            _settings = MelonPreferences.CreateCategory(SettingsCatName);
            _settings.SetFilePath("Mods/PullApart.cfg");
            _pullApartGroups = _settings.CreateEntry(nameof(PullApartGroups), KeyCode.F4,
                description: "Press this Key to pull apart all the groups in your inventory.");
            _fixBrokenParts = _settings.CreateEntry(nameof(FixBrokenParts), false,
                description: "Set to true to automatically fix parts that were broken in version 1.0.0." + Environment.NewLine + "THIS SHOULD ONLY NEED TO BE DONE ONCE.");
            if (!File.Exists($"{Directory.GetCurrentDirectory()}\\Mods\\PullApart.cfg"))
            {
                _settings.SaveToFile();
            }
        }

        public void ResetFixBrokenParts()
        {
            _fixBrokenParts.Value = false;
            _settings.SaveToFile();
        }
    }

    public class PullApart : MelonMod
    {
        /// <summary>
        /// Reference to Settings file.
        /// </summary>
        private PullApartConfigFile _configFile;

        /// <summary>
        /// Global reference to the current scene.
        /// </summary>
        private string _currentScene = string.Empty;

        public override void OnInitializeMelon()
        {
            // Tell the user that we're loading the Settings.
            MelonLogger.Msg("Loading Settings...");
            _configFile = new PullApartConfigFile();
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            // Save a reference to the current scene.
            _currentScene = sceneName.ToLower();
            if (_currentScene.Equals("christmas") ||
                _currentScene.Equals("easter") ||
                _currentScene.Equals("halloween"))
            {
                _currentScene = "garage";
            }
        }

        public override void OnUpdate()
        {
            // Check if the user pressed the PullApartGroups Key in Settings.
            if (Input.GetKeyDown(_configFile.PullApartGroups))
            {
                // Check if the user is currently using the Seach box.
                if (!CheckIfInputIsFocused())
                {
                    // Check that the user is in the Garage and
                    // the setting to fix parts is turned on.
                    if (_currentScene.Equals("garage") &&
                        _configFile.FixBrokenParts)
                    {
                        FixBrokenParts();
                    }

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
            // Look for all inputfields in the game.
            var inputFields = UnityEngine.Object.FindObjectsOfType<InputField>();
            foreach (var inputField in inputFields)
            {
                // If an inputfield is found,
                // see if it has the user's focus.
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
        /// Helper method to return the type of GroupItem.
        /// </summary>
        /// <param name="groupID">ID of the GroupItem</param>
        /// <returns>(Enum) The type of SpecialGroup that the GroupItem is.</returns>
        private SpecialGroup GetSpecialGroup(string groupID)
        {
            var gameInventory = Singleton<GameInventory>.Instance;
            var itemProperty = gameInventory.GetItemProperty(groupID);
            return itemProperty.SpecialGroup;
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
                                    // Check if the group is an engine.
                                    // There are unique properties the game applies to an engine.
                                    if (GetSpecialGroup(group.ID) == SpecialGroup.Engine)
                                    {
                                        // The game copies the part ID to NormalID
                                        // and then appends (i) to the ID.
                                        if (!string.IsNullOrWhiteSpace(item.NormalID))
                                        {
                                            // Don't add Oil Dipstick, Oil Drain Plug and Oil Fill Plug.
                                            if (item.NormalID.Equals("korekOleju_1") ||
                                                item.NormalID.Equals("bagnet_1") ||
                                                item.NormalID.Equals("korek_spustowy_1"))
                                            {
                                                // Still de-increment the counter for later logic.
                                                itemCount--;
                                                continue;
                                            }
                                            // Put the proper ID back and remove the NormalID.
                                            item.ID = item.NormalID;
                                            item.NormalID = "";
                                        }
                                    }
                                    // Add the item to the inventory and
                                    // de-increment the counter.
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

        private void FixBrokenParts()
        {
            // Setup a count of deleted items.
            int deletedCount = 0;
            // Setup a temporary list for the deleted items.
            List<Item> deletedItemQueue = new List<Item>();

            // Setup a count of fixed items to show the user.
            int invItemCount = 0;

            // Go through all the items in the inventory and
            // change their IDs if they are wrong.
            var inventory = Singleton<Inventory>.Instance;
            if (inventory.items.Count > 0)
            {
                foreach (var item in inventory.items)
                {
                    // Delete the Oil Dipstick, Oil Drain Plug and Oil Fill Plug.
                    if (item.ID.Contains("korekOleju_1") ||
                        item.ID.Contains("bagnet_1") ||
                        item.ID.Contains("korek_spustowy_1"))
                    {
                        deletedItemQueue.Add(item);
                        deletedCount++;
                        continue;
                    }
                    // Look for a (*) that was appended to the ID and
                    // move the NormalID back to the ID.
                    var regex = new Regex(@"\([0-9]+\)");
                    if (regex.IsMatch(item.ID))
                    {
                        if (!string.IsNullOrWhiteSpace(item.NormalID))
                        {
                            item.ID = item.NormalID;
                            item.NormalID = "";
                            invItemCount++;
                        }
                    }
                }
                // If any of the items above need to be deleted,
                // do that now.
                if (deletedItemQueue.Count > 0)
                {
                    foreach (var deletedItem in deletedItemQueue)
                    {
                        inventory.items.Remove(deletedItem);
                    }
                    deletedItemQueue.Clear();
                }
            }
            
            // Go through each Warehouse and fix any items that are wrong.
            // The easiest way to search all the warehouses is to select them.
            // Setup a count of fixed items to show the user.
            int wareItemCount = 0;
            int prevWareItemCount = 0;
            // A reference to the Warehouse.
            var warehouse = GameManager.Instance.Warehouse;
            // Save the currently selected warehouse to return to.
            int selectedWarehouse = warehouse.SelectedOption;
            for (int i = 0; i < Warehouse.amountOfUnlockedWarehouses; i++)
            {
                // Save the previous item count to show the user later.
                prevWareItemCount = wareItemCount;
                // Select the warehouse.
                warehouse.SelectedOption = i;
                // Get all the items and groups, although we are only using items.
                var wareItems = warehouse.GetAllItemsAndGroups();
                if (wareItems.Count > 0)
                {
                    // Loop through all the BaseItems.
                    foreach (var baseItem in wareItems)
                    {
                        // Try to case the BaseItem to Item and
                        // only work on the non-null Items.
                        if (baseItem.TryCast<Item>() != null)
                        {
                            var wareItem = baseItem.TryCast<Item>();
                            // Delete the Oil Dipstick, Oil Drain Plug and Oil Fill Plug.
                            if (wareItem.ID.Contains("korekOleju_1") ||
                                wareItem.ID.Contains("bagnet_1") ||
                                wareItem.ID.Contains("korek_spustowy_1"))
                            {
                                deletedItemQueue.Add(wareItem);
                                deletedCount++;
                                continue;
                            }
                            // Look for a (*) that was appended to the ID and
                            // move the NormalID back to the ID.
                            var regex = new Regex(@"\([0-9]+\)");
                            if (regex.IsMatch(wareItem.ID))
                            {
                                if (!string.IsNullOrWhiteSpace(wareItem.NormalID))
                                {
                                    wareItem.ID = wareItem.NormalID;
                                    wareItem.NormalID = "";
                                    wareItemCount++;
                                }
                            }
                        }
                    }
                    // If any of the items above need to be deleted,
                    // do that now.
                    if (deletedItemQueue.Count > 0)
                    {
                        foreach (var deletedItem in deletedItemQueue)
                        {
                            warehouse.Delete(deletedItem);
                        }
                        deletedItemQueue.Clear();
                    }
                }
                // Only six warehouses are shown in the game, so don't confuse users.
                if (i < 6)
                {
                    MelonLogger.Msg($"Warehouse {i + 1} Items Fixed: {wareItemCount - prevWareItemCount}");
                }
            }
            warehouse.SelectedOption = selectedWarehouse;
            MelonLogger.Msg($"Fixed Items: Inventory: {invItemCount} Warehouse: {wareItemCount} Deleted: {deletedCount}");
            // If we fixed parts, tell the user and then turn off this function.
            if (invItemCount > 0 ||
                wareItemCount > 0 ||
                deletedCount > 0)
            {
                Singleton<UIManager>.Instance.ShowPopup(BuildInfo.Name, "All Items have been fixed.", PopupType.Normal);
                Singleton<UIManager>.Instance.ShowPopup(BuildInfo.Name, "Setting FixBrokenParts to false.", PopupType.Normal);
                _configFile.ResetFixBrokenParts();
            }
        }
    }
}
