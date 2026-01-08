using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EquinoxsModUtils;
using EquinoxsModUtils.Additions;
using HarmonyLib;
using UnityEngine;

using TechCategory = Unlock.TechCategory;
using CoreType = ResearchCoreDefinition.CoreType;
using ResearchTier = TechTreeState.ResearchTier;
using TechtonicaFramework.TechTree;

namespace WormholeChests
{
    public static class ChestInstanceExtensions
    {
        public static bool IsNull(this ChestInstance chest)
        {
            return chest.commonInfo.instanceId == 0;
        }
    }

    [BepInPlugin(MyGUID, PluginName, VersionString)]
    [BepInDependency("com.equinox.EquinoxsModUtils")]
    [BepInDependency("com.equinox.EMUAdditions")]
    [BepInDependency("com.certifired.TechtonicaFramework")]
    public class WormholeChestsPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.equinox.WormholeChests";
        private const string PluginName = "WormholeChests";
        private const string VersionString = "3.1.1";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;
        public static bool isUnlockActive = false;  // Global flag - when false, ALL patches do nothing

        public static Texture2D wormholeTexture;

        // Custom sprites dictionary to cache loaded sprites
        public static Dictionary<string, Sprite> customSprites = new Dictionary<string, Sprite>();

        public static ConfigEntry<bool> freeWormholeChests;
        public static ConfigEntry<bool> useRelativePositioning;
        public static ConfigEntry<float> relativeXPercent;
        public static ConfigEntry<float> relativeYPercent;
        public static ConfigEntry<float> relativeButtonXPercent;
        public static ConfigEntry<float> channelBoxXOffset;
        public static ConfigEntry<float> channelBoxYOffset;
        public static ConfigEntry<float> channelBoxWidth;
        public static ConfigEntry<float> createButtonXOffset;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");

            Harmony.PatchAll();

            // Use EMU 6.1.3 Action-based events
            EMU.Events.GameDefinesLoaded += OnGameDefinesLoaded;
            EMU.Events.TechTreeStateLoaded += OnTechTreeStateLoaded;
            EMU.Events.MachineManagerLoaded += OnMachineManagerLoaded;

            CreateConfigEntries();
            ApplyPatches();
            ChestGUI.LoadImages();

            // Load custom icons before registering unlocks
            LoadCustomIcons();

            // Use loaded custom sprite if available, otherwise fall back to generated texture
            Sprite sprite = customSprites.ContainsKey("wormhole")
                ? customSprites["wormhole"]
                : Sprite.Create(wormholeTexture,
                    new Rect(0f, 0f, wormholeTexture.width, wormholeTexture.height),
                    new Vector2(0f, 0f), 512f);

            // Add new unlock using EMUAdditions (EMU 6.1.3 compatible - no numScansNeeded)
            EMUAdditions.AddNewUnlock(new NewUnlockDetails
            {
                category = ModdedTabModule.ModdedCategory,
                coreTypeNeeded = CoreType.Blue,
                coreCountNeeded = 2000,
                description = "Allow chests on the same channel to share inventories.",
                displayName = "Wormhole Chests",
                requiredTier = ResearchTier.Tier2,
                treePosition = 0,
                sprite = sprite
            });

