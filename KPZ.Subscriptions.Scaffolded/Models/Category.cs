using System;
using System.Collections.Generic;

namespace KPZ.Subscriptions.Scaffolded.Models;

public partial class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
