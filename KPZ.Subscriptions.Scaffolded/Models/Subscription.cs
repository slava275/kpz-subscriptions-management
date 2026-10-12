using System;
using System.Collections.Generic;

namespace KPZ.Subscriptions.Scaffolded.Models;

public partial class Subscription
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Notes { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public string Period { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly NextChargeDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public int RemindBeforeDays { get; set; }

    public string Status { get; set; } = null!;

    public int UserId { get; set; }

    public int CategoryId { get; set; }

    public int PaymentMethodId { get; set; }

    public byte[] Version { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateOnly? TrialEndsAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<Charge> Charges { get; set; } = new List<Charge>();

    public virtual PaymentMethod PaymentMethod { get; set; } = null!;

    public virtual ICollection<SubscriptionTag> SubscriptionTags { get; set; } = new List<SubscriptionTag>();

    public virtual User User { get; set; } = null!;
}
