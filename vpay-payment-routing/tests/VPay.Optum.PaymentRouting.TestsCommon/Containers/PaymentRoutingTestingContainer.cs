using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.Dsl;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace VPay.Optum.PaymentRouting.TestsCommon.Containers
{
    public class PaymentRoutingTestingContainer
    {
        private readonly IServiceCollection _collection;
        private IServiceProvider _provider;

        public PaymentRoutingTestingContainer(Action<IServiceCollection>? servicesAction = null)
        {
            _collection = new ServiceCollection();
            _collection.AddSingleton<Fixture>();
            servicesAction?.Invoke(_collection);
            _provider = _collection.BuildServiceProvider();
        }

        public T Get<T>() => _provider.GetService<T>();

        public T Mock<T>()
            where T : class
        {
            var mock = Substitute.For<T>();
            _collection.Replace(ServiceDescriptor.Singleton(_ => mock));
            _provider = _collection.BuildServiceProvider();
            return mock;
        }

        public T AddMock<T>(T mock)
            where T : class
        {
            _collection.Replace(ServiceDescriptor.Singleton(_ => mock));
            _provider = _collection.BuildServiceProvider();
            return mock;
        }

        public T Fake<T>() =>
            Get<Fixture>().Create<T>();

        public T Fake<T>(Func<ICustomizationComposer<T>, IPostprocessComposer<T>> composer) =>
            composer(Get<Fixture>().Build<T>()).Create();

        public IEnumerable<T> FakeMany<T>(int count = 3) =>
            Get<Fixture>().CreateMany<T>(count);

        public IEnumerable<T> FakeMany<T>(Func<ICustomizationComposer<T>, IPostprocessComposer<T>> composer, int count = 3) =>
            composer(Get<Fixture>().Build<T>()).CreateMany(count);
    }
}
