using System.Text;
using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Io.Juenger.Autoconf
{
    /// <summary>
    ///     Extensions of Autofac's <see cref="IComponentContext"/>
    /// </summary>
    public static class ComponentContextExtensions
    {
        /// <summary>
        ///     Extension method that allows to resolve a XXXConfig class.
        /// </summary>
        /// <param name="componentContext">Instance of <see cref="IComponentContext"/></param>
        /// <typeparam name="TConfig">Type of the XXXConfig class</typeparam>
        /// <returns></returns>
        public static TConfig ResolveConfig<TConfig>(this IComponentContext componentContext) where TConfig : class, new()
        {
            var configuration = componentContext.Resolve<IConfiguration>();
            var sectionName = typeof(TConfig).Name;
            var configurationSection = configuration.GetSection(sectionName);
            var config = configurationSection.Get<TConfig>() ?? new TConfig();

            LogConfiguration(componentContext, config);
                    
            return config;
        }

        private static void LogConfiguration<TConfig>(IComponentContext componentContext, TConfig config) where TConfig : class, new()
        {
            ILogger<TConfig> logger;

            try
            {
                logger = componentContext.Resolve<ILogger<TConfig>>();
            }
            catch
            {
                return;
            }

            if(!logger.IsEnabled(LogLevel.Information)) return;

            var (logString, values) = BuildLog(config);
            
            #pragma warning disable CA2254
            logger.LogInformation(logString, values);
            #pragma warning restore CA2254
        }

        private static (string LogString, object[] values) BuildLog<TConfig>(TConfig config) where TConfig : class, new()
        {
            var configType = config.GetType();

            var stringBuilder = new StringBuilder($"Configuration of '{configType.Name}':");

            var propertyValues = new List<object>();
            var properties = configType.GetProperties();
            foreach (var property in properties)
            {
                var propertyValue = property.GetValue(config);
                if(propertyValue == null) continue;

                propertyValues.Add(propertyValue);
                stringBuilder.Append($" {property.Name}={{{property.Name}}}");
            }

            var logString = stringBuilder.ToString();

            return (logString, propertyValues.ToArray());
        }
    }
}