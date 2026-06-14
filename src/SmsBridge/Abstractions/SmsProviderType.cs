namespace SmsBridge.Abstractions;

public enum SmsProviderType
{
    Unknown = 0,
    Twilio = 1,
    Vonage = 2,
    MessageBird = 3,
    AwsSns = 4,
    Infobip = 5,
    Termii = 6,
    Unifonic = 7,
    Sinch = 8,
    Plivo = 9
}
