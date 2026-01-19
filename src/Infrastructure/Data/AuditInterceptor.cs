using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RealTimeAuction.Application.Common.Interfaces;
using RealTimeAuction.Domain.Common;

namespace RealTimeAuction.Infrastructure.Data;

public class AuditLogEntry
{
    public int Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? ChangedBy { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public string Action { get; set; } = string.Empty;
}

public class AuditInterceptor
{
    private readonly ILogger<AuditInterceptor> _logger;
    private readonly IUser _currentUser;

    public AuditInterceptor(ILogger<AuditInterceptor> logger, IUser currentUser)
    {
        _logger = logger;
        _currentUser = currentUser;
    }

    public void LogChanges(DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                var entityName = entry.Entity.GetType().Name;
                var entityId = entry.Entity.Id;
                var userId = _currentUser.Id ?? "System";

                foreach (var property in entry.Properties)
                {
                    if (property.IsModified && !IsAuditProperty(property.Metadata.Name))
                    {
                        var oldValue = property.OriginalValue?.ToString() ?? "null";
                        var newValue = property.CurrentValue?.ToString() ?? "null";

                        if (oldValue != newValue)
                        {
                            _logger.LogInformation(
                                "AUDIT: Entity {EntityName} ID={EntityId} Property '{PropertyName}' changed from '{OldValue}' to '{NewValue}' by User {UserId}",
                                entityName,
                                entityId,
                                property.Metadata.Name,
                                oldValue,
                                newValue,
                                userId
                            );
                        }
                    }
                }
            }
            else if (entry.State == EntityState.Added)
            {
                _logger.LogInformation(
                    "AUDIT: Entity {EntityName} ID={EntityId} created by User {UserId}",
                    entry.Entity.GetType().Name,
                    entry.Entity.Id,
                    _currentUser.Id ?? "System"
                );
            }
            else if (entry.State == EntityState.Deleted)
            {
                _logger.LogInformation(
                    "AUDIT: Entity {EntityName} ID={EntityId} deleted by User {UserId}",
                    entry.Entity.GetType().Name,
                    entry.Entity.Id,
                    _currentUser.Id ?? "System"
                );
            }
        }
    }

    private static bool IsAuditProperty(string propertyName)
    {
        return propertyName
            is "Created"
                or "CreatedBy"
                or "LastModified"
                or "LastModifiedBy"
                or "RowVersion";
    }
}
