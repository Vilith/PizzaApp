namespace PizzaApp.Api.Services;

public class OrderingOptions
{
    public bool LockAfterDeadline { get; set; }
}

public sealed class OrderException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
