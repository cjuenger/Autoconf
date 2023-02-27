using System.Linq;
using Autofac;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace Io.Juenger.Autoconf.Tests
{
    [TestFixture]
    public class AutoconfTests
    {
        [Test]
        public void Register_And_Resolve_MyTestConfiguration()
        {
            var containerBuilder = new ContainerBuilder();

            RegisterConfiguration(containerBuilder);

            var container = containerBuilder.Build();

            var myTestConfig = container.Resolve<IMyTestConfig>();

            myTestConfig.Should().NotBeNull();
            myTestConfig.PropInt.Should().Be(123);
            myTestConfig.PropString.Should().Be("abc");
            myTestConfig.PropFloat.Should().Be(1.23f);
            myTestConfig.PropBool.Should().BeTrue();
        }

        [Test]
        public void Log_Resolving_MyTestConfiguration()
        {
            var containerBuilder = new ContainerBuilder();

            RegisterConfiguration(containerBuilder);

            var myTestConfigLogger = Substitute.For<ILogger<MyTestConfig>>();
            myTestConfigLogger.IsEnabled(LogLevel.Information).Returns(true);
            
            containerBuilder
                .Register(_ => myTestConfigLogger)
                .As<ILogger<MyTestConfig>>();

            var container = containerBuilder.Build();

            _ = container.Resolve<IMyTestConfig>();

            ShouldHaveReceivedLogMethod(
                myTestConfigLogger, 
                LogLevel.Information, 
                "Configuration of 'MyTestConfig': PropInt=123 PropString=abc PropFloat=1.23 PropBool=True",
                1);
        }
        
        [Test]
        public void Do_Not_Log_Resolving_MyTestConfiguration_If_LogLevel_Is_Below_Information()
        {
            var containerBuilder = new ContainerBuilder();

            RegisterConfiguration(containerBuilder);

            var myTestConfigLogger = Substitute.For<ILogger<MyTestConfig>>();
            myTestConfigLogger.IsEnabled(LogLevel.Information).Returns(false);
            
            containerBuilder
                .Register(_ => myTestConfigLogger)
                .As<ILogger<MyTestConfig>>();

            var container = containerBuilder.Build();

            _ = container.Resolve<IMyTestConfig>();

            ShouldHaveReceivedLogMethod(
                myTestConfigLogger, 
                LogLevel.Debug, 
                "Configuration of 'MyTestConfig': PropInt=123 PropString=abc PropFloat=1.23 PropBool=True",
                0);
        }

        private static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();
            return config;
        }

        private static void RegisterConfiguration(ContainerBuilder containerBuilder)
        {
            containerBuilder
                .Register(_ => InitConfiguration())
                .As<IConfiguration>();

            containerBuilder
                .RegisterConfiguration<MyTestConfig>()
                .As<IMyTestConfig>();
        }

        private static void ShouldHaveReceivedLogMethod(
            ILogger<MyTestConfig> myTestConfigLogger, 
            LogLevel logLevel, 
            string logMsg,
            int receiveCount)
        {
            var calls = myTestConfigLogger
                .ReceivedCalls()
                .Where(call => 
                    call.GetMethodInfo().Name == "Log" && 
                    call.GetArguments().Length >= 3 &&
                    (LogLevel) call.GetArguments()[0]! == logLevel &&
                    call.GetArguments()[2]!.ToString() == logMsg);

            calls.Should().HaveCount(receiveCount);
        }
    }
}

