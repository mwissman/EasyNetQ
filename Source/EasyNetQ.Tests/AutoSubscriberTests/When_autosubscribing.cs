﻿// ReSharper disable InconsistentNaming

using System;
using System.Collections;
using System.Threading.Tasks;
using EasyNetQ.AutoSubscribe;
using EasyNetQ.Loggers;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using RabbitMQ.Client;
using Rhino.Mocks;

namespace EasyNetQ.Tests.AutoSubscriberTests
{
    [TestFixture]
    public class When_autosubscribing
    {
        private MockBuilder mockBuilder;

        private const string expectedQueueName1 =
            "EasyNetQ.Tests.AutoSubscriberTests.When_autosubscribing+MessageA:EasyNetQ.Tests_my_app:d7617d39b90b6b695b90c630539a12e2";

        private const string expectedQueueName2 =
            "EasyNetQ.Tests.AutoSubscriberTests.When_autosubscribing+MessageB:EasyNetQ.Tests_MyExplicitId";

        private const string expectedQueueName3 =
            "EasyNetQ.Tests.AutoSubscriberTests.When_autosubscribing+MessageC:EasyNetQ.Tests_my_app:8b7980aa5e42959b4202e32ee442fc52";

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();
//            mockBuilder = new MockBuilder(x => x.Register<IEasyNetQLogger, ConsoleLogger>());

            var autoSubscriber = new AutoSubscriber(mockBuilder.Bus, "my_app");

            autoSubscriber.Subscribe(GetType().Assembly);
        }

        [Test]
        public void Should_have_declared_the_queues()
        {
            Action<string> assertQueueDeclared = queueName =>
                mockBuilder.Channels[0].AssertWasCalled(x => x.QueueDeclare(
                    Arg<string>.Is.Equal(queueName),
                    Arg<bool>.Is.Equal(true),
                    Arg<bool>.Is.Equal(false),
                    Arg<bool>.Is.Equal(false),
                    Arg<IDictionary>.Is.Anything
                    ));

            assertQueueDeclared(expectedQueueName1);
            assertQueueDeclared(expectedQueueName2);
            assertQueueDeclared(expectedQueueName3);
        }

        [Test]
        public void Should_have_started_consuming_from_the_correct_queues()
        {
            Action<int, string> assertConsumerStarted = (channelIndex, queueName) =>
                mockBuilder.Channels[channelIndex].AssertWasCalled(x => x.BasicConsume(
                    Arg<string>.Is.Equal(queueName),
                    Arg<bool>.Is.Equal(false), // NoAck
                    Arg<string>.Is.Anything,
                    Arg<IBasicConsumer>.Is.Anything));

            assertConsumerStarted(1, expectedQueueName1);
            assertConsumerStarted(2, expectedQueueName2);
            assertConsumerStarted(3, expectedQueueName3);
        }


        // Discovered by reflection over test assembly, do not remove.
        private class MyConsumer : IConsume<MessageA>, IConsume<MessageB>, IConsume<MessageC>
        {
            public void Consume(MessageA message)
            {
            }

            [AutoSubscriberConsumer(SubscriptionId = "MyExplicitId")]
            public void Consume(MessageB message)
            {
            }

            void IConsume<MessageC>.Consume(MessageC message)
            {
            }
        }

        // Discovered by reflection over test assembly, do not remove.
        private class MyAsyncConsumer : IConsumeAsync<MessageA>, IConsumeAsync<MessageB>, IConsumeAsync<MessageC>
        {
            public Task Consume(MessageA message)
            {
                throw new NotImplementedException();
            }

            [AutoSubscriberConsumer(SubscriptionId = "MyExplicitId")]
            public Task Consume(MessageB message)
            {
                throw new NotImplementedException();
            }

            public Task Consume(MessageC message)
            {
                throw new NotImplementedException();
            }
        }

        private class MessageA
        {
            public string Text { get; set; }
        }

        private class MessageB
        {
            public string Text { get; set; }
        }

        private class MessageC
        {
            public string Text { get; set; }
        }

    }
}

// ReSharper restore InconsistentNaming