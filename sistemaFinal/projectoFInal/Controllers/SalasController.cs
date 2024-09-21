using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using projectoFInal.Models;
using System.Data;

namespace projectoFInal.Controllers
{
    public class SalasController : Controller
    {
        static string cadena = "server=localhost; database=DP; integrated security=true;";

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        //procedimiento para guardar todo los datos de la sala en una lista
        public JsonResult ObtenerSalas()
        {
            List<Sala> listaSalas = new List<Sala>();

            using (SqlConnection cn = new SqlConnection(cadena))
            {
                SqlCommand cmd = new SqlCommand("ObtenerSalas", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    Sala sala = new Sala
                    {
                        Id = Convert.ToInt32(dr["IdSala"]), 
                        Nombre = dr["Nombre"].ToString(),
                        Ubicacion = dr["Ubicacion"].ToString(),
                        Capacidad = Convert.ToInt32(dr["Capacidad"]),
                        HoraInicio = TimeSpan.Parse(dr["HoraInicio"].ToString()),
                        HoraFin = TimeSpan.Parse(dr["HoraFin"].ToString())

                    };
                    listaSalas.Add(sala);
                }
            }

            return Json(listaSalas);
        }
    }

}
}
