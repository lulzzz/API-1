﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Aiursoft.API.Models.UserViewModels
{
    public class ForgotPasswordViaEmailViewModel
    {
        public bool ModelStateValid { get; set; } = true;
        [Required]
        public string Email { get; set; }
    }
}
