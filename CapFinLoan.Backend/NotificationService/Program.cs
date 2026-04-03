using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Configuration;
using NotificationService.Interfaces;
using NotificationService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace NotificationService;

class Program
{
    private static IEmailService? _emailService;
    private const int MaxRetryAttempts = 3;
    private const int RetryDelayMilliseconds = 2000;

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Notification Service Started ===");
        Console.WriteLine("Initializing...\n");

        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        // Setup DI
        var serviceProvider = new ServiceCollection()
            .AddSingleton(configuration.GetSection("EmailSettings").Get<EmailSettings>() ?? new EmailSettings())
            .AddSingleton<IEmailService, EmailService>()
            .BuildServiceProvider();

        _emailService = serviceProvider.GetRequiredService<IEmailService>();

        Console.WriteLine("[✓] Configuration loaded");
        Console.WriteLine("[✓] Email service initialized");
        Console.WriteLine("Waiting for ApplicationSubmitted events...\n");

        var factory = new ConnectionFactory { HostName = "localhost" };
        
        try
        {
            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            // Declare queue
            await channel.QueueDeclareAsync(
                queue: "application-submitted",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            Console.WriteLine("[*] Connected to RabbitMQ");
            Console.WriteLine("[*] Listening on queue: application-submitted\n");

            // Create consumer with manual acknowledgment
            var consumer = new AsyncEventingBasicConsumer(channel);
            
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                try
                {
                    // Deserialize event
                    var eventData = JsonSerializer.Deserialize<ApplicationSubmittedEvent>(message);
                    
                    if (eventData != null)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 📨 Received event for: {eventData.Email}");
                        
                        // Send email with retry logic
                        bool emailSent = await SendEmailWithRetryAsync(eventData);
                        
                        if (emailSent)
                        {
                            // Acknowledge message on success
                            await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✅ Email sent successfully to {eventData.Email}");
                            Console.WriteLine($"    Application ID: {eventData.ApplicationId}");
                            Console.WriteLine($"    Message acknowledged\n");
                        }
                        else
                        {
                            // Reject and requeue on failure
                            await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ❌ Failed to send email after {MaxRetryAttempts} attempts");
                            Console.WriteLine($"    Message requeued for retry\n");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ❌ ERROR processing message: {ex.Message}");
                    // Reject and requeue on exception
                    await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            // Start consuming with manual acknowledgment (autoAck = false)
            await channel.BasicConsumeAsync(
                queue: "application-submitted",
                autoAck: false,
                consumer: consumer);

            Console.WriteLine("Press [Enter] to exit.");
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to connect to RabbitMQ: {ex.Message}");
            Console.WriteLine("Make sure RabbitMQ is running on localhost:5672");
        }
    }

    private static async Task<bool> SendEmailWithRetryAsync(ApplicationSubmittedEvent eventData)
    {
        for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            try
            {
                if (_emailService == null)
                {
                    throw new InvalidOperationException("Email service not initialized");
                }

                await _emailService.SendApplicationSubmittedEmailAsync(
                    eventData.Email,
                    eventData.ApplicantName,
                    eventData.ApplicationId);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ⚠️  Attempt {attempt}/{MaxRetryAttempts} failed: {ex.Message}");
                
                if (attempt < MaxRetryAttempts)
                {
                    Console.WriteLine($"    Retrying in {RetryDelayMilliseconds}ms...");
                    await Task.Delay(RetryDelayMilliseconds);
                }
            }
        }

        return false;
    }
}
