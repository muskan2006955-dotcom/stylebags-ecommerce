using System;
using System.Collections.Generic;

namespace BagsWebsite.Models;

public partial class Payment
{
    public int Id { get; set; }

    public int? OrderId { get; set; }

    public string? PaymentMethod { get; set; }

    public string? PaymentStatus { get; set; }

    public DateTime? PaymentDate { get; set; }

    public virtual Order? Order { get; set; }
}
