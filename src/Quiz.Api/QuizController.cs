using System;
using System.Linq;
using System.Threading.Tasks;
using EasyEventSourcing;
using EasyEventSourcing.Aggregate;
using EasyNetQ;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Quiz.Domain;
using Quiz.Domain.Commands;

namespace Quiz.Api
{
    [Route("[controller]")]
    public class QuizController
    {
        private readonly IRepository _quizRepository;
        private readonly IEventStoreProjections _projectionsClient;
        private readonly IBus _brokerBus;

        public QuizController(
            IRepository quizRepository, 
            IEventStoreProjections projectionsClient,
            IBus brokerBus)
        {
            _quizRepository = quizRepository;
            _projectionsClient = projectionsClient;
            _brokerBus = brokerBus;
        }

        [HttpGet]
        public async Task<QuizReadModel> Get()
        {
            var result = await _projectionsClient.GetStateAsync(); 
            return JsonConvert.DeserializeObject<QuizReadModel>(result);
        }

        [HttpPost]
        [Route("{id}")]
        public async Task Vote(Guid id, [FromBody]QuizAnswersCommand quizAnswersComand) =>
            await _brokerBus.PublishAsync(new QuizAnswersCommand(id, quizAnswersComand.Answers));

        [HttpPut]
        public async Task<object> Start()
        {
            var quizModel = QuizModelFactory.Create();
            var quiz = new QuizAggregate();
            quiz.Start(quizModel);
            await _quizRepository.Save(quiz);
            return new 
            {
                QuizId = quiz.Id,
                Questions = quiz.QuizModel.Questions
            };
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task Close(Guid id)
        {
            var quiz = await _quizRepository.GetById<QuizAggregate>(id);
            quiz.Close();
            await _quizRepository.Save(quiz);
        }
    }
}