            Log.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
        }

        private void OnGUI()
        {
            if (ChestGUI.shouldShowGUI)
            {
                try
                {
                    ChestGUI.DrawChestGUI();
                }
                catch (Exception ex)
                {
                    Log.LogError($"OnGUI DrawChestGUI failed: {ex.Message}\n{ex.StackTrace}");
                    ChestGUI.shouldShowGUI = false;
                }
            }
        }

        private void OnGameDefinesLoaded()
        {
            Unlock threshing = EMU.Unlocks.GetUnlockByName("Core Boost (Threshing)");
            Unlock mining = EMU.Unlocks.GetUnlockByName("Core Boost (Mining)");
            EMU.Unlocks.UpdateUnlockTier("Wormhole Chests", threshing.requiredTier);
            EMU.Unlocks.UpdateUnlockTreePosition("Wormhole Chests", mining.treePosition);

            // Apply custom sprites to unlocks and resources after game defines are loaded
            ApplyCustomSprites();
        }

        private void OnTechTreeStateLoaded()
        {
            // Check if unlock is active - this determines if mod does ANYTHING
            Unlock wormholeUnlock = EMU.Unlocks.GetUnlockByName("Wormhole Chests");
            if (wormholeUnlock != null && TechTreeState.instance != null)
            {
                isUnlockActive = TechTreeState.instance.IsUnlockActive(wormholeUnlock.uniqueId);
                Log.LogInfo($"Wormhole Chests unlock active: {isUnlockActive}");
            }
            else
            {
                isUnlockActive = false;
                Log.LogInfo("Wormhole Chests: Could not check unlock status, disabling mod functionality");
            }
        }

        private void OnMachineManagerLoaded()
        {
            // Skip if unlock not researched
            if (!isUnlockActive)
            {
                Log.LogInfo("WormholeChests: Unlock not active, skipping data load");
                return;
            }

            try
            {
                WormholeManager.LoadData(SaveState.instance.metadata.worldName);
                Log.LogInfo("WormholeChests Loaded");

                MachineInstanceList<ChestInstance, ChestDefinition> machineList =
                    MachineManager.instance.GetMachineList<ChestInstance, ChestDefinition>(MachineTypeEnum.Chest);

                if (machineList == null || machineList.myArray == null)
                {
                    Log.LogWarning("Could not get chest machine list");
                    return;
                }

                for (int i = 0; i < machineList.myArray.Length; i++)
                {
                    ChestInstance chest = machineList.myArray[i];
                    uint instanceId = chest.commonInfo.instanceId;

                    if (instanceId == 0) continue; // Skip invalid entries

                    if (WormholeManager.chestChannelMap.ContainsKey(instanceId))
                    {
                        Inventory wormholeInventory = WormholeManager.GetInventoryForChest(instanceId);
                        if (wormholeInventory.numSlots > 0)
                        {
                            chest.commonInfo.inventories[0] = wormholeInventory;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"OnMachineManagerLoaded failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void CreateConfigEntries()
        {
            freeWormholeChests = Config.Bind("General", "Free Wormhole Chests", false,
                new ConfigDescription("Disables the cost of creating Wormhole Chests. Cheat, not recommended."));

            // Resolution-aware positioning (recommended)
            useRelativePositioning = Config.Bind("GUI Layout", "Use Relative Positioning", true,
                new ConfigDescription("When enabled, UI positions are calculated as percentage of screen size for proper scaling across all resolutions. Disable to use fixed pixel offsets instead."));
            relativeXPercent = Config.Bind("GUI Layout", "Relative X Position", 0.28f,
                new ConfigDescription("Horizontal position of channel input as percentage of screen width (0.0 = left, 0.5 = center, 1.0 = right). Default 0.28 places it left of storage title. Only used when Use Relative Positioning is enabled.",
                new AcceptableValueRange<float>(0f, 1f)));
            relativeYPercent = Config.Bind("GUI Layout", "Relative Y Position", 0.22f,
                new ConfigDescription("Vertical position as percentage of screen height (0.0 = top, 0.5 = center, 1.0 = bottom). Default 0.22 aligns with storage title area. Only used when Use Relative Positioning is enabled.",
                new AcceptableValueRange<float>(0f, 1f)));
            relativeButtonXPercent = Config.Bind("GUI Layout", "Relative Button X Position", 0.58f,
                new ConfigDescription("Horizontal position of Create/Link button as percentage of screen width. Default 0.58 places it right of storage title. Only used when Use Relative Positioning is enabled.",
                new AcceptableValueRange<float>(0f, 1f)));

            // Fixed pixel offsets (legacy, for manual fine-tuning)
            channelBoxXOffset = Config.Bind("GUI Layout (Fixed)", "Channel Box X Offset", 32f,
                new ConfigDescription("Fixed pixel offset from screen center. Only used when Use Relative Positioning is disabled."));
            channelBoxYOffset = Config.Bind("GUI Layout (Fixed)", "Channel Box Y Offset", -550f,
                new ConfigDescription("Fixed pixel offset from screen center. Only used when Use Relative Positioning is disabled. For ultrawide (3440x1440), try -550."));
            channelBoxWidth = Config.Bind("GUI Layout", "Channel Box Width", 240f,
                new ConfigDescription("Width of the Channel input box."));
            createButtonXOffset = Config.Bind("GUI Layout", "Create Button X Offset", 444f,
                new ConfigDescription("Horizontal offset of Create/Link button from the Channel box."));
        }

        private void ApplyPatches()
        {
            Harmony.CreateAndPatchAll(typeof(ChestDefinitionPatch));
            Harmony.CreateAndPatchAll(typeof(ChestInstancePatch));
            Harmony.CreateAndPatchAll(typeof(InventoryNavigatorPatch));
            Harmony.CreateAndPatchAll(typeof(SaveStatePatch));
        }

        /// <summary>
        /// Load custom icons from embedded resources with appropriate tinting
        /// </summary>
        private void LoadCustomIcons()
        {
            // Load wormhole icon with purple/magenta tint
            Sprite wormholeSprite = LoadEmbeddedSprite("wormhole.png", new Color(0.8f, 0.2f, 1f)); // Purple/magenta tint
            if (wormholeSprite != null)
            {
                customSprites["wormhole"] = wormholeSprite;
                Log.LogInfo("Loaded custom wormhole icon with purple tint");
            }
            else
            {
                Log.LogWarning("Failed to load wormhole.png embedded resource, using fallback icon");
            }
        }

        /// <summary>
        /// Apply custom sprites to unlocks and resources after game defines are loaded
        /// </summary>
        private void ApplyCustomSprites()
        {
            // Apply wormhole sprite to unlock
            if (customSprites.TryGetValue("wormhole", out Sprite wormholeSprite))
            {
                Unlock wormholeUnlock = EMU.Unlocks.GetUnlockByName("Wormhole Chests");
                if (wormholeUnlock != null)
                {
                    wormholeUnlock.sprite = wormholeSprite;
                    Log.LogInfo("Applied custom wormhole sprite to Wormhole Chests unlock");
                }

                // Also try to apply to any associated resource if it exists
                ResourceInfo wormholeResource = EMU.Resources.GetResourceInfoByName("Wormhole Chests");
                if (wormholeResource != null)
                {
                    // Use reflection to set the rawSprite field
                    var spriteField = typeof(ResourceInfo).GetField("rawSprite", BindingFlags.Public | BindingFlags.Instance);
                    if (spriteField != null)
                    {
                        spriteField.SetValue(wormholeResource, wormholeSprite);
                        Log.LogInfo("Applied custom wormhole sprite to Wormhole Chests resource");
                    }
                }
            }
        }

        /// <summary>
        /// Load sprite from embedded resource PNG file with optional tinting
        /// </summary>
        public static Sprite LoadEmbeddedSprite(string resourceName, Color? tintColor = null, int width = 256, int height = 256)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string fullName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith(resourceName));

                if (string.IsNullOrEmpty(fullName))
                {
                    Log.LogWarning($"Embedded resource not found: {resourceName}");
                    return null;
                }

                using (var stream = assembly.GetManifestResourceStream(fullName))
                {
                    if (stream == null) return null;

                    byte[] data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);

                    Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    tex.filterMode = FilterMode.Bilinear;

                    if (tex.LoadImage(data))
                    {
                        // Apply tint if specified
                        if (tintColor.HasValue)
                        {
                            TintTexture(tex, tintColor.Value);
                        }
                        tex.Apply();
                        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                        Log.LogInfo($"Loaded embedded sprite: {resourceName} ({tex.width}x{tex.height})" + (tintColor.HasValue ? " - tinted" : ""));
                        return sprite;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to load embedded sprite {resourceName}: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Tint a texture by multiplying each pixel's color with a tint color
        /// </summary>
        private static void TintTexture(Texture2D tex, Color tint)
        {
            Color[] pixels = tex.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                // Preserve alpha, tint RGB based on original brightness
                float brightness = (pixels[i].r + pixels[i].g + pixels[i].b) / 3f;
                pixels[i] = new Color(
                    tint.r * brightness,
                    tint.g * brightness,
                    tint.b * brightness,
                    pixels[i].a
                );
            }
            tex.SetPixels(pixels);
        }
    }

    public class Wormhole
    {
        public string channel;
        public Inventory inventory;

        public Wormhole()
        {
            inventory = new Inventory
            {
                myStacks = new ResourceStack[56],
                numSlots = 56
            };
        }

        public Wormhole(string serial)
        {
            inventory = new Inventory
            {
                myStacks = new ResourceStack[56],
                numSlots = 56
            };

            for (int i = 0; i < 56; i++)
            {
                inventory.myStacks[i] = ResourceStack.CreateEmptyStack();
            }

            string[] parts = serial.Split('|');
            channel = parts[0];

            for (int j = 1; j < parts.Length; j++)
            {
                string[] itemParts = parts[j].Split(',');
                string idStr = itemParts[0];
                string countStr = itemParts[1];

                if (idStr != "null" && countStr != "null")
                {
                    int id = int.Parse(idStr);
                    int count = int.Parse(countStr);
                    inventory.AddResources(id, count, true);
                }
            }
        }

        public string Serialise()
        {
            string result = channel;

            if (inventory.myStacks != null)
            {
                foreach (ResourceStack stack in inventory.myStacks)
                {
                    if (!stack.isEmpty)
                    {
                        result += $"|{stack.info.uniqueId},{stack.count}";
                    }
                }
            }
            else
            {
                result += "|null,null";
            }

            return result;
        }
    }

    public static class WormholeManager
    {
        public static Dictionary<string, Wormhole> wormholes = new Dictionary<string, Wormhole>();
        public static Dictionary<uint, string> chestChannelMap = new Dictionary<uint, string>();

        private static string dataFolder => Application.persistentDataPath + "/WormholeChests";

        public static void AddWormhole(Wormhole wormhole)
        {
            wormholes.Add(wormhole.channel, wormhole);
        }

        public static Wormhole GetWormhole(string channel)
        {
            if (!wormholes.TryGetValue(channel, out Wormhole wormhole))
            {
                WormholeChestsPlugin.Log.LogWarning($"GetWormhole: Channel '{channel}' not found in wormholes dictionary");
                return null;
            }
            return wormhole;
        }

        public static Inventory GetInventoryForChest(uint chestID)
        {
            if (!chestChannelMap.TryGetValue(chestID, out string channel))
            {
                WormholeChestsPlugin.Log.LogWarning($"GetInventoryForChest: ChestID {chestID} not found in chestChannelMap");
                return default;
            }
            Wormhole wormhole = GetWormhole(channel);
            if (wormhole == null)
            {
                return default;
            }
            return wormhole.inventory;
        }

        public static Inventory GetInventoryForChest(ChestInstance chestInstance)
        {
            return GetInventoryForChest(chestInstance.commonInfo.instanceId);
        }

        public static List<Wormhole> GetAllWormholes()
        {
            return wormholes.Values.ToList();
        }

        public static void CheckForEmptyChannels()
        {
            int index = 0;
            while (index < wormholes.Count)
            {
                string channel = wormholes.Keys.ToList()[index];
                if (GetNumChestsInChannel(channel) == 0)
                {
                    wormholes.Remove(channel);
                }
                else
                {
                    index++;
                }
            }
        }

        public static bool DoesChannelExist(string channel)
        {
            return wormholes.ContainsKey(channel);
        }

        public static ChestInstance GetAimedAtChest()
        {
            try
            {

                // Check if player/interaction exists
                if (Player.instance == null)
                {
                    return default;
                }
                if (Player.instance.interaction == null)
                {
                    return default;
                }

                object fieldValue = EMU.GetPrivateField<PlayerInteraction>("targetMachineRef", Player.instance.interaction);

                if (fieldValue == null)
                {
                    return default;  // Not aiming at anything, normal case
                }

                GenericMachineInstanceRef machineRef = (GenericMachineInstanceRef)fieldValue;


                // Check if this is actually a chest (typeIndex == MachineTypeEnum.Chest)
                if (machineRef.typeIndex != MachineTypeEnum.Chest)
                {
                    return default;  // Not a chest, normal case when opening other inventories
                }

                if (machineRef.index < 0)
                {
                    WormholeChestsPlugin.Log.LogWarning($"GetAimedAtChest: Invalid machineRef index: {machineRef.index}");
                    return default;
                }

                ChestInstance result = MachineManager.instance.Get<ChestInstance, ChestDefinition>(machineRef.index, MachineTypeEnum.Chest);
                return result;
            }
            catch (Exception ex)
            {
                WormholeChestsPlugin.Log.LogError($"GetAimedAtChest failed: {ex.Message}\n{ex.StackTrace}");
                return default;
            }
        }

        public static bool IsChestWormholeChest(ChestInstance chest)
        {
            return chestChannelMap.ContainsKey(chest.commonInfo.instanceId);
        }

        public static int GetCostToCreateOrLink()
        {
            return Mathf.CeilToInt(100f * Mathf.Pow(1.05f, chestChannelMap.Count));
        }

        public static int GetNumChestsInChannel(string channel)
        {
            int count = 0;
            foreach (string value in chestChannelMap.Values)
            {
                if (value.Equals(channel))
                {
                    count++;
                }
            }
            return count;
        }

        public static void SaveData(string worldName)
        {
            Directory.CreateDirectory(dataFolder);
            Directory.CreateDirectory(dataFolder + "/" + worldName);

            string wormholesPath = dataFolder + "/" + worldName + "/Wormholes.txt";
            List<string> wormholeLines = new List<string>();
            foreach (Wormhole wormhole in GetAllWormholes())
            {
                wormholeLines.Add(wormhole.Serialise());
            }
            File.WriteAllLines(wormholesPath, wormholeLines);

            string mapPath = dataFolder + "/" + worldName + "/ChestChannelMap.txt";
            List<string> mapLines = new List<string>();
            foreach (KeyValuePair<uint, string> kvp in chestChannelMap)
            {
                mapLines.Add($"{kvp.Key}|{kvp.Value}");
            }
            File.WriteAllLines(mapPath, mapLines);
        }

        public static void LoadData(string worldName)
        {
            try
            {
                string wormholesPath = dataFolder + "/" + worldName + "/Wormholes.txt";
                if (!File.Exists(wormholesPath))
                {
                    WormholeChestsPlugin.Log.LogInfo($"No wormhole data file found at {wormholesPath}");
                    return;
                }

                wormholes.Clear();
                chestChannelMap.Clear();

                string[] wormholeLines = File.ReadAllLines(wormholesPath);
                foreach (string serial in wormholeLines)
                {
                    if (string.IsNullOrEmpty(serial)) continue;
                    try
                    {
                        AddWormhole(new Wormhole(serial));
                    }
                    catch (Exception ex)
                    {
                        WormholeChestsPlugin.Log.LogWarning($"Failed to parse wormhole line: {serial} - {ex.Message}");
                    }
                }

                string mapPath = dataFolder + "/" + worldName + "/ChestChannelMap.txt";
                if (!File.Exists(mapPath))
                {
                    WormholeChestsPlugin.Log.LogWarning($"ChestChannelMap file not found at {mapPath}");
                    return;
                }

                string[] mapLines = File.ReadAllLines(mapPath);
                foreach (string line in mapLines)
                {
                    if (string.IsNullOrEmpty(line)) continue;
                    try
                    {
                        string[] parts = line.Split('|');
                        if (parts.Length >= 2)
                        {
                            uint key = uint.Parse(parts[0]);
                            string value = parts[1];
                            if (!chestChannelMap.ContainsKey(key))
                            {
                                chestChannelMap.Add(key, value);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        WormholeChestsPlugin.Log.LogWarning($"Failed to parse map line: {line} - {ex.Message}");
                    }
                }

                WormholeChestsPlugin.Log.LogInfo($"Loaded {wormholes.Count} wormholes and {chestChannelMap.Count} chest mappings");
            }
            catch (Exception ex)
            {
                WormholeChestsPlugin.Log.LogError($"LoadData failed: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    public static class ChestGUI
    {
        public static bool shouldShowGUI;
        public static uint currentChestID;
        public static bool showLinkedLabel;
        public static string lastChannel;
        public static string channel = "";

        private static Texture2D textBoxNormal;
        private static Texture2D textBoxHover;

        public static bool freeChests => WormholeChestsPlugin.freeWormholeChests.Value;
        public static bool useRelative => WormholeChestsPlugin.useRelativePositioning.Value;
        public static float channelBoxXOffset => WormholeChestsPlugin.channelBoxXOffset.Value;
        public static float channelBoxYOffset => WormholeChestsPlugin.channelBoxYOffset.Value;
        public static float channelBoxWidth => WormholeChestsPlugin.channelBoxWidth.Value;
        public static float createButtonXOffset => WormholeChestsPlugin.createButtonXOffset.Value;

        // Resolution-aware positioning: use percentage-based or fixed offset based on config
        public static float xPos => useRelative
            ? Screen.width * WormholeChestsPlugin.relativeXPercent.Value
            : Screen.width / 2f + channelBoxXOffset;

        public static float yPos => useRelative
            ? Screen.height * WormholeChestsPlugin.relativeYPercent.Value
            : Screen.height / 2f + channelBoxYOffset;

        public static float buttonXPos => useRelative
            ? Screen.width * WormholeChestsPlugin.relativeButtonXPercent.Value
            : xPos + createButtonXOffset;

        public static void LoadImages()
        {
            textBoxNormal = CreateTexture(240, 40, new Color(0.2f, 0.2f, 0.2f, 0.8f), new Color(0.5f, 0.5f, 0.5f, 1f));
            textBoxHover = CreateTexture(240, 40, new Color(0.25f, 0.25f, 0.25f, 0.9f), new Color(0.6f, 0.6f, 0.6f, 1f));
            WormholeChestsPlugin.wormholeTexture = CreateTexture(64, 64, new Color(0.1f, 0f, 0.2f, 1f), new Color(0.3f, 0f, 0.5f, 1f));
        }

        private static Texture2D CreateTexture(int width, int height, Color fillColor, Color borderColor)
        {
            Texture2D texture = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                    {
                        pixels[y * width + x] = borderColor;
                    }
                    else
                    {
                        pixels[y * width + x] = fillColor;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static void HandleKeyPresses()
        {
            try
            {
                if ((Event.current.keyCode == KeyCode.Tab || Event.current.character == '\t') &&
                    Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout)
                {
                    Event.current.Use();
                }

                if (UnityInput.Current.GetKey(KeyCode.Escape) || UnityInput.Current.GetKey(KeyCode.Tab))
                {
                    if (UIManager.instance != null && UIManager.instance.inventoryAndStorageMenu != null)
                    {
                        ((MachineMenuUI<ChestInstance>)(object)UIManager.instance.inventoryAndStorageMenu).Close();
                    }
                }

                if (InputHandler.instance != null)
                {
                    InputHandler.instance.uiInputBlocked = true;
                }
            }
            catch (Exception ex)
            {
                WormholeChestsPlugin.Log.LogError($"HandleKeyPresses failed: {ex.Message}");
            }
        }

        public static void DrawChestGUI()
        {
            HandleKeyPresses();
            DrawChannelBox();

            if (channel != "")
            {
                DrawCreateButton();
                DrawLinkedLabel();
                lastChannel = channel;
            }
        }

        private static void DrawChannelBox()
        {
            GUIStyle textBoxStyle = new GUIStyle
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft
            };
            textBoxStyle.normal.textColor = Color.white;
            textBoxStyle.normal.background = textBoxNormal;
            textBoxStyle.hover.textColor = Color.white;
            textBoxStyle.hover.background = textBoxHover;

            GUIStyle placeholderStyle = new GUIStyle
            {
                fontSize = 16,
                padding = new RectOffset(10, 0, 0, 0),
                alignment = TextAnchor.MiddleLeft
            };
            placeholderStyle.normal.textColor = Color.gray;

            channel = GUI.TextField(new Rect(xPos, yPos, channelBoxWidth, 40f), channel, textBoxStyle);

            if (channel == "")
            {
                GUI.Label(new Rect(xPos + 2f, yPos, channelBoxWidth - 10f, 40f), "Channel", placeholderStyle);
            }
        }

        private static void DrawCreateButton()
        {
            GUIStyle buttonStyle = new GUIStyle
            {
                fontSize = 16,
                padding = new RectOffset(10, 0, 0, 0),
                alignment = freeChests ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft
            };
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.normal.background = textBoxNormal;
            buttonStyle.hover.textColor = Color.white;
            buttonStyle.hover.background = textBoxHover;

            ChestInstance aimedChest = WormholeManager.GetAimedAtChest();
            if (aimedChest.IsNull())
            {
                // Can't get chest, don't draw create button
                return;
            }
            bool channelExists = WormholeManager.DoesChannelExist(channel);

            if (channelExists && WormholeManager.chestChannelMap.ContainsKey(aimedChest.commonInfo.instanceId))
            {
                showLinkedLabel = true;
            }

            string buttonText = channelExists ? "Link" : "Create";

            if (GUI.Button(new Rect(buttonXPos, yPos, channelBoxWidth, 40f), buttonText, buttonStyle))
            {
                if (!freeChests)
                {
                    CheckAndRemoveCores();
                }

                Inventory currentInventory = aimedChest.GetInventory();

                if (!channelExists)
                {
                    Inventory newInventory = new Inventory();
                    newInventory.CopyFrom(ref currentInventory);
                    WormholeManager.AddWormhole(new Wormhole
                    {
                        channel = channel,
                        inventory = newInventory
                    });
                }
                else
                {
                    Inventory oldInventory = aimedChest.GetInventory();
                    aimedChest.commonInfo.inventories[0] = WormholeManager.GetWormhole(channel).inventory;

                    ResourceStack[] stacks = oldInventory.myStacks;
                    for (int i = 0; i < stacks.Length; i++)
                    {
                        ResourceStack stack = stacks[i];
                        if (!stack.isEmpty)
                        {
                            int added;
                            ChestInstance.AddResources(ref aimedChest, stack.info.uniqueId, out added, stack.count);
                        }
                    }
                }

                showLinkedLabel = true;
                WormholeManager.chestChannelMap[currentChestID] = channel;

                GUI.SetNextControlName(" ");
                GUI.Label(new Rect(-100f, -100f, 1f, 1f), "");
                GUI.FocusControl(" ");
            }

            if (!freeChests)
            {
                DrawCostGUI();
            }
        }

        private static void CheckAndRemoveCores()
        {
            float cost = WormholeManager.GetCostToCreateOrLink();
            if (TechTreeState.instance.NumCoresAvailable(CoreType.Blue) < cost)
            {
                Player.instance.audio.buildError.PlayRandomClip(true);
                return;
            }

            Player.instance.audio.buildClick.PlayRandomClip(true);
            TechTreeState.instance.usedResearchCores[2] += WormholeManager.GetCostToCreateOrLink();
        }

        private static void DrawCostGUI()
        {
            float cost = WormholeManager.GetCostToCreateOrLink();
            bool canAfford = TechTreeState.instance.NumCoresAvailable(CoreType.Blue) >= cost;
            string costText = $"Cost: {cost}";

            GUIStyle costStyle = new GUIStyle
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleRight
            };
            costStyle.normal.textColor = canAfford ? Color.green : Color.red;

            GUIStyle iconStyle = new GUIStyle();
            iconStyle.normal.background = null;

            GUI.Box(new Rect(buttonXPos + channelBoxWidth - 35f, yPos + 5f, 30f, 30f),
                EMU.Images.GetImageForResource("Research Core 480nm (Blue)"), iconStyle);
            GUI.Label(new Rect(buttonXPos + 10f, yPos + 5f, channelBoxWidth - 50f, 30f), costText, costStyle);
        }

        private static void DrawLinkedLabel()
        {
            if (!string.IsNullOrEmpty(lastChannel) && lastChannel != channel)
            {
                showLinkedLabel = false;
            }

            if (showLinkedLabel)
            {
                GUIStyle linkedStyle = new GUIStyle
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                linkedStyle.normal.textColor = Color.green;
                linkedStyle.normal.background = textBoxNormal;

                GUI.Box(new Rect(buttonXPos, yPos, channelBoxWidth, 40f), "Linked!", linkedStyle);
            }
        }
    }

    // Harmony Patches
    internal class ChestDefinitionPatch
    {
        [HarmonyPatch(typeof(MachineDefinition<ChestInstance, ChestDefinition>), "OnDeconstruct")]
        [HarmonyPostfix]
        private static void UpdateChestMap(ChestDefinition __instance, ref ChestInstance erasedInstance)
        {
            // Skip if unlock not researched
            if (!WormholeChestsPlugin.isUnlockActive) return;

            uint instanceId = erasedInstance.commonInfo.instanceId;

            if (!WormholeManager.chestChannelMap.ContainsKey(instanceId))
                return;

            int numInChannel = WormholeManager.GetNumChestsInChannel(WormholeManager.chestChannelMap[instanceId]);

            if (numInChannel > 1)
            {
                ResourceStack[] stacks = erasedInstance.GetInventory().myStacks;
                for (int i = 0; i < stacks.Length; i++)
                {
                    ResourceStack stack = stacks[i];
                    if (!stack.isEmpty)
                    {
                        Player.instance.inventory.TryRemoveResources(stack.info.uniqueId, stack.count);
                    }
                }
            }

            WormholeManager.chestChannelMap.Remove(instanceId);
            WormholeManager.CheckForEmptyChannels();
        }
    }

    internal class ChestInstancePatch
    {
        [HarmonyPatch(typeof(ChestInstance), "GetInventory")]
        [HarmonyPrefix]
        private static void GetWormholeInsteadOfInventory(ChestInstance __instance)
        {
            // Skip ALL processing if unlock not researched
            if (!WormholeChestsPlugin.isUnlockActive) return;

            try
            {
                if (WormholeManager.IsChestWormholeChest(__instance))
                {
                    Inventory wormholeInventory = WormholeManager.GetInventoryForChest(__instance);
                    // Only replace if we got a valid inventory (has slots)
                    if (wormholeInventory.numSlots > 0)
                    {
                        __instance.commonInfo.inventories[0] = wormholeInventory;
                    }
                }
            }
            catch (Exception ex)
            {
                WormholeChestsPlugin.Log.LogError($"GetWormholeInsteadOfInventory failed: {ex.Message}");
            }
        }
    }

    internal class InventoryNavigatorPatch
    {
        [HarmonyPatch(typeof(InventoryNavigator), "OnOpen")]
        [HarmonyPrefix]
        private static void ShowGUI(InventoryNavigator __instance)
        {
            // Skip ALL processing if unlock not researched - let normal chest behavior work
            if (!WormholeChestsPlugin.isUnlockActive) return;

            try
            {
                // Safe chest retrieval - returns default if not looking at a chest
                ChestInstance chest = WormholeManager.GetAimedAtChest();
                if (chest.IsNull()) return;  // Not a chest, do nothing

                ChestGUI.shouldShowGUI = true;
                ChestGUI.currentChestID = chest.commonInfo.instanceId;

                if (WormholeManager.chestChannelMap.ContainsKey(chest.commonInfo.instanceId))
                {
                    ChestGUI.channel = WormholeManager.chestChannelMap[chest.commonInfo.instanceId];
                    if (WormholeManager.DoesChannelExist(ChestGUI.channel))
                    {
                        Wormhole wormhole = WormholeManager.GetWormhole(ChestGUI.channel);
                        if (wormhole != null)
                        {
                            chest.commonInfo.inventories[0] = wormhole.inventory;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WormholeChestsPlugin.Log.LogError($"ShowGUI failed: {ex.Message}");
                ChestGUI.shouldShowGUI = false;
            }
        }

        [HarmonyPatch(typeof(InventoryNavigator), "OnClose")]
        [HarmonyPrefix]
        private static void HideGui()
        {
            try
            {
                if (InputHandler.instance != null)
                {
                    InputHandler.instance.uiInputBlocked = false;
                }
                ChestGUI.shouldShowGUI = false;
                ChestGUI.channel = "";
            }
            catch (Exception ex)
            {
                WormholeChestsPlugin.Log.LogError($"HideGui failed: {ex.Message}");
            }
        }
    }

    internal class SaveStatePatch
    {
        [HarmonyPatch(typeof(SaveState), "SaveToFile")]
        [HarmonyPostfix]
        private static void SaveWormholes()
        {
            WormholeManager.SaveData(SaveState.instance.metadata.worldName);
            WormholeChestsPlugin.Log.LogInfo("WormholeChests Saved");
        }
    }
}
