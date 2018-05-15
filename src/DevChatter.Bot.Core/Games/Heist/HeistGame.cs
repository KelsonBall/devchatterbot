using System;
using System.Collections.Generic;
using System.Linq;
using DevChatter.Bot.Core.Automation;
using DevChatter.Bot.Core.Data.Model;
using DevChatter.Bot.Core.Events;
using DevChatter.Bot.Core.Systems.Chat;
using DevChatter.Bot.Core.Util;

namespace DevChatter.Bot.Core.Games.Heist
{
    public class HeistGame : JoinableGame
    {
        private readonly ICurrencyGenerator _currencyGenerator;
        public bool IsGameRunning { get; private set; }
        private const UserRole ROLE_REQUIRED_TO_START = UserRole.Subscriber;
        private const int HEIST_DELAY_IN_SECONDS = 90;

        private HeistMission _selectedHeist = null;

        private readonly Dictionary<HeistRoles, string> _heistMembers = new Dictionary<HeistRoles, string>();
        private DelayedMessageAction _lastCallToJoin;
        private OneTimeCallBackAction _startHeistAction;

        public HeistGame(IAutomatedActionSystem automatedActionSystem, ICurrencyGenerator currencyGenerator)
            : base(automatedActionSystem)
        {
            _currencyGenerator = currencyGenerator;
        }

        public void AttemptToCreateGame(IChatClient chatClient, ChatUser chatUser)
        {
            if (IsGameRunning) return;

            // role check here
            if (!chatUser.IsInThisRoleOrHigher(ROLE_REQUIRED_TO_START))
            {
                chatClient.SendMessage($"Sorry, {chatUser.DisplayName}, but you're not experience enough (sub only) to organize a heist like this one...");
                return;
            }

            // start the game
            IsGameRunning = true;
            SelectRandomHeist();
            chatClient.SendMessage($"{chatUser.DisplayName} is organizing a {_selectedHeist.Name}. Type !heist or !heist [role] to join the team!");

            ScheduleAutomatedActions(chatClient);
        }

        private void SelectRandomHeist()
        {
            (_, _selectedHeist) = MyRandom.ChooseRandomItem(HeistMission.Missions);
        }

        private void ScheduleAutomatedActions(IChatClient chatClient)
        {
            _lastCallToJoin = new DelayedMessageAction(HEIST_DELAY_IN_SECONDS - 30,
                "Only 30 seconds left to join the heist! Type !heist to join it!", chatClient);
            AutomatedActionSystem.AddAction(_lastCallToJoin);

            _startHeistAction = new OneTimeCallBackAction(HEIST_DELAY_IN_SECONDS, () => StartHeist(chatClient));
            AutomatedActionSystem.AddAction(_startHeistAction);
        }

        public void StartHeist(IChatClient chatClient)
        {
            HeistMissionResult heistMissionResult = _selectedHeist.AttemptHeist(_heistMembers);

            foreach (string resultMessage in heistMissionResult.ResultMessages)
            {
                chatClient.SendMessage(resultMessage);
            }

            _currencyGenerator.AddCurrencyTo(heistMissionResult.SurvivingMembers, 50);

            ResetHeist();
        }

        private void ResetHeist()
        {
            _heistMembers.Clear();
            IsGameRunning = false;
            _selectedHeist = null;
            AutomatedActionSystem.RemoveAction(_lastCallToJoin);
            AutomatedActionSystem.RemoveAction(_startHeistAction);
        }

        public override JoinGameResult AttemptToJoin(ChatUser chatUser, IList<string> arguments)
        {
            JoinGameResult joinGameResult = base.AttemptToJoin(chatUser, arguments);

            if (joinGameResult.Success)
            {
                return HandleHeistJoining(chatUser, arguments);
            }

            return joinGameResult;
        }

        private JoinGameResult HandleHeistJoining(ChatUser chatUser, IList<string> arguments)
        {
            if (!Enum.TryParse(arguments?.ElementAtOrDefault(0) ?? "", true, out HeistRoles role))
            {
                bool success;
                (success, role) = MyRandom.ChooseRandomItem(GetAvailableRoles());
                if (!success)
                {
                    return HeistJoinResults.HeistFullResult(chatUser.DisplayName);
                }
            }

            if (_heistMembers.ContainsKey(role))
            {
                return HeistJoinResults.RoleTakenResult(chatUser.DisplayName, role);
            }

            _heistMembers.Add(role, chatUser.DisplayName);

            return HeistJoinResults.SuccessJoinResult(chatUser.DisplayName, role);
        }

        private List<HeistRoles> GetAvailableRoles()
        {
            var allHeistRoles = Enum.GetValues(typeof(HeistRoles)).Cast<HeistRoles>();
            var claimedRoles = _heistMembers.Keys;
            return allHeistRoles.Except(claimedRoles).ToList();
        }

        public bool AttemptToStartGame(IChatClient chatClient, ChatUser chatUser)
        {
            if (!GetAvailableRoles().Any() && IsGameRunning)
            {
                AutomatedActionSystem.RemoveAction(_startHeistAction);
                StartHeist(chatClient);
                return true;
            }

            return false;
        }

        public override bool IsGameJoinable => IsRunning && GetAvailableRoles().Any();
    }
}
