namespace NotificationService;

public class ApplicationSubmittedEvent
{
    public Guid ApplicationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
}
