namespace eShop.EventBusRabbitMQ;

public class EventBusOptions
{
    public string SubscriptionClientName { get; set; }
    public int RetryCount { get; set; } = 10;
    public bool DisableHeartbeat { get; set; }
}
