using LearningAgent.Api.Models.Conversation;
using LearningAgent.Api.Services.Conversation;
using System.Collections.Concurrent;

namespace LearningAgent.Api.Services.Memory;

public class MemoryService : IMemoryService
{
    private readonly IConversationContextFactory _contextFactory;
    private readonly IConversationStore _conversationStore;
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public MemoryService(IConversationContextFactory contextFactory, IConversationStore conversationStore)
    {
        _contextFactory = contextFactory;
        _conversationStore = conversationStore;
    }


    public ConversationContext GetOrCreate(Guid conversationId)
    {
        var context = _conversationStore.Get(conversationId);

        if(context is not null)
        {
            return context;
        }

        context = _contextFactory.Create(conversationId);

        _conversationStore.Save(context);


        return context;
    }

    public void Save(ConversationContext context)
    {
        _conversationStore.Save(context);
    }

    private SemaphoreSlim GetLock(Guid conversationId)
    {
        return _locks.GetOrAdd(conversationId, _ => new SemaphoreSlim(1, 1));
    }

    public async Task<T> ExecuteAsync<T>(Guid conversationId, Func<Task<T>> operation)
    {
        var semaphore = GetLock(conversationId);
        Console.WriteLine($"[{conversationId}] Waiting for lock");

        await semaphore.WaitAsync();

        Console.WriteLine($"[{conversationId}] Acquired lock");

        try
        {
            return await operation();

        }
        finally
        {
            semaphore.Release();
            Console.WriteLine($"[{conversationId}] Released lock");
        }
    }
}