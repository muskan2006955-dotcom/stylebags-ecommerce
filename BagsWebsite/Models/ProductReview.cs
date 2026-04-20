using System;
using System.Collections.Generic;

namespace BagsWebsite.Models;

public partial class ProductReview
{
    public int Id { get; set; }

    public string CustomerName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Message { get; set; } = null!;

    public int Rating { get; set; }

    public string? ImageUrl { get; set; }

    public bool? IsApproved { get; set; }

    public DateTime? CreatedAt { get; set; }
}
