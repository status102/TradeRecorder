using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.IO;

namespace TradeBuddy
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

        public bool ShowTrade { get; set; } = true;

        public string tradeConfirmStr { get; set; } = "交易完成。";
        public string tradeCancelStr { get; set; } = "交易取消。";

        public Dictionary<string, int> preset = new Dictionary<string, int>() { };

        #region Init and Save

        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }

        #endregion
    }
}
