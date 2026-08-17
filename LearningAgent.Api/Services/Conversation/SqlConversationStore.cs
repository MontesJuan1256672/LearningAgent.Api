using Microsoft.Data.SqlClient;
using LearningAgent.Api.Models.Chat;
using LearningAgent.Api.Models.Conversation;

namespace LearningAgent.Api.Services.Conversation
{
    public class SqlConversationStore : IConversationStore
    {
        private readonly string _connectionString;

        public SqlConversationStore(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("LearningAgentDb")
                ?? throw new InvalidOperationException(
                    "Connection string 'LearningAgentDb' was not found.");
        }

        public ConversationContext? Get(Guid conversationId)
        {
            const string conversationSql = """
                SELECT ConversationId, SystemPrompt
                FROM Agent.Conversations
                WHERE ConversationId = @ConversationId;
                """;

            const string messagesSql = """
                SELECT Role, Content
                FROM Agent.ConversationMessages
                WHERE ConversationId = @ConversationId
                ORDER BY MessageId;
                """;

            using var connection = new SqlConnection(_connectionString);

            connection.Open();

            using var conversationCommand = new SqlCommand(conversationSql, connection);
                

            conversationCommand.Parameters.AddWithValue("@ConversationId", conversationId);               

            using var conversationReader = conversationCommand.ExecuteReader();
                
            if (!conversationReader.Read())
            {
                return null;
            }

            var context = new ConversationContext
            {
                ConversationId = conversationReader.GetGuid(conversationReader.GetOrdinal("ConversationId")),
                   
                SystemPrompt = conversationReader.GetString(conversationReader.GetOrdinal("SystemPrompt"))     
            };

            conversationReader.Close();

            using var messagesCommand = new SqlCommand(messagesSql, connection);
               
            messagesCommand.Parameters.AddWithValue("@ConversationId", conversationId);

            using var messagesReader = messagesCommand.ExecuteReader();

            while (messagesReader.Read())
            {
                context.Messages.Add(new ConversationMessage
                {
                    Role = messagesReader.GetString(messagesReader.GetOrdinal("Role")),
                    Content = messagesReader.GetString(messagesReader.GetOrdinal("Content"))
                });
            }

            return context;
        }

        public void Save(ConversationContext context)
        {
            const string conversationSql = """
                UPDATE Agent.Conversations
                SET
                    SystemPrompt = @SystemPrompt,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE ConversationId = @ConversationId;

                IF @@ROWCOUNT = 0
                BEGIN
                    INSERT INTO Agent.Conversations
                    (
                        ConversationId,
                        SystemPrompt
                    )
                    VALUES
                    (
                        @ConversationId,
                        @SystemPrompt
                    );
                END
                """;

            const string countMessagesSql = """
                SELECT COUNT(*)
                FROM Agent.ConversationMessages
                WHERE ConversationId = @ConversationId;
                """;

            const string insertMessageSql = """
                INSERT INTO Agent.ConversationMessages
                (
                    ConversationId,
                    Role,
                    Content
                )
                VALUES
                (
                    @ConversationId,
                    @Role,
                    @Content
                );
                """;

            using var connection = new SqlConnection(_connectionString);

            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Crear o actualizar la conversación
                using var conversationCommand = new SqlCommand(conversationSql, connection, transaction);
                conversationCommand.Parameters.AddWithValue("@ConversationId", context.ConversationId);
                conversationCommand.Parameters.AddWithValue("@SystemPrompt", context.SystemPrompt);
                conversationCommand.ExecuteNonQuery();

                // 2. Obtener cantidad de mensajes ya persistidos
                using var countMessagesCommand = new SqlCommand(countMessagesSql, connection, transaction);
                countMessagesCommand.Parameters.AddWithValue("@ConversationId", context.ConversationId);
                var existingMessageCount = Convert.ToInt32(countMessagesCommand.ExecuteScalar());

                // 3. Insertar solamente mensajes nuevos
                for (var i = existingMessageCount; i < context.Messages.Count; i++)
                {
                    var message = context.Messages[i];
                    using var insertMessageCommand = new SqlCommand(insertMessageSql, connection, transaction);
                    insertMessageCommand.Parameters.AddWithValue("@ConversationId", context.ConversationId);
                    insertMessageCommand.Parameters.AddWithValue("@Role", message.Role);
                    insertMessageCommand.Parameters.AddWithValue("@Content", message.Content);
                    insertMessageCommand.ExecuteNonQuery();
                }

                // 4. Confirmar toda la operación
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}

