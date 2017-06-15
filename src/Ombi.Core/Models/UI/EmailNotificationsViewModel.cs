﻿using System.Collections.Generic;
using Ombi.Settings.Settings.Models.Notifications;
using Ombi.Store.Entities;

namespace Ombi.Models.Notifications
{
    /// <summary>
    /// The view model for the notification settings page
    /// </summary>
    /// <seealso cref="Ombi.Settings.Settings.Models.Notifications.EmailNotificationSettings" />
    public class EmailNotificationsViewModel : EmailNotificationSettings
    {
        /// <summary>
        /// Gets or sets the notification templates.
        /// </summary>
        /// <value>
        /// The notification templates.
        /// </value>
        public List<NotificationTemplates> NotificationTemplates { get; set; }
    }
}