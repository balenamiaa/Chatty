namespace Chatty.Shared.Models.Devices;

/// <summary>
///     Device keys for E2E encryption
/// </summary>
public class DeviceKeys
{
    /// <summary>
    ///     Device private key
    /// </summary>
    public byte[] PrivateKey { get; set; } = default!;

    /// <summary>
    ///     Device pre-key private
    /// </summary>
    public byte[] PreKeyPrivate { get; set; } = default!;
}
