using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace RealTimeAuction.Web.Infrastructure.Idempotency;

public class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    private const string HeaderName = "X-Idempotency-Key";

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next
    )
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var idempotencyKey))
        {
            await next();
            return;
        }

        var cache = context.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();

        var cacheKey = $"idempotency-{idempotencyKey}";

        var cachedResult = await cache.GetStringAsync(cacheKey);
        if (cachedResult != null)
        {
            context.Result = new ObjectResult(null) { StatusCode = 201 };
            return;
        }

        var executedContext = await next();

        if (executedContext.Result is ObjectResult result && result.StatusCode is >= 200 and < 300)
        {
            await cache.SetStringAsync(
                cacheKey,
                "processed",
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
                }
            );
        }
    }
}
