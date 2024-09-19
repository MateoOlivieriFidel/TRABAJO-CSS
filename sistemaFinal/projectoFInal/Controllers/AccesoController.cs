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
        [HttpPost]
        //AQUI CREARE EL REGISTRO DE USUARIOS
        public IActionResult Registrar(users oUsuario)
        {
            bool registrado;
            string mensaje;

            if (oUsuario.contraseña == oUsuario.confirmarContraseña) //TIENES QUE CONFIRMAR CONTRASEÑAS
            {
                oUsuario.contraseña = ConvertirSha256(oUsuario.contraseña); //ACA ESTOY CONVIRTIENDO A LA CONTRASEÑA A SHA256 PARA CIFRARLA
            }
            else //EN CASO DE NO HABER CONFIRMADO BIEN LAS CONTRASEÑAS SIMPLEMENTE MONSTRAR UN MENSAJE Y SALIR
            {
                ViewData["Mensaje"] = "Las contraseñas no coinciden";
                return View();
            }
            //CONECTAR A BASE DE DATOS
            using (SqlConnection cn = new SqlConnection(cadena))
            {
                //COMANDO DE REGISTRO
                SqlCommand cmd = new SqlCommand("RegistrarUsuario", cn);
                //PASO PARAMETROS A LA BASE DE DATOS
                cmd.Parameters.AddWithValue("email", oUsuario.email);
                cmd.Parameters.AddWithValue("contraseña", oUsuario.contraseña);
                cmd.Parameters.AddWithValue("tipoUser", oUsuario.tipoUser);
                cmd.Parameters.Add("registrado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("mensaje", SqlDbType.VarChar, 100).Direction = ParameterDirection.Output;
                cmd.CommandType = CommandType.StoredProcedure;

                cn.Open(); 
                cmd.ExecuteNonQuery();

                registrado = Convert.ToBoolean(cmd.Parameters["registrado"].Value);
                mensaje = cmd.Parameters["mensaje"].Value.ToString();
            }
            ViewData["Mensaje"] = mensaje;
            if (registrado)
            {
                RedirectToAction("Logic", "Acceso");
            }
            else
            {
                return View();
            }
            return View();
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

                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewData["Mensaje"] = "usuario no encontrado";
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
