namespace SmsBridge.Abstractions;

public enum SmsDeliveryStatus
{
    Unknown = 0,
    Accepted = 1,
    Queued = 2,
    Sent = 3,
    Delivered = 4,
    Failed = 5,
    Undelivered = 6
}
