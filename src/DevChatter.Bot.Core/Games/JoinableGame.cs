using DevChatter.Bot.Core.Automation;
using DevChatter.Bot.Core.Data.Model;
using DevChatter.Bot.Core.Extensions;
using DevChatter.Bot.Core.Systems.Chat;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DevChatter.Bot.Core.Games
{
    public abstract class JoinableGame : IGame
    {
        protected readonly IAutomatedActionSystem AutomatedActionSystem;
        protected JoinableGame(IAutomatedActionSystem automatedActionSystem)
        {
            AutomatedActionSystem = automatedActionSystem;
        }

        public List<string> CurrentPlayerNames { get; set; } = new List<string>();
        public bool IsRunning { get; protected set;  }
        public abstract bool IsGameJoinable { get; }

        protected void CreateGameJoinWindow(IChatClient chatClient, Action gameStartFunction)
        {
            var joinWarning = new DelayedMessageAction(30, "You only have 30 seconds left to join the quiz game! Type \"!quiz join\" to join the game!", chatClient);
            AutomatedActionSystem.AddAction(joinWarning);

            var startAskingQuestions = new OneTimeCallBackAction(60, gameStartFunction);
            AutomatedActionSystem.AddAction(startAskingQuestions);
        }

        public JoinGameResult AttemptToJoin(ChatUser chatUser)
        {
            if (CurrentPlayerNames.Any(x => x.EqualsIns(chatUser.DisplayName)))
            {
                return StaticResults.AlreadyInGameResult(chatUser.DisplayName);
            }

            if (!IsGameJoinable)
            {
                return StaticResults.NotJoinTimeResult(chatUser.DisplayName);
            }

            CurrentPlayerNames.Add(chatUser.DisplayName);
            return StaticResults.SuccessJoinResult(chatUser.DisplayName);
        }
    }
}
