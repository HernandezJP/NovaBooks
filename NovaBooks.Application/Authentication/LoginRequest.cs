using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NovaBooks.Application.Authentication
{
    public sealed class LoginRequest
    {
        [Required(ErrorMessage = "El usuario o correo electrónico es obligatorio.")]
        [StringLength(
        256,
        ErrorMessage = "El usuario o correo electrónico no puede superar los 256 caracteres.")]
        public string UserNameOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(
            100,
            MinimumLength = 6,
            ErrorMessage = "La contraseña debe contener entre 6 y 100 caracteres.")]
        public string Password { get; set; } = string.Empty;
    }
}
