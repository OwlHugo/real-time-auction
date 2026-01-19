using MassTransit;
using Microsoft.Extensions.Logging;
using RealTimeAuction.Application.Auctions.Events;

namespace RealTimeAuction.Infrastructure.Messaging;

public class AuctionWonEventConsumer : IConsumer<AuctionWonEvent>
{
    private readonly ILogger<AuctionWonEventConsumer> _logger;

    public AuctionWonEventConsumer(ILogger<AuctionWonEventConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AuctionWonEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "🎉 Processando evento de leilão ganho: AuctionId={AuctionId}, WinnerId={WinnerId}",
            message.AuctionId,
            message.WinnerId
        );

        await Task.Run(() =>
        {
            _logger.LogInformation(
                "📧 [SIMULADO] Email enviado para {WinnerId}: Parabéns! Você ganhou o leilão #{AuctionId}",
                message.WinnerId,
                message.AuctionId
            );
        });

        _logger.LogInformation(
            "✅ Evento processado com sucesso: AuctionId={AuctionId}",
            message.AuctionId
        );
    }
}
