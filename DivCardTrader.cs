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

namespace DivCardTrader
{
    public class DivCardTrader : BaseSettingsPlugin<DivCardTraderSettings>
    {
        private Coroutine _mainTurnInCoroutine;
        private TurnInState _currentState = TurnInState.Idle;

        private enum TurnInState
        {
            Idle,
            TurningIn,
            TakingReward
        }


        public override Job Tick()
        {
            if (!Settings.Enable.Value || !GameController.InGame) return null;

            if (!GameController.IngameState.IngameUi.CardTradeWindow.IsVisible)
            {
                if (_currentState != TurnInState.Idle)
                {
                    LogMessage("Trade window closed, stopping the turn-in process.", 3);
                    _currentState = TurnInState.Idle;
                    if (_mainTurnInCoroutine != null && !_mainTurnInCoroutine.IsDone)
                    {
                        _mainTurnInCoroutine.Done(true);
                    }
                }
                return null;
            }

            if (Input.IsKeyDown(Settings.TurnInCardsKey.Value) && _currentState == TurnInState.Idle)
            {
                _mainTurnInCoroutine = new Coroutine(ProcessAllCards(), this, "DivCardTrader.ProcessAllCards");
                Core.ParallelRunner.Run(_mainTurnInCoroutine);
            }

            if (Input.IsKeyDown(Settings.StopProcessKey.Value))
            {
                if (_currentState != TurnInState.Idle)
                {
                    LogMessage("Manual stop requested. Halting the turn-in process.", 5);
                    _currentState = TurnInState.Idle;
                    if (_mainTurnInCoroutine != null && !_mainTurnInCoroutine.IsDone)
                    {
                        _mainTurnInCoroutine.Done(true);
                    }
                }
            }

            return null;
        }

        private IEnumerator<WaitTime> ProcessAllCards()
        {
            try
            {
                _currentState = TurnInState.TurningIn;
                LogMessage("Starting full divination card turn-in process.", 5);

                while (_currentState == TurnInState.TurningIn)
                {
                    if (IsInventoryFull())
                    {
                        LogMessage("Inventory is full. Stopping process.", 5);
                        break;
                    }

                    var inventory = GameController.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];
                    var cardStack = inventory.VisibleInventoryItems
                        .FirstOrDefault(item =>
                        {
                            if (item.Item == null || !item.Item.Path.StartsWith("Metadata/Items/DivinationCards/"))
                                return false;

                            if (!item.Item.TryGetComponent<Stack>(out var stack) || stack.Size != stack.Info.MaxStackSize)
                                return false;

                            if (string.IsNullOrWhiteSpace(Settings.CardNames.Value))
                                return true;

                            var cardName = item.Item.GetComponent<Base>()?.Name;
                            var allowedCards = new HashSet<string>(Settings.CardNames.Value.Split(',').Select(s => s.Trim()), System.StringComparer.OrdinalIgnoreCase);
                            return allowedCards.Contains(cardName);
                        });

                    if (cardStack == null)
                    {
                        LogMessage("No more full stacks of divination cards found.", 5);
                        break;
                    }
                    
                    var tradeWindow = GameController.IngameState.IngameUi.CardTradeWindow;

                    if (tradeWindow.CardSlotItem != null)
                    {
                        LogMessage("Trade window is not empty, stopping.", 5);
                        break;
                    }

                    LogMessage($"Turning in: {cardStack.Item.GetComponent<Base>()?.Name}", 5);
                    var turnInClick = ClickItem(cardStack.GetClientRect());
                    while (turnInClick.MoveNext())
                    {
                        yield return turnInClick.Current;
                    }
                    yield return new WaitTime(Settings.DelayBetweenActions.Value);

                    if (tradeWindow.CardSlotItem == null)
                    {
                        LogMessage("Card did not move to trade window. Aborting.", 5);
                        break;
                    }

                    var tradeButton = tradeWindow.TradeButton;
                    if (tradeButton == null || !tradeButton.IsVisible)
                    {
                        LogMessage("Could not find the 'Trade' button. Aborting.", 5);
                        break;
                    }

                    var tradeButtonRect = tradeButton.GetClientRect();
                    var newCenter = new Vector2(tradeButtonRect.Center.X + Settings.TradeButtonOffsetX.Value, tradeButtonRect.Center.Y + Settings.TradeButtonOffsetY.Value);
                    Input.SetCursorPos(newCenter);
                    yield return new WaitTime(Settings.DelayBetweenActions.Value);
                    Input.Click(MouseButtons.Left);
                    yield return new WaitTime(Settings.DelayBetweenActions.Value + 150);

                    _currentState = TurnInState.TakingReward;
                    var rewardItem = tradeWindow.CardSlotItem;
                    if (rewardItem != null)
                    {
                        LogMessage("Taking reward item.", 5);
                        var takeRewardClick = ClickItem(rewardItem.GetClientRect());
                        while (takeRewardClick.MoveNext())
                        {
                            yield return takeRewardClick.Current;
                        }
                    }
                    else
                    {
                        LogMessage("No reward item found after trade.", 5);
                    }

                    LogMessage($"Pausing for {Settings.PauseBetweenCycles.Value}ms.", 5);
                    yield return new WaitTime(Settings.PauseBetweenCycles.Value);
                    _currentState = TurnInState.TurningIn;
                }

                LogMessage("All divination card stacks have been processed.", 5);
            }
            finally
            {
                Input.KeyUp(Keys.LControlKey);
                _currentState = TurnInState.Idle;
            }
        }

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

        private bool IsInventoryFull()
        {
            var inventory = GameController.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];
            return inventory.VisibleInventoryItems.Count >= 60;
        }

    }
}