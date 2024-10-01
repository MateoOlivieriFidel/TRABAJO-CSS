using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using projectoFInal.Models;
using System.Data;

namespace projectoFInal.Controllers
{
    public class ReservasController : Controller
    {
        static string connectionString = "server=localhost; database=DP; integrated security=true;";
        public IActionResult Index()
        {
            return View();
        }

        // METODO PARA VER

        [HttpGet]
        public IActionResult VerReservas(int idSala)
        {
            List<Reservas> reservas = new List<Reservas>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("VerReservas", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Parámetro de entrada
                    cmd.Parameters.AddWithValue("@IdSala", idSala);

                    try
                    {
                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Leer los valores de la fila y agregarlos a la lista
                                Reservas reserva = new Reservas
                                {
                                    IdReserva = Convert.ToInt32(reader["IdReserva"]),
                                    IdSala = Convert.ToInt32(reader["IdSala"]),
                                    HoraInicio = TimeSpan.Parse(reader["HoraInicio"].ToString()),
                                    HoraFin = TimeSpan.Parse(reader["HoraFin"].ToString()),
                                    NumeroAsistentes = Convert.ToInt32(reader["NumeroAsistentes"]),
                                    Prioridad = Convert.ToInt32(reader["Prioridad"]),
                                    IdUsuario = Convert.ToInt32(reader["IdUsuario"]),
                                    IdReservaGrupo = reader["IdReservaGrupo"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["IdReservaGrupo"])
                                };

                                reservas.Add(reserva);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Manejo de errores
                        return Json(new { success = false, message = $"Error al obtener reservas: {ex.Message}" });
                    }
                    finally
                    {
                        con.Close();
                    }
                }
            }

            return Json(reservas);  // Retorna la lista de reservas como JSON
        }


        // METODO PARA ELIMINAR

        [HttpPost]
        public JsonResult EliminarReserva(int idReserva)
        {
            string mensajeSalida;
            bool exito;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("EliminarReserva", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Parámetro de entrada
                    cmd.Parameters.AddWithValue("@IdReserva", idReserva);

                    try
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();

                        mensajeSalida = "Procedimiento ejecutado con exito.";
                        exito = true;  // Operación exitosa
                    }
                    catch (Exception ex)
                    {
                        // Manejo de errores
                        mensajeSalida = $"Error al eliminar la reserva: {ex.Message}";
                        exito = false;  // Operación fallida
                    }
                }
            }

            // Devolver un resultado JSON con el mensaje y el estado
            return Json(new { exito, mensaje = mensajeSalida });
        }

        // METODO PARA ACTUALIZAR

        [HttpPost]
        public IActionResult ActualizarReserva(int idReserva, TimeSpan nuevaHoraInicio, TimeSpan nuevaHoraFin, int nuevaUbicacion)
        {
            string mensajeSalida;

            // Validar que la nueva hora de fin sea posterior a la nueva hora de inicio
            if (nuevaHoraInicio >= nuevaHoraFin)
            {
                mensajeSalida = "Error: La nueva hora de fin debe ser posterior a la nueva hora de inicio.";
                TempData["Mensaje"] = mensajeSalida;
                return RedirectToAction("VerReserva", new { idReserva });
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("ActualizarReserva", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Parámetros de entrada
                    cmd.Parameters.AddWithValue("@IdReserva", idReserva);
                    cmd.Parameters.AddWithValue("@NuevaHoraInicio", nuevaHoraInicio);
                    cmd.Parameters.AddWithValue("@NuevaHoraFin", nuevaHoraFin);
                    cmd.Parameters.AddWithValue("@NuevaUbicacion", nuevaUbicacion);

                    // Parámetro de salida
                    SqlParameter mensajeParam = new SqlParameter("@mensaje", SqlDbType.VarChar, 100)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(mensajeParam);

                    // Abrir la conexión y ejecutar el procedimiento almacenado
                    try
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();

                        // Recuperar el mensaje de salida
                        mensajeSalida = $"Reserva Actualizada con exito";
                    }
                    catch (Exception ex)
                    {
                        // Manejo de errores
                        mensajeSalida = $"Error al actualizar la reserva: {ex.Message}";
                    }
                    finally
                    {
                        con.Close();
                    }
                }
            }

            // Devolver un mensaje o redirigir según el resultado
            TempData["Mensaje"] = mensajeSalida;

            return RedirectToAction("VerReserva", new { idReserva });
        }


        // BUSCAR NOTIFIACIONES NO LEIDAS

        [HttpGet]
        public IActionResult ObtenerNotificacionesNoLeidas(int idUsuario)
        {
            List<Notificacion> notificaciones = new List<Notificacion>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("ObtenerNotificacionesNoLeidas", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

                    try
                    {
                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                notificaciones.Add(new Notificacion
                                {
                                    IdNotificacion = reader.GetInt32(0),
                                    Mensaje = reader.GetString(1),
                                    FechaCreacion = reader.GetDateTime(2)
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return BadRequest($"Error al obtener las notificaciones: {ex.Message}");
                    }
                    finally
                    {
                        con.Close();
                    }
                }
            }

            return Json(notificaciones); 
        }

        [HttpPost]
        public IActionResult MarcarNotificacionComoLeida(int idUsuario)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("MarcarNotificacionComoLeida", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

                    try
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        return BadRequest($"Error al marcar las notificaciones como leídas: {ex.Message}");
                    }
                    finally
                    {
                        con.Close();
                    }
                }
            }

            return Ok("Notificaciones marcadas como leídas.");
        }

        // METODO PARA CREAR RESERVAS
        [HttpPost]
        public JsonResult CrearReserva(int idSala, TimeSpan horaInicio, TimeSpan horaFin, int numeroAsistentes, int prioridad, int idUsuario, bool multiSala, string listaSalas)
        {
            bool registrado;
            string mensaje;


            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand("CrearReserva", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Parámetros de entrada
                    command.Parameters.AddWithValue("@IdSala", idSala);
                    command.Parameters.AddWithValue("@HoraInicio", horaInicio);
                    command.Parameters.AddWithValue("@HoraFin", horaFin);
                    command.Parameters.AddWithValue("@NumeroAsistentes", numeroAsistentes);
                    command.Parameters.AddWithValue("@Prioridad", prioridad);
                    command.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    command.Parameters.AddWithValue("@MultiSala", multiSala);
                    command.Parameters.AddWithValue("@ListaSalas", (object)listaSalas ?? DBNull.Value); // Manejo de null

                    // Parámetros de salida
                    SqlParameter registradoParam = new SqlParameter("@registrado", SqlDbType.Bit)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(registradoParam);

                    SqlParameter mensajeParam = new SqlParameter("@mensaje", SqlDbType.VarChar, 100)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(mensajeParam);

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();

                        // Obtener los valores de los parámetros de salida
                        registrado = (bool)registradoParam.Value;
                        mensaje = (string)mensajeParam.Value;

                        return Json(new { Exito = registrado, Mensaje = mensaje });
                    }
                    catch (Exception ex)
                    {
                        // Manejo de errores
                        return Json(new { Exito = false, Mensaje = "Ocurrió un error al crear la reserva: " + ex.Message });
                    }
                }
            }
        }

    }
}
