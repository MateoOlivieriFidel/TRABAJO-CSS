namespace projectoFInal.Models
{
    public class users
    {
        public int IdUsuario { get; set; }
        public string email { get; set; }
        public string contraseña { get; set; }
        public int tipoUser { get; set; }

        public string confirmarContraseña { get; set; }
    }
}
