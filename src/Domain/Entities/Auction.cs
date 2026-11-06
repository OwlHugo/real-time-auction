using System.ComponentModel.DataAnnotations;
using RealTimeAuction.Domain.Enums;

namespace RealTimeAuction.Domain.Entities;

public class Auction : BaseAuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal StartingPrice { get; set; }
    public decimal? ReservePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public AuctionStatus Status { get; set; }

    public string? WinnerId { get; set; }
    public string? WinnerName { get; set; }

    public string SellerId { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;

    /// <summary>
    /// Relógio lógico do leilão: incrementa a cada lance aceito.
    /// Os celulares usam este número para ordenar eventos e detectar mensagens
    /// perdidas ou fora de ordem (se chegar seq N+2 sem N+1, o cliente ressincroniza).
    /// </summary>
    public long Sequence { get; set; }

    public ICollection<Bid> Bids { get; set; } = new List<Bid>();

    /// <summary>
    /// Token de concorrência otimista. Todo UPDATE do leilão inclui
    /// "WHERE RowVersion = @versaoLida"; se outro nó/usuário alterou antes,
    /// nenhuma linha é afetada e o EF lança DbUpdateConcurrencyException.
    /// </summary>
    [ConcurrencyCheck]
    public Guid RowVersion { get; set; } = Guid.NewGuid();

    public bool IsActive(DateTime now) =>
        Status == AuctionStatus.Active && now >= StartTime && now <= EndTime;

    public bool IsActive() => IsActive(DateTime.UtcNow);

    public bool CanPlaceBid(decimal bidAmount) => IsActive() && bidAmount > CurrentPrice;
}
