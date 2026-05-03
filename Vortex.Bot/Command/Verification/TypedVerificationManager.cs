using System.Collections.Concurrent;

namespace Vortex.Bot.Command.Verification;

public static class TypedVerificationManager
{
    private static readonly ConcurrentDictionary<string, TypedVerification> PendingVerifications = new();

    public class TypedVerification
    {
        public required long UserId { get; set; }
        public required long GroupId { get; set; }
        public required string ActionType { get; set; }
        public required string ActionName { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required int TimeoutSeconds { get; set; }
        public object? Data { get; set; }
        public bool IsVerified { get; set; } = false;
        public bool IsCancelled { get; set; } = false;

        public bool IsExpired => DateTime.Now > CreatedAt.AddSeconds(TimeoutSeconds);
        public TimeSpan RemainingTime => CreatedAt.AddSeconds(TimeoutSeconds) - DateTime.Now;
    }

    public class VerificationResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public TypedVerification? Verification { get; set; }
    }

    private static string GetKey(long userId, long groupId, string actionType) =>
        $"{userId}_{groupId}_{actionType}";

    public static TypedVerification Create(
        long userId,
        long groupId,
        string actionType,
        string actionName,
        int timeoutSeconds = 60,
        object? data = null,
        string? verifyKey = null)
    {   
        if (timeoutSeconds < 5) timeoutSeconds = 5;
        if (timeoutSeconds > 300) timeoutSeconds = 300;

        var key = verifyKey ?? GetKey(userId, groupId, actionType);

        PendingVerifications.TryRemove(key, out _);

        var verification = new TypedVerification
        {
            UserId = userId,
            GroupId = groupId,
            ActionType = actionType,
            ActionName = actionName,
            CreatedAt = DateTime.Now,
            TimeoutSeconds = timeoutSeconds,
            Data = data
        };

        PendingVerifications[key] = verification;
        return verification;
    }

    public static VerificationResult Verify(long userId, long groupId, string actionType, string? verifyKey = null)
    {
        var key = verifyKey ?? GetKey(userId, groupId, actionType);

        if (!PendingVerifications.TryGetValue(key, out var verification) || verification == null)
        {
            return new VerificationResult
            {
                Success = false,
                Message = $"没有待确认的{GetActionTypeDisplayName(actionType)}操作"
            };
        }

        if (verification.IsExpired)
        {
            PendingVerifications.TryRemove(key, out _);
            return new VerificationResult
            {
                Success = false,
                Message = $"{verification.ActionName}确认已超时，请重新发起"
            };
        }

        if (verification.IsCancelled)
        {
            PendingVerifications.TryRemove(key, out _);
            return new VerificationResult
            {
                Success = false,
                Message = $"{verification.ActionName}已被取消"
            };
        }

        verification.IsVerified = true;
        PendingVerifications.TryRemove(key, out _);

        return new VerificationResult
        {
            Success = true,
            Message = $"{verification.ActionName}确认成功",
            Verification = verification
        };
    }

    public static VerificationResult Cancel(long userId, long groupId, string actionType)
    {
        var key = GetKey(userId, groupId, actionType);

        if (!PendingVerifications.TryGetValue(key, out var verification) || verification == null)
        {
            return new VerificationResult
            {
                Success = false,
                Message = $"没有待取消的{GetActionTypeDisplayName(actionType)}操作"
            };
        }

        if (verification.IsExpired)
        {
            PendingVerifications.TryRemove(key, out _);
            return new VerificationResult
            {
                Success = false,
                Message = $"{verification.ActionName}已超时"
            };
        }

        verification.IsCancelled = true;
        PendingVerifications.TryRemove(key, out _);

        return new VerificationResult
        {
            Success = true,
            Message = $"{verification.ActionName}已取消"
        };
    }

    public static TypedVerification? GetPending(long userId, long groupId, string actionType, string? verifyKey = null)
    {
        var key = verifyKey ?? GetKey(userId, groupId, actionType);
        if (PendingVerifications.TryGetValue(key, out var verification))
        {
            if (verification.IsExpired)
            {
                PendingVerifications.TryRemove(key, out _);
                return null;
            }
            return verification;
        }
        return null;
    }

    public static List<TypedVerification> GetAllPending(long userId, long groupId)
    {
        var prefix = $"{userId}_{groupId}_";
        return [.. PendingVerifications
            .Where(kv => kv.Key.StartsWith(prefix))
            .Select(kv => kv.Value)
            .Where(v => !v.IsExpired)];
    }

    public static async Task StartTimeoutMonitorAsync(
        long userId,
        long groupId,
        string actionType,
        Func<TypedVerification, Task> onTimeout,
        string? verifyKey = null)
    {
        var key = verifyKey ?? GetKey(userId, groupId, actionType);

        if (!PendingVerifications.TryGetValue(key, out var verification) || verification == null)
            return;

        await Task.Delay(TimeSpan.FromSeconds(verification.TimeoutSeconds));

        if (PendingVerifications.TryGetValue(key, out var pending))
        {
            if (!pending.IsVerified && !pending.IsCancelled && pending.IsExpired)
            {
                PendingVerifications.TryRemove(key, out _);
                await onTimeout(pending);
            }
        }
    }

    private static string GetActionTypeDisplayName(string actionType)
    {
        return actionType.ToLower() switch
        {
            "delete" => "删除",
            "transfer" => "转账",
            "reset" => "重置",
            "buy" => "购买",
            _ => actionType
        };
    }
}
