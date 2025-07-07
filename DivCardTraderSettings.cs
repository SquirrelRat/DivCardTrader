using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using System.Windows.Forms;

namespace DivCardTurner
{
    /// <summary>
    /// Holds the settings for the DivCardTurner plugin.
    /// </summary>
    public class DivCardTurnerSettings : ISettings
    {
        // Master switch for the plugin.
        public ToggleNode Enable { get; set; } = new ToggleNode(true);

        // Hotkey to start the all-in-one turn-in process.
        public HotkeyNode TurnInCardsKey { get; set; } = new HotkeyNode(Keys.F5);

        // Hotkey to manually stop the process.
        public HotkeyNode StopProcessKey { get; set; } = new HotkeyNode(Keys.F6);

        // Delay for micro-actions like moving the mouse and clicking.
        public RangeNode<int> DelayBetweenActions { get; set; } = new RangeNode<int>(100, 20, 500);

        // A longer pause between each full card turn-in cycle.
        public RangeNode<int> PauseBetweenCycles { get; set; } = new RangeNode<int>(500, 100, 2000);
    }
}
