using DXDocsMVC.Code;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace DXDocsMVC.Models
{
    public class LogOnModel
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "The value is required")]
        [RegularExpression("\\w+", ErrorMessage = "Invalid user name")]
        public string AccountName { get; set; }
        public string UserPassword { get; set; }
        public string ErrorText { get; set; }

    }
}