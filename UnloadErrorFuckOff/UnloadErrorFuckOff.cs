using Dalamud.Game.Command;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ExposedObject;
using System;
using System.Linq;
using System.Reflection;

namespace UnloadErrorFuckOff
{
    public class UnloadErrorFuckOff : IDalamudPlugin
    {
        private IDalamudPluginInterface pi;
        private ICommandManager cmd;
        private IPluginLog PluginLog;

        public string Name => "UnloadErrorFuckOff";

        public UnloadErrorFuckOff(IDalamudPluginInterface pi, ICommandManager cmd, IPluginLog log)
        {
            this.pi = pi;
            this.cmd = cmd;
            this.PluginLog = log;
            pi.UiBuilder.OpenConfigUi += FuckOff;
            cmd.AddHandler("/fuckoff", new(delegate { FuckOff(); }));
        }

        void FuckOff()
        {
            try
            {
                var pluginManager = Exposed.From(pi.GetType().Assembly.
                    GetType("Dalamud.Service`1", true)
                    .MakeGenericType(pi.GetType().Assembly.GetType("Dalamud.Plugin.Internal.PluginManager", true)))
                    .Get();
                var installedPlugins = Exposed.From(pluginManager).InstalledPlugins;
                Type stateEnum = pi.GetType().Assembly.GetType("Dalamud.Plugin.Internal.Types.PluginState");
                
                foreach (var t in installedPlugins)
                {
                    var localPlugin = (object)t;
                    var state = localPlugin.GetType().GetProperty("State", BindingFlags.Public | BindingFlags.Instance).GetValue(localPlugin).ToString();
                    PluginLog.Information($"Plugin {localPlugin.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance).GetValue(localPlugin)}, state {state}");
                    if (state == "UnloadError" || state == "LoadError"
                         || state == "DependencyResolutionFailed")
                    {
                        PluginLog.Warning("Detected error state, let's fix it");
                        localPlugin.GetType().GetProperty("State", BindingFlags.Public | BindingFlags.Instance).SetValue(localPlugin, stateEnum.GetEnumValues().GetValue(0));
                        var manifest = localPlugin.GetType().GetProperty("Manifest", BindingFlags.Public | BindingFlags.Instance).GetValue(localPlugin);
                        manifest.GetType().GetProperty("Disabled", BindingFlags.Public | BindingFlags.Instance).SetValue(manifest, true);
                        state = localPlugin.GetType().GetProperty("State", BindingFlags.Public | BindingFlags.Instance).GetValue(localPlugin).ToString();
                        PluginLog.Information($"Plugin {localPlugin.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance).GetValue(localPlugin)}, state {state}");
                    }

                }
            }
            catch(Exception e)
            {
                PluginLog.Error($"{e.Message}\n{e.StackTrace}");
            }
        }

        public void Dispose()
        {
            cmd.RemoveHandler("/fuckoff");
        }
    }
}
