using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PizzaApp.Models
{
    public class PizzaOrderInput
    {
        [Required(ErrorMessage = "Du måste ange ett namn.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Du måste ange en pizza.")]
        public string Pizza { get; set; } = string.Empty;

        public string? Comment { get; set; }

    }
}
