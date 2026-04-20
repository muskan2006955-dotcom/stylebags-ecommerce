using System;
using System.Collections.Generic;

namespace BagsWebsite.Models;

public partial class LoginLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? UserEmail { get; set; }

    public DateTime? LoginTime { get; set; }

    public string? IpAddress { get; set; }

    public string? Status { get; set; }

    public string? Browser { get; set; }

    public virtual User? User { get; set; }
}
