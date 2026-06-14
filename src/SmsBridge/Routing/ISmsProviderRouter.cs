using SmsBridge.Abstractions;

namespace SmsBridge.Routing;

internal interface ISmsProviderRouter
{
    ISmsProvider Resolve(SmsRoutingContext context);
}
