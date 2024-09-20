using Microsoft.AspNetCore.Mvc;
using projectoFInal.Models;
using System.Security.Cryptography;
using System.Text;

using System.Data;
using Microsoft.Data.SqlClient;
using System.Collections;
using Newtonsoft.Json;

namespace projectoFInal.Controllers
{
    //AQUI ESTARE CONSTRUYENDO EL CONTROLADOR PARA LOS REGISTROS Y LOGINGS
    public class AccesoController : Controller
    {
        static string cadena = "server=localhost; database=DP; integrated security=true;";

        public IActionResult Login()
        {
            return View();
        }
        public IActionResult Registrar()
        {
            return View();
        }
        public ActionResult Hub()
        {
            // Verificar si el usuario está en sesión
            var usuarioJson = HttpContext.Session.GetString("usuario");
            if (string.IsNullOrEmpty(usuarioJson))
            {
                // Si no hay usuario en sesión, redirigir al login
                return RedirectToAction("Login", "Acceso");
            }

            // Convertir el JSON de la sesión en un objeto de usuario
            users oUsuario = JsonConvert.DeserializeObject<users>(usuarioJson);

            // Aquí podrías pasar información adicional al HUB, como el nombre del usuario
            ViewBag.NombreUsuario = oUsuario.email; // o algún otro dato relevante

            return View();
        }

        [HttpPost]
        public IActionResult Registrar(users oUsuario)
        {
            bool registrado;
            string mensaje;

            oUsuario.tipoUser = 0;

            // Verifica si las contraseñas coinciden
            if (oUsuario.contraseña != oUsuario.confirmarContraseña)
            {
                ViewData["MensajeError"] = "Las contraseñas no coinciden";
                return View();
            }

            // Convierte la contraseña a SHA256
            oUsuario.contraseña = ConvertirSha256(oUsuario.contraseña);

            // Conectar a la base de datos
            using (SqlConnection cn = new SqlConnection(cadena))
            {
                // Configurar el comando para llamar al procedimiento almacenado
                SqlCommand cmd = new SqlCommand("RegistrarUsuario", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // Pasar los parámetros al procedimiento almacenado
                cmd.Parameters.AddWithValue("email", oUsuario.email);
                cmd.Parameters.AddWithValue("contraseña", oUsuario.contraseña);
                cmd.Parameters.AddWithValue("tipoUser", oUsuario.tipoUser);
                cmd.Parameters.Add("registrado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("mensaje", SqlDbType.VarChar, 100).Direction = ParameterDirection.Output;

                // Ejecutar el comando
                cn.Open();
                cmd.ExecuteNonQuery();

                // Obtener los valores de los parámetros de salida
                registrado = Convert.ToBoolean(cmd.Parameters["registrado"].Value);
                mensaje = cmd.Parameters["mensaje"].Value.ToString();
            }

            // Manejar el resultado del registro
            if (registrado)
            {
                return RedirectToAction("Login", "Acceso");
            }
            else
            {
                ViewData["MensajeError"] = mensaje; // Asegúrate de que el mensaje se muestra correctamente
                return View();
            }
        }
        [HttpPost]
        //AQUI ESTA GUARDADO EL METODO PARA LOGEARSE
        public ActionResult Login(users oUsuario)
        {
            oUsuario.contraseña = ConvertirSha256(oUsuario.contraseña); //aqui cifro la contraseña porque guardo las contras cifradas
            //conectar
            using (SqlConnection cn = new SqlConnection(cadena))
            {
                //aqui llamo a mi procedimiento de la base de datos validar user
                SqlCommand cmd = new SqlCommand("validarUser", cn);
                //aqui añado los valores al procedimiento
                cmd.Parameters.AddWithValue("email", oUsuario.email);
                cmd.Parameters.AddWithValue("contraseña", oUsuario.contraseña);
                //ejecuto
                cmd.CommandType = CommandType.StoredProcedure;

                cn.Open();
                cmd.ExecuteNonQuery();
                oUsuario.IdUsuario = Convert.ToInt32(cmd.ExecuteScalar().ToString());
            }
            if (oUsuario.IdUsuario != 0)
            {


                string usuarioJson = JsonConvert.SerializeObject(oUsuario);

                HttpContext.Session.SetString("usuario", usuarioJson);

                return RedirectToAction("Hub", "Acceso");
            }
            else
            {
                ViewData["MensajeError"] = "Usuario no encontrado o contraseña incorrecta.";
                return View();
            }
        }

        //ACA ESTA EL METODO PARA CIFRAR CONTRASEÑAS
        public static string ConvertirSha256(string texto)
        {
            //using System.Text;
            //USAR LA REFERENCIA DE "System.Security.Cryptography"

            StringBuilder Sb = new StringBuilder();
            using (SHA256 hash = SHA256Managed.Create())
            {
                Encoding enc = Encoding.UTF8;
                byte[] result = hash.ComputeHash(enc.GetBytes(texto));

                foreach (byte b in result)
                    Sb.Append(b.ToString("x2"));
            }

            return Sb.ToString();
        }
    }
}
