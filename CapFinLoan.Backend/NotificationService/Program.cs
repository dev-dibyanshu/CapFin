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
        Console.WriteLine("========================================");
        Console.WriteLine("=== Notification Service Started ===");
        Console.WriteLine("========================================");
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
        Console.WriteLine("\nListening on queues:");
        Console.WriteLine("  - application-submitted");
        Console.WriteLine("  - password-reset");
        Console.WriteLine();

        var factory = new ConnectionFactory { HostName = "localhost" };
        
        try
        {
            await using var connection = await factory.CreateConnectionAsync();
            
            // Create two channels for two queues
            await using var applicationChannel = await connection.CreateChannelAsync();
            await using var passwordResetChannel = await connection.CreateChannelAsync();

            // Declare application-submitted queue
            await applicationChannel.QueueDeclareAsync(
                queue: "application-submitted",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Declare password-reset queue
            await passwordResetChannel.QueueDeclareAsync(
                queue: "password-reset",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            Console.WriteLine("[*] Connected to RabbitMQ");
            Console.WriteLine("[*] Ready to process messages\n");

            // Consumer for application-submitted queue
            var applicationConsumer = new AsyncEventingBasicConsumer(applicationChannel);
            applicationConsumer.ReceivedAsync += async (model, ea) =>
            {
                await HandleApplicationSubmittedAsync(applicationChannel, ea);
            };

            // Consumer for password-reset queue
            var passwordResetConsumer = new AsyncEventingBasicConsumer(passwordResetChannel);
            passwordResetConsumer.ReceivedAsync += async (model, ea) =>
            {
                await HandlePasswordResetAsync(passwordResetChannel, ea);
            };

            // Start consuming from both queues
            await applicationChannel.BasicConsumeAsync(
                queue: "application-submitted",
                autoAck: false,
                consumer: applicationConsumer);

            await passwordResetChannel.BasicConsumeAsync(
                queue: "password-reset",
                autoAck: false,
                consumer: passwordResetConsumer);

            Console.WriteLine("Press [Enter] to exit.");
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to connect to RabbitMQ: {ex.Message}");
            Console.WriteLine("Make sure RabbitMQ is running on localhost:5672");
        }
    }

    private static async Task HandleApplicationSubmittedAsync(IChannel channel, BasicDeliverEventArgs ea)
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        
        try
        {
            var eventData = JsonSerializer.Deserialize<ApplicationSubmittedEvent>(message);
            
            if (eventData != null)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 📨 [APPLICATION] Received event for: {eventData.Email}");
                
                bool emailSent = await SendApplicationEmailWithRetryAsync(eventData);
                
                if (emailSent)
                {
                    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✅ [APPLICATION] Email sent to {eventData.Email}");
                    Console.WriteLine($"    Application ID: {eventData.ApplicationId}");
                    Console.WriteLine($"    Message acknowledged\n");
                }
                else
                {
                    await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ❌ [APPLICATION] Failed after {MaxRetryAttempts} attempts");
                    Console.WriteLine($"    Message requeued\n");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ❌ [APPLICATION] ERROR: {ex.Message}");
            await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    private static async Task HandlePasswordResetAsync(IChannel channel, BasicDeliverEventArgs ea)
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        
        try
        {
            var eventData = JsonSerializer.Deserialize<PasswordResetRequestedEvent>(message);
            
            if (eventData != null)
            {
                Console.WriteLine("========================================");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 📨 [PASSWORD RESET] Received event");
                Console.WriteLine($"[QUEUE] Email: {eventData.Email}");
                Console.WriteLine($"[QUEUE] User: {eventData.UserName}");
                Console.WriteLine($"[QUEUE] Reset link length: {eventData.ResetLink.Length}");
                
                bool emailSent = await SendPasswordResetEmailWithRetryAsync(eventData);
                
                if (emailSent)
                {
                    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✅ [PASSWORD RESET] Email sent to {eventData.Email}");
                    Console.WriteLine($"[QUEUE] Message acknowledged");
                    Console.WriteLine("========================================\n");
                }
                else
                {
                    await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ❌ [PASSWORD RESET] Failed after {MaxRetryAttempts} attempts");
                    Console.WriteLine($"[QUEUE] Message requeued");
                    Console.WriteLine("========================================\n");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ❌ [PASSWORD RESET] ERROR: {ex.Message}");
            Console.WriteLine(ex.ToString());
            await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    private static async Task<bool> SendApplicationEmailWithRetryAsync(ApplicationSubmittedEvent eventData)
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
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ⚠️  [APPLICATION] Attempt {attempt}/{MaxRetryAttempts} failed: {ex.Message}");
                
                if (attempt < MaxRetryAttempts)
                {
                    Console.WriteLine($"    Retrying in {RetryDelayMilliseconds}ms...");
                    await Task.Delay(RetryDelayMilliseconds);
                }
            }
        }

        return false;
    }

    private static async Task<bool> SendPasswordResetEmailWithRetryAsync(PasswordResetRequestedEvent eventData)
    {
        for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            try
            {
                if (_emailService == null)
                {
                    throw new InvalidOperationException("Email service not initialized");
                }

                Console.WriteLine($"[EMAIL] Attempt {attempt}/{MaxRetryAttempts} - Sending to {eventData.Email}");
                
                await _emailService.SendPasswordResetEmailAsync(
                    eventData.Email,
                    eventData.UserName,
                    eventData.ResetLink);

                Console.WriteLine($"[EMAIL] ✅ Sent successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ⚠️  [PASSWORD RESET] Attempt {attempt}/{MaxRetryAttempts} failed");
                Console.WriteLine($"[EMAIL ERROR] {ex.Message}");
                
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
