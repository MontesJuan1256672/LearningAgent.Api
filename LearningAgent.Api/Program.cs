using LearningAgent.Api.Options;
using LearningAgent.Api.Services.Agent;
using LearningAgent.Api.Services.Chat;
using LearningAgent.Api.Services.Prompts;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("Ollama"));

builder.Services.AddHttpClient();

builder.Services.AddScoped<OpenAIService>();
builder.Services.AddScoped<IChatService, OllamaService>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<IPromptBuilder, PromptBuilder>();
builder.Services.AddScoped<ISystemPromptProvider, SystemPromptProvider>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
