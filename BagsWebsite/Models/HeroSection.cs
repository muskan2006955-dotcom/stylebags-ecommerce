using System;
using System.Collections.Generic;

namespace BagsWebsite.Models;

public partial class HeroSection
{
    public int Id { get; set; }

    public string ImageUrl { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }
}
