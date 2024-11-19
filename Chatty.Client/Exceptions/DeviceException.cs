namespace Chatty.Client.Exceptions;

/// <summary>
///     Exception thrown when a device operation fails
/// </summary>
public class DeviceException(
    string message,
    string code = "DEVICE_ERROR",
    Exception? innerException = null)
    : ChattyException(message, code, innerException)
{
    public static DeviceException StorageError(string message, Exception? innerException = null) =>
        new(
            $"Failed to access secure storage: {message}",
            "STORAGE_ERROR",
            innerException);

    public static DeviceException DeviceNotRegistered() =>
        new(
            "Device is not registered. Call RegisterDeviceAsync first.",
            "DEVICE_NOT_REGISTERED");

    public static DeviceException DeviceAlreadyRegistered() =>
        new(
            "Device is already registered.",
            "DEVICE_ALREADY_REGISTERED");

    public static DeviceException DeviceNotFound(Guid deviceId) =>
        new(
            $"Device {deviceId} not found",
            "DEVICE_NOT_FOUND");

    public static DeviceException KeyStorageError(string keyType, string operation, Exception? innerException = null) =>
        new(
            $"Failed to {operation} {keyType} key",
            "KEY_STORAGE_ERROR",
            innerException);
}
