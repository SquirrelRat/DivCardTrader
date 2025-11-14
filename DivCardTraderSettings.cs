using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using System.Windows.Forms;
using ExileCore.Shared.Attributes;

namespace DivCardTrader
{
    public class DivCardTraderSettings : ISettings
    {
        [Menu("Enable", "Toggles the DivCardTrader plugin on or off.")]
        public ToggleNode Enable { get; set; } = new ToggleNode(true);
        [Menu("Turn In Cards Key", "Hotkey to start the process of turning in divination cards.")]
        public HotkeyNode TurnInCardsKey { get; set; } = new HotkeyNode(Keys.F5);
        [Menu("Stop Process Key", "Hotkey to stop the current divination card trading process.")]
        public HotkeyNode StopProcessKey { get; set; } = new HotkeyNode(Keys.F6);
        [Menu("Delay Between Actions", "Delay in milliseconds between individual actions (e.g., clicking, moving).")]
        public RangeNode<int> DelayBetweenActions { get; set; } = new RangeNode<int>(100, 20, 500);
        [Menu("Pause Between Cycles", "Pause in milliseconds between complete cycles of turning in cards.")]
        public RangeNode<int> PauseBetweenCycles { get; set; } = new RangeNode<int>(500, 100, 2000);
        [Menu("Card Names", "Comma-separated list of divination card names to trade. Leave empty to trade all recognized cards.")]
        public TextNode CardNames { get; set; } = new TextNode("");

        [Menu("Trade Button Offset X", "Fine-tune the X-axis position for the trade button click.")]
        public RangeNode<int> TradeButtonOffsetX { get; set; } = new RangeNode<int>(0, -100, 100);

        [Menu("Trade Button Offset Y", "Fine-tune the Y-axis position for the trade button click.")]
        public RangeNode<int> TradeButtonOffsetY { get; set; } = new RangeNode<int>(0, -100, 100);
    }
}