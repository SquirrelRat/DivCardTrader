using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using System.Windows.Forms;

namespace DivCardTrader
{
    public class DivCardTraderSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(true);
        public HotkeyNode TurnInCardsKey { get; set; } = new HotkeyNode(Keys.F5);
        public HotkeyNode StopProcessKey { get; set; } = new HotkeyNode(Keys.F6);
        public RangeNode<int> DelayBetweenActions { get; set; } = new RangeNode<int>(100, 20, 500);
        public RangeNode<int> PauseBetweenCycles { get; set; } = new RangeNode<int>(500, 100, 2000);
    }
}