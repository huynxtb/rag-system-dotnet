using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using RagSystem.Application.Abstractions;
using RagSystem.Infrastructure.Options;

namespace RagSystem.Infrastructure.Rag;

public class OpenAiChatCompletionService : IChatCompletionService
{
    private readonly ChatClient _client;

    public OpenAiChatCompletionService(IOptions<OpenAiOptions> opts)
    {
        _client = new OpenAIClient(opts.Value.ApiKey).GetChatClient(opts.Value.ChatModel);
    }

    public async Task<string> CompleteAsync(IReadOnlyList<ChatTurn> messages, CancellationToken ct)
    {
        var mapped = messages.Select<ChatTurn, ChatMessage>(m => m.Role switch
        {
            "system" => new SystemChatMessage(m.Content),
            "assistant" => new AssistantChatMessage(m.Content),
            _ => new UserChatMessage(m.Content)
        }).ToList();

        var result = await _client.CompleteChatAsync(mapped, cancellationToken: ct);
        return string.Concat(result.Value.Content.Select(p => p.Text));
    }
}
