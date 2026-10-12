using System;
using System.Collections.Generic;

namespace KPZ.Subscriptions.Scaffolded.Models;

public partial class Tag
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<SubscriptionTag> SubscriptionTags { get; set; } = new List<SubscriptionTag>();

    public virtual User User { get; set; } = null!;
}
