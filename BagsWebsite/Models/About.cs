using System;
using System.Collections.Generic;

namespace BagsWebsite.Models;

public partial class About
{
    public int Id { get; set; }

    public string Heading { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string? ImageUrl { get; set; }
}
