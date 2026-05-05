namespace SpotRent.Api.Logging;

internal static class IotControllerEventIds
{
    internal static readonly EventId RegisterDeviceAttempt = new(9000, nameof(RegisterDeviceAttempt));

    internal static readonly EventId RegisterDeviceInvalid = new(9001, nameof(RegisterDeviceInvalid));

    internal static readonly EventId RegisterDeviceSuccess = new(9002, nameof(RegisterDeviceSuccess));

    internal static readonly EventId RegisterDeviceFailure = new(9003, nameof(RegisterDeviceFailure));

    internal static readonly EventId DeviceStatusUpdateAttempt = new(9004, nameof(DeviceStatusUpdateAttempt));

    internal static readonly EventId DeviceStatusUpdateInvalid = new(9005, nameof(DeviceStatusUpdateInvalid));

    internal static readonly EventId DeviceStatusUpdateSuccess = new(9006, nameof(DeviceStatusUpdateSuccess));

    internal static readonly EventId DeviceStatusUpdateFailure = new(9007, nameof(DeviceStatusUpdateFailure));

    internal static readonly EventId UnlockAttempt = new(9008, nameof(UnlockAttempt));

    internal static readonly EventId UnlockInvalid = new(9009, nameof(UnlockInvalid));

    internal static readonly EventId UnlockSuccess = new(9010, nameof(UnlockSuccess));

    internal static readonly EventId UnlockFailure = new(9011, nameof(UnlockFailure));

    internal static readonly EventId LockAttempt = new(9012, nameof(LockAttempt));

    internal static readonly EventId LockInvalid = new(9013, nameof(LockInvalid));

    internal static readonly EventId LockSuccess = new(9014, nameof(LockSuccess));

    internal static readonly EventId LockFailure = new(9015, nameof(LockFailure));

    internal static readonly EventId GenerateQrAttempt = new(9016, nameof(GenerateQrAttempt));

    internal static readonly EventId GenerateQrInvalid = new(9017, nameof(GenerateQrInvalid));

    internal static readonly EventId GenerateQrSuccess = new(9018, nameof(GenerateQrSuccess));

    internal static readonly EventId GenerateQrFailure = new(9019, nameof(GenerateQrFailure));

    internal static readonly EventId GenerateQrFailureOwner = new(9020, nameof(GenerateQrFailureOwner));

    internal static readonly EventId GenerateQrSuccessOwner = new(9021, nameof(GenerateQrSuccessOwner));

    internal static readonly EventId GenerateQrAttemptOwner = new(9022, nameof(GenerateQrAttemptOwner));

    internal static readonly EventId GetDeviceIdAttempt = new(9023, nameof(GetDeviceIdAttempt));

    internal static readonly EventId GetDeviceIdInvalid = new(9024, nameof(GetDeviceIdInvalid));

    internal static readonly EventId GetDeviceIdSuccess = new(9025, nameof(GetDeviceIdSuccess));

    internal static readonly EventId GetDeviceIdFailure = new(9026, nameof(GetDeviceIdFailure));
}
