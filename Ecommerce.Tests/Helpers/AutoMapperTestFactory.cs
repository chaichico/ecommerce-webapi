using AutoMapper;
using Mappings;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Tests.Helpers;

public static class AutoMapperTestFactory
{
    public static IMapper CreateMapper()
    {
        ILoggerFactory loggerFactory = LoggerFactory.Create(builder => { });
        MapperConfiguration configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<OrderProfile>();
            cfg.AddProfile<UserProfile>();
        }, loggerFactory);

        configuration.AssertConfigurationIsValid();
        return configuration.CreateMapper();
    }
}