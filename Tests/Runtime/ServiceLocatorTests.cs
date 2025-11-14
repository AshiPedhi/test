using NUnit.Framework;
using UnityEngine;
using ChunaVR.Core;

namespace ChunaVR.Tests
{
    /// <summary>
    /// ServiceLocator 단위 테스트
    /// Unity Test Framework 사용
    /// </summary>
    public class ServiceLocatorTests
    {
        // 테스트용 더미 클래스
        private class TestService
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }

        [SetUp]
        public void Setup()
        {
            // 각 테스트 전에 ServiceLocator 초기화
            ServiceLocator.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            // 각 테스트 후 정리
            ServiceLocator.Clear();
        }

        [Test]
        public void Register_ValidService_ShouldSucceed()
        {
            // Arrange
            var service = new TestService { Name = "Test", Value = 123 };

            // Act
            ServiceLocator.Register(service);

            // Assert
            Assert.IsTrue(ServiceLocator.IsRegistered<TestService>());
        }

        [Test]
        public void Get_RegisteredService_ShouldReturnSameInstance()
        {
            // Arrange
            var service = new TestService { Name = "Test", Value = 123 };
            ServiceLocator.Register(service);

            // Act
            var retrieved = ServiceLocator.Get<TestService>();

            // Assert
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(service, retrieved);
            Assert.AreEqual("Test", retrieved.Name);
            Assert.AreEqual(123, retrieved.Value);
        }

        [Test]
        public void Get_UnregisteredService_ShouldReturnNull()
        {
            // Act
            var retrieved = ServiceLocator.Get<TestService>();

            // Assert
            Assert.IsNull(retrieved);
        }

        [Test]
        public void Register_NullService_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => ServiceLocator.Register<TestService>(null));
        }

        [Test]
        public void Register_SameServiceTwice_ShouldReplace()
        {
            // Arrange
            var service1 = new TestService { Name = "First", Value = 1 };
            var service2 = new TestService { Name = "Second", Value = 2 };

            // Act
            ServiceLocator.Register(service1);
            ServiceLocator.Register(service2);
            var retrieved = ServiceLocator.Get<TestService>();

            // Assert
            Assert.AreEqual(service2, retrieved);
            Assert.AreEqual("Second", retrieved.Name);
        }

        [Test]
        public void Unregister_RegisteredService_ShouldRemove()
        {
            // Arrange
            var service = new TestService { Name = "Test", Value = 123 };
            ServiceLocator.Register(service);

            // Act
            ServiceLocator.Unregister<TestService>();

            // Assert
            Assert.IsFalse(ServiceLocator.IsRegistered<TestService>());
            Assert.IsNull(ServiceLocator.Get<TestService>());
        }

        [Test]
        public void Clear_MultipleServices_ShouldRemoveAll()
        {
            // Arrange
            ServiceLocator.Register(new TestService { Name = "Test1", Value = 1 });

            // Act
            ServiceLocator.Clear();

            // Assert
            Assert.IsFalse(ServiceLocator.IsRegistered<TestService>());
        }

        [Test]
        public void IsRegistered_WithRegisteredService_ShouldReturnTrue()
        {
            // Arrange
            var service = new TestService();
            ServiceLocator.Register(service);

            // Act
            bool isRegistered = ServiceLocator.IsRegistered<TestService>();

            // Assert
            Assert.IsTrue(isRegistered);
        }

        [Test]
        public void IsRegistered_WithoutRegisteredService_ShouldReturnFalse()
        {
            // Act
            bool isRegistered = ServiceLocator.IsRegistered<TestService>();

            // Assert
            Assert.IsFalse(isRegistered);
        }
    }
}
