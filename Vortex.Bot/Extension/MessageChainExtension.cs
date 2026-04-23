using Lagrange.Core.Message;
using Lagrange.Core.Message.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Vortex.Bot.Extension;

public static class MessageChainExtension
{
    public static IEnumerable<T> GetEnitys<T>(this MessageChain messages) where T : IMessageEntity
    {
        return messages.Where(m => m is T).Select(m => (T)m);
    }
}
