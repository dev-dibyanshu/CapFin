using CapFinLoan.Auth.Application.Interfaces;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace CapFinLoan.Auth.Infrastructure.Messaging;

public class RabbitMQPublisher : IMessagePublisher
{
    private readonly string _hostName;

    public RabbitMQPublisher(string hostName = "localhost")
    {
        _hostName = hostName;
    }

    public async Task PublishAsync<T>(string queueName, T message, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("========================================");
        Console.WriteLine($"[RABBITMQ] Publishing message to queue: {queueName}");
        Console.WriteLine($"[RABBITMQ] Message type: {typeof(T).Name}");
        
        var factory = new ConnectionFactory { HostName = _hostName };

        try
        {
            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            // Declare queue
            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            // Serialize message
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            Console.WriteLine($"[RABBITMQ] Message serialized (length: {body.Length} bytes)");

            // Publish message
            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                body: body,
                cancellationToken: cancellationToken);

            Console.WriteLine($"[RABBITMQ] ✅ Message published successfully");
            Console.WriteLine("========================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RABBITMQ] ❌ Failed to publish message: {ex.Message}");
            Console.WriteLine($"[RABBITMQ ERROR] {ex}");
            Console.WriteLine("========================================");
            throw new InvalidOperationException($"Failed to publish message to RabbitMQ: {ex.Message}", ex);
        }
    }
}
