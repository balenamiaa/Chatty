using System;
using System.Collections.Generic;

namespace Chatty.Shared.Models.Notifications;

public sealed record NotificationPreferences(
    bool EnablePushNotifications,
    bool EnableMessageNotifications,
    bool EnableDirectMessageNotifications,
    bool EnableMentionNotifications,
    bool EnableCallNotifications,
    bool EnableSoundEffects,
    TimeSpan? QuietHoursStart = null,
    TimeSpan? QuietHoursEnd = null,
    HashSet<Guid>? MutedChannels = null,
    HashSet<Guid>? MutedUsers = null);
