using System;
using System.Collections.Generic;

namespace KPZ.Subscriptions.Scaffolded.Models;

public partial class SubscriptionTag
{
    public int SubscriptionId { get; set; }

    public int TagId { get; set; }

    public DateTime AddedAt { get; set; }

    public string? Note { get; set; }

    public virtual Subscription Subscription { get; set; } = null!;

    public virtual Tag Tag { get; set; } = null!;
}
