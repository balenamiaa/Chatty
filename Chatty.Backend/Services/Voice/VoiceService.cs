using Chatty.Backend.Realtime.Events;using Chatty.Shared.Models.Calls;using Chatty.Shared.Models.Common;namespace Chatty.Backend.Services.Voice;public class VoiceService{    private readonly ILogger<VoiceService> _logger;    private readonly IEventBus _eventBus;    public VoiceService(ILogger<VoiceService> logger, IEventBus eventBus)    {        _logger = logger;        _eventBus = eventBus;    }    public async Task<Result<CallDto>> InitiateCallAsync(        Guid initiatorId,        CreateCallRequest request,        CancellationToken ct = default)    {
        // ... existing call creation code ...


        try        {
            // ... create call and save to database ...


            var callDto = call.ToDto();


            // Publish call started event

            await _eventBus.PublishAsync(new CallStartedEvent(callDto), ct);            return Result<CallDto>.Success(callDto);        }        catch (Exception ex)        {            _logger.LogError(ex, "Failed to initiate call");            return Result<CallDto>.Failure(Error.Internal("Failed to initiate call"));        }    }}