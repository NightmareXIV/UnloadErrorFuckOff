using Dalamud.Game.Command;
using Dalamud.Logging;
using Dalamud.Plugin;
using ExposedObject;
using System;
using System.Linq;
using System.Reflection;

namespace UnloadErrorFuckOff
{
    public class UnloadErrorFuckOff : IDalamudPlugin
    {
        private DalamudPluginInterface pi;
        private CommandManager cmd;

        public string Name => "UnloadErrorFuckOff";

        public UnloadErrorFuckOff(DalamudPluginInterface pi, CommandManager cmd)
        {
            this.pi = pi;
            this.cmd = cmd;
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
                    var localPlugin = Exposed.From(t);
                    PluginLog.Information($"Plugin {localPlugin.Name}, state {localPlugin.State}");
                    if (localPlugin.State.ToString() == "UnloadError" || localPlugin.State.ToString() == "LoadError"
                         || localPlugin.State.ToString() == "DependencyResolutionFailed")
                    {
                        PluginLog.Warning("Detected error state, let's fix it");
                        localPlugin.State = stateEnum.GetEnumValues().GetValue(0);
                        var manifest = Exposed.From(localPlugin.Manifest);
                        manifest.Disabled = true;
                        PluginLog.Information($"Plugin {localPlugin.Name} new state {localPlugin.State}");
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
