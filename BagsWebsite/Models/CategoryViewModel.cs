using System.ComponentModel.DataAnnotations; // Ye line lazmi hai

namespace BagsWebsite.Areas.Admin.Models
{
    public class CategoryViewModel
    {
        [Required(ErrorMessage = "Category Name is required")]
        public string Name { get; set; } = null!;
    }
}