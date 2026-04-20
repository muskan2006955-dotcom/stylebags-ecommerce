using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;

namespace BagsWebsite.Areas.Admin.Models
{
    public class ProductViewModel
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string? Description { get; set; }
        [Required]
        public decimal Price { get; set; }
        [Required]
        public string? Color { get; set; }
        [Required]
        public int Stock { get; set; }
        [Required]
        public int Discount { get; set; }

        [Display(Name = "Creation Date")]
        public DateTime? CreatedAt { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public List<ProductImageVariant> ImageVariants { get; set; } = new List<ProductImageVariant>();
    }

    public class ProductImageVariant
    {
        // IS LINE KO DEKHEIN: Ab ye multiple files handle karega
        public List<IFormFile>? Files { get; set; }
        public string? ColorCode { get; set; }
    }
}