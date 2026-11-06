namespace RealTimeAuction.Application.Common.Exceptions;

/// <summary>Disputa de concorrência perdida: outro participante alterou o recurso primeiro (HTTP 409).</summary>
public class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message) { }
}
