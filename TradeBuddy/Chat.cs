using Dalamud.Game.Text.SeStringHandling;

namespace TradeBuddy
{
    public class Chat
    {
        public static void PrintError(string msg)
        {
            // 45 绿 #00CC22
            // 17 红 #DC0000
            // 508 粉红 #FF8080
            var builder = new SeStringBuilder()
                .AddUiForeground($"[{TradeBuddy.PluginName}]", 45)
                .AddUiForeground(msg, 508);
            DalamudInterface.ChatGui.PrintError(builder.BuiltString);
        }

        public static void PrintWarning(string msg)
        {
            // 62 黄 #F5EB67
            var builder = new SeStringBuilder()
                .AddUiForeground($"[{TradeBuddy.PluginName}]", 45)
                .AddUiForeground(msg, 62);
            DalamudInterface.ChatGui.PrintError(builder.BuiltString);
        }
    }
}
