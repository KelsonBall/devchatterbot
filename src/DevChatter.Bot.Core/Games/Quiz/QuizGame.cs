using DevChatter.Bot.Core.Automation;
using DevChatter.Bot.Core.Data;
using DevChatter.Bot.Core.Data.Model;
using DevChatter.Bot.Core.Data.Specifications;
using DevChatter.Bot.Core.Events;
using DevChatter.Bot.Core.Systems.Chat;
using DevChatter.Bot.Core.Util;
using System.Collections.Generic;
using System.Linq;

namespace DevChatter.Bot.Core.Games.Quiz
{
    public class QuizGame : JoinableGame
    {
        private readonly IRepository _repository;
        private readonly ICurrencyGenerator _currencyGenerator;

        public Dictionary<string, char> CurrentPlayers { get; set; } = new Dictionary<string, char>();

        public QuizGame(IRepository repository, ICurrencyGenerator currencyGenerator,
            IAutomatedActionSystem automatedActionSystem) : base(automatedActionSystem)
        {
            _repository = repository;
            _currencyGenerator = currencyGenerator;
        }

        private bool _questionAskingStarted = false;
        private DelayedMessageAction _messageHint1;
        private DelayedMessageAction _messageHint2;
        private OneTimeCallBackAction _oneTimeActionEndingQuestion;

        public override bool IsGameJoinable => IsRunning && !_questionAskingStarted;

        public void StartGame(IChatClient chatClient)
        {
            if (IsRunning)
            {
                return;
            }

            CreateGameJoinWindow(chatClient, () => StartAskingQuestions(chatClient));

            IsRunning = true;
        }

        private void StartAskingQuestions(IChatClient chatClient)
        {
            _questionAskingStarted = true;

            chatClient.SendMessage($"Starting the quiz now! Our competitors are: {string.Join(", ", CurrentPlayers.Keys)}");

            QuizQuestion randomQuestion = GetRandomQuestion();

            chatClient.SendMessage(randomQuestion.MainQuestion);
            chatClient.SendMessage(randomQuestion.GetRandomizedAnswers());

            _messageHint1 = new DelayedMessageAction(10, $"Hint 1: {randomQuestion.Hint1}", chatClient);
            AutomatedActionSystem.AddAction(_messageHint1);
            _messageHint2 = new DelayedMessageAction(20, $"Hint 2: {randomQuestion.Hint2}", chatClient);
            AutomatedActionSystem.AddAction(_messageHint2);
            _oneTimeActionEndingQuestion = new OneTimeCallBackAction(30, () => CompleteQuestion(chatClient, randomQuestion));
            AutomatedActionSystem.AddAction(_oneTimeActionEndingQuestion);
        }

        private void CompleteQuestion(IChatClient chatClient, QuizQuestion question)
        {
            chatClient.SendMessage($"The correct answer was... {question.CorrectAnswer}");

            AwardWinners(chatClient, question);

            ResetGame();
        }

        private void AwardWinners(IChatClient chatClient, QuizQuestion question)
        {
            char correctLetter = question.LetterAssignment.Single(x => x.Value == question.CorrectAnswer).Key;
            List<string> winners = CurrentPlayers.Where(x => x.Value == correctLetter).Select(x => x.Key).ToList();
            chatClient.SendMessage($"Congratulations to {string.Join(", ", winners)}");
            _currencyGenerator.AddCurrencyTo(winners, 10);
        }

        private void ResetGame()
        {
            IsRunning = false;
            _questionAskingStarted = false;
            CurrentPlayers.Clear();
            AutomatedActionSystem.RemoveAction(_messageHint1);
            AutomatedActionSystem.RemoveAction(_messageHint2);
            AutomatedActionSystem.RemoveAction(_oneTimeActionEndingQuestion);
        }

        private QuizQuestion GetRandomQuestion()
        {
            return MyRandom.ChooseRandomItem(_repository.List(DataItemPolicy<QuizQuestion>.All())).ChosenItem;
        }

        public string UpdateGuess(ChatUser chatUser, string guess)
        {
            if (CurrentPlayers.ContainsKey(chatUser.DisplayName))
            {
                CurrentPlayers[chatUser.DisplayName] = guess.ToLower().Single();
                return $"You updated your guess to {guess}, {chatUser.DisplayName}.";
            }

            return $"You aren't playing. Stop it, {chatUser.DisplayName}.";
        }

        public string AttemptToLeave(ChatUser chatUser)
        {
            if (!IsRunning)
            {
                return "You can't leave a game that isn't being played."; // TODO: this needs to be a whisper
            }

            // TODO: Make this boolean check be an abstract in the JoinableGame class
            if (_questionAskingStarted)
            {
                return "The questions have started, you can't leave."; // TODO: this needs to be a whisper
            }

            if (CurrentPlayers.ContainsKey(chatUser.DisplayName))
            {
                CurrentPlayers.Remove(chatUser.DisplayName);
                return $"{chatUser.DisplayName} has quit the game.";
            }

            return $"You aren't in this game, {chatUser.DisplayName}"; // TODO: this needs to be a whisper
        }
    }
}
