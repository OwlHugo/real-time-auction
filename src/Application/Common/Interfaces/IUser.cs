namespace RealTimeAuction.Application.Common.Interfaces;

public interface IUser
{
    string? Id { get; }

    /// <summary>Nome público (apelido ou parte local do e-mail), nunca o e-mail completo.</summary>
    string? DisplayName { get; }

    List<string>? Roles { get; }
}
