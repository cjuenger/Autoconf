using Autofac;
using Autofac.Builder;

namespace Io.Juenger.Autoconf
{
    /// <summary>
    ///     Extensions of Autofac's <see cref="ContainerBuilder"/>
    /// </summary>
    public static class ContainerBuilderExtensions
    {
        /// <summary>
        ///     Extension method that allows to register a XXXConfig class.
        /// </summary>
        /// <param name="containerBuilder">Instance of <see cref="ContainerBuilder"/></param>
        /// <typeparam name="TConfig">Type of the XXXConfig class</typeparam>
        /// <returns></returns>
        public static IRegistrationBuilder<TConfig, SimpleActivatorData, SingleRegistrationStyle> 
            RegisterConfiguration<TConfig>(this ContainerBuilder containerBuilder)  where TConfig : class, new()
        {
            return containerBuilder.Register(c => c.ResolveConfig<TConfig>());
        }
    }
}