﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using ServerSync;
using UnityEngine.Experimental.Playables;


namespace Trader20
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class Trader20 : BaseUnityPlugin
    {
        private const string ModName = "Trader2.0";
        public const string ModVersion = "0.0.2";
        private const string ModGUID = "com.zarboz.Trader20";
        public static ConfigSync configSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion};
        public static readonly CustomSyncedValue<Dictionary<string, ItemDataEntry>> traderConfig = new(configSync, "trader config", new Dictionary<string, ItemDataEntry>());
        internal static GameObject Knarr;
        internal static GameObject CustomTraderScreen;
        internal static Sprite coins;
        internal static ConfigEntry<ItemDataEntry> _syncedValue;
        public static Dictionary<string, ItemDataEntry> entry_ { get; set; }
        internal static AssetBundle assetBundle { get; set; }
        internal static string paths = Paths.ConfigPath;
        public static ConfigEntry<bool> serverConfigLocked;
        internal static ConfigEntry<string> CurrencyPrefabName;

        ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => config(group, name, value, new ConfigDescription(description), synchronizedSetting);

        public void Awake()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Harmony harmony = new(ModGUID);
            harmony.PatchAll(assembly);
            assetBundle = Utilities.LoadAssetBundle("traderbundle")!;
            serverConfigLocked = config("General", "Lock Configuration", false, "Lock Configuration");
            configSync.AddLockingConfigEntry(serverConfigLocked);
            if (!File.Exists(paths + "/trader_config.yaml"))
            {
                var file = File.Create(paths + "/trader_config.yaml");
                file.Close();
            }
            if (File.ReadLines(paths+"/trader_config.yaml").Count() != 0)
            {
                var file = File.OpenText(Trader20.paths + "/trader_config.yaml");
                entry_ = YMLParser.ReadSerializedData(file.ReadToEnd());
                traderConfig.Value = entry_;
                traderConfig.ValueChanged += OnValChangUpdateStore;
            }

            CurrencyPrefabName = config("Genera", "Config Prefab Name", "Coins",
                "This is the prefab name for the currency that Knarr uses in his trades");
        }

        private void OnValChangUpdateStore()
        {
            if (ObjectDB.instance.m_items.Count <= 0 || ObjectDB.instance.GetItemPrefab("Wood") == null) return;
            OdinStore.instance.DumpDict();
            OdinStore.instance.ClearStore();
            foreach (var variable in traderConfig.Value)
            {
                var drop = ObjectDB.instance.GetItemPrefab(variable.Key)
                    .GetComponent<ItemDrop>();
                OdinStore.instance.AddItemToDict(drop, variable.Value.ItemCostInt,
                    variable.Value.ItemCount);
            }
        }
    }
}