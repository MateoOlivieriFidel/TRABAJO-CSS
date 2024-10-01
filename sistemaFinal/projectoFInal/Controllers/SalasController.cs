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

            try
            {
                using (SqlConnection cn = new SqlConnection(cadena))
                {
                    SqlCommand cmd = new SqlCommand("ObtenerSalas", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        // Verifica los datos obtenidos
                        int Id = Convert.ToInt32(dr["IdSala"]);
                        string Nombre = dr["Nombre"].ToString();
                        string Ubicacion = dr["Ubicacion"].ToString();
                        int Capacidad = Convert.ToInt32(dr["Capacidad"]);
                        TimeSpan HoraInicio = TimeSpan.Parse(dr["HoraInicio"].ToString());
                        TimeSpan HoraFin = TimeSpan.Parse(dr["HoraFin"].ToString());


                        Sala sala = new Sala
                        {
                            Id = Id,
                            Nombre = Nombre,
                            Ubicacion = Ubicacion,
                            Capacidad = Capacidad,
                            HoraInicio = HoraInicio,
                            HoraFin = HoraFin
                        };
                        listaSalas.Add(sala);
                    }
                }
            }
            catch (Exception ex)
            {
                // Maneja la excepción y registra el error
                Console.WriteLine($"Error al obtener salas: {ex.Message}");
            }

            return Json(listaSalas);
        }
    }


}
