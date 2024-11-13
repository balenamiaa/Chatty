using Chatty.Backend.Realtime.Events;
        // ... existing call creation code ...


        try
            // ... create call and save to database ...


            var callDto = call.ToDto();


            // Publish call started event

            await _eventBus.PublishAsync(new CallStartedEvent(callDto), ct);