using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ExileCore.Shared;
using SharpDX;

namespace DivCardTurner
{
    /// <summary>
    /// Main plugin class for automating divination card turn-ins.
    /// </summary>
    public class DivCardTurner : BaseSettingsPlugin<DivCardTurnerSettings>
    {
        private Coroutine _mainTurnInCoroutine;

        public override Job Tick()
        {
            if (!Settings.Enable.Value || !GameController.InGame) return null;

            var tradeWindow = GameController.IngameState.IngameUi.CardTradeWindow;
            if (!tradeWindow.IsVisible)
            {
                if (_mainTurnInCoroutine != null && !_mainTurnInCoroutine.IsDone)
                {
                    LogMessage("Trade window closed, stopping the turn-in process.", 3);
                    _mainTurnInCoroutine.Done(true);
                }
                return null;
            }

            // F5 starts the main all-in-one process
            if (Input.IsKeyDown(Settings.TurnInCardsKey.Value))
            {
                if (_mainTurnInCoroutine == null || _mainTurnInCoroutine.IsDone)
                {
                    _mainTurnInCoroutine = new Coroutine(ProcessAllCards(), this, "DivCardTurner.ProcessAllCards");
                    Core.ParallelRunner.Run(_mainTurnInCoroutine);
                }
            }

            // F6 is now the emergency stop key, using the updated setting name
            if (Input.IsKeyDown(Settings.StopProcessKey.Value))
            {
                if (_mainTurnInCoroutine != null && !_mainTurnInCoroutine.IsDone)
                {
                    LogMessage("Manual stop requested. Halting the turn-in process.", 5);
                    _mainTurnInCoroutine.Done(true);
                }
            }

            return null;
        }

        /// <summary>
        /// The main coroutine that handles the entire turn-in cycle for all available cards.
        /// </summary>
        private IEnumerator<WaitTime> ProcessAllCards()
        {
            try
            {
                LogMessage("Starting full divination card turn-in process.", 5);
                
                while (true)
                {
                    var inventory = GameController.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];

                    // Find the next available card stack inside the loop.
                    var cardStack = inventory.VisibleInventoryItems
                        .FirstOrDefault(item =>
                            item.Item != null &&
                            item.Item.Path.StartsWith("Metadata/Items/DivinationCards/") &&
                            item.Item.TryGetComponent<Stack>(out var stack) && stack.Size == stack.Info.MaxStackSize);

                    // If no more stacks are found, the process is complete.
                    if (cardStack == null)
                    {
                        LogMessage("No more full stacks of divination cards found.", 5);
                        break;
                    }

                    // Check for inventory space before each cycle.
                    if (IsInventoryFull())
                    {
                        LogMessage("Inventory is full. Stopping process.", 5);
                        break;
                    }

                    // 1. Turn in the card
                    var turnIn = TurnInSingleCard(cardStack);
                    while (turnIn.MoveNext())
                    {
                        yield return turnIn.Current;
                    }

                    // 2. Take the reward
                    var takeReward = TakeSingleReward();
                    while (takeReward.MoveNext())
                    {
                        yield return takeReward.Current;
                    }
                    
                    // Use the configurable pause between cycles.
                    LogMessage($"Pausing for {Settings.PauseBetweenCycles.Value}ms.", 5);
                    yield return new WaitTime(Settings.PauseBetweenCycles.Value); 
                }

                LogMessage("All divination card stacks have been processed.", 5);
            }
            finally
            {
                // This ensures the key is always released when the coroutine ends for any reason.
                Input.KeyUp(Keys.LControlKey);
            }
        }

        /// <summary>
        /// Helper to turn in one specific card stack.
        /// </summary>
        private IEnumerator<WaitTime> TurnInSingleCard(NormalInventoryItem cardStack)
        {
            var tradeWindow = GameController.IngameState.IngameUi.CardTradeWindow;

            if (tradeWindow.CardSlotItem != null)
            {
                LogMessage("Cannot turn in card, the trade window is not empty.", 5);
                yield break;
            }

            if (cardStack?.Item == null)
            {
                LogMessage("Card stack is invalid.", 3);
                yield break;
            }

            LogMessage($"Turning in: {cardStack.Item.GetComponent<Base>()?.Name}", 5);
            var clicker = ClickItem(cardStack.GetClientRect());
            while (clicker.MoveNext())
            {
                yield return clicker.Current;
            }
        }

        /// <summary>
        /// Helper to take one reward item.
        /// </summary>
        private IEnumerator<WaitTime> TakeSingleReward()
        {
            var tradeWindow = GameController.IngameState.IngameUi.CardTradeWindow;

            if (tradeWindow.CardSlotItem == null)
            {
                LogMessage("No card in the window to trade.", 5);
                yield break;
            }

            var tradeButton = tradeWindow.TradeButton;
            if (tradeButton == null || !tradeButton.IsVisible)
            {
                LogMessage("Could not find the 'Trade' button.", 5);
                yield break;
            }

            Input.SetCursorPos(tradeButton.GetClientRect().Center);
            yield return new WaitTime(Settings.DelayBetweenActions.Value);
            Input.Click(MouseButtons.Left);
            yield return new WaitTime(Settings.DelayBetweenActions.Value + 150);

            // The reward item should now be in the slot.
            var rewardItem = GameController.IngameState.IngameUi.CardTradeWindow.CardSlotItem;
            if (rewardItem != null)
            {
                LogMessage("Taking reward item.", 5);
                var clicker = ClickItem(rewardItem.GetClientRect());
                while (clicker.MoveNext())
                {
                    yield return clicker.Current;
                }
            }
            else
            {
                LogMessage("No reward item found after trade.", 5);
            }
        }
        
        /// <summary>
        /// Helper method to perform a Ctrl+Click on a given rectangle area.
        /// </summary>
        private IEnumerator<WaitTime> ClickItem(RectangleF rect)
        {
            Input.KeyDown(Keys.LControlKey);
            yield return new WaitTime(20);
            Input.SetCursorPos(rect.Center);
            yield return new WaitTime(Settings.DelayBetweenActions.Value);
            Input.Click(MouseButtons.Left);
            yield return new WaitTime(20);
            Input.KeyUp(Keys.LControlKey);
            yield return new WaitTime(Settings.DelayBetweenActions.Value);
        }

        /// <summary>
        /// Checks if the player's main inventory has any free space.
        /// </summary>
        private bool IsInventoryFull()
        {
            var inventory = GameController.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];
            // The main inventory has 60 slots (12x5).
            // We check if the number of visible items is at the maximum.
            return inventory.VisibleInventoryItems.Count >= 60;
        }
    }
}
