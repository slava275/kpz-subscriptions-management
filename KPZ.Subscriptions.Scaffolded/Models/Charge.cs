using System;
using System.Collections.Generic;

namespace KPZ.Subscriptions.Scaffolded.Models;

public partial class Charge
{
    public int Id { get; set; }

    public int SubscriptionId { get; set; }

    public DateOnly DueDate { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? PaidAt { get; set; }

    public virtual Subscription Subscription { get; set; } = null!;
}
