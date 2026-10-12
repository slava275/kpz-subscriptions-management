using System;
using System.Collections.Generic;

namespace KPZ.Subscriptions.Scaffolded.Models;

public partial class ExternalLogin
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Provider { get; set; } = null!;

    public string ProviderSubject { get; set; } = null!;

    public string? ProviderEmail { get; set; }

    public DateTime LinkedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
