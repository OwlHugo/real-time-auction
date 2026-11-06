using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using StackExchange.Redis;

namespace RealTimeAuction.Web.Infrastructure.Idempotency;

/// <summary>
/// Endpoint filter (Minimal API). Se o mesmo X-Idempotency-Key chegar de novo para o
/// mesmo usuário (retry de rede, duplo clique), devolve a resposta original em vez de
/// executar o lance outra vez. A reserva usa SET NX no Redis, então duas requisições
/// simultâneas em instâncias diferentes também não executam duas vezes.
/// </summary>
public sealed class IdempotencyFilter(IConnectionMultiplexer redis) : IEndpointFilter
{
    public const string HeaderName = "X-Idempotency-Key";
    private static readonly TimeSpan ResultTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan LockTtl = TimeSpan.FromSeconds(30);
    // Mesmo formato (camelCase) que o restante da API, para o reenvio ser idêntico ao original.
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new(System.Text.Json.JsonSerializerDefaults.Web);

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        var http = context.HttpContext;
        if (
            !http.Request.Headers.TryGetValue(HeaderName, out var header)
            || string.IsNullOrWhiteSpace(header)
        )
        {
            return await next(context);
        }

        var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anon";
        var resultKey = $"idem:{userId}:{header}";
        var lockKey = $"{resultKey}:lock";
        var db = redis.GetDatabase();

        var cached = await db.StringGetAsync(resultKey);
        if (cached.HasValue)
        {
            return Results.Content(cached!, "application/json", statusCode: 201);
        }

        if (!await db.StringSetAsync(lockKey, "1", LockTtl, When.NotExists))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Requisição em andamento",
                detail: "Esta mesma requisição já está sendo processada. Aguarde e tente novamente."
            );
        }

        try
        {
            var result = await next(context);

            // Só guarda sucessos (2xx) com corpo; erros de validação podem ser refeitos.
            if (
                result is IValueHttpResult valued
                && result is IStatusCodeHttpResult status
                && status.StatusCode is >= 200 and < 300
            )
            {
                var json = System.Text.Json.JsonSerializer.Serialize(valued.Value, JsonOptions);
                await db.StringSetAsync(resultKey, json, ResultTtl);
            }

            return result;
        }
        finally
        {
            await db.KeyDeleteAsync(lockKey);
        }
    }
}
