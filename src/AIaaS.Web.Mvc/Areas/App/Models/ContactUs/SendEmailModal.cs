using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AIaaS.Web.Areas.App.Models.ContactUs
{
    public class SendEmailModal
    {
        [Required]
        [MaxLength(256)]
        public string Name { get; set; }

        [Required]
        [MaxLength(256)]
        public string EMail { get; set; }

        [Required]
        [MaxLength(65536)]
        public string Message { get; set; }

    }
}
