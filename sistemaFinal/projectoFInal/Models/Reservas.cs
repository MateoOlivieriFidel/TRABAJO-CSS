namespace projectoFInal.Models
{
    public class Reservas
    {
        public int IdReserva { get; set; }      
        public int IdSala { get; set; }          
        public string NombreSala { get; set; }     
        public TimeSpan HoraInicio { get; set; }  
        public TimeSpan HoraFin { get; set; }     
        public int NumeroAsistentes { get; set; } 
        public int Prioridad { get; set; }         
        public int IdUsuario { get; set; }        
        public string NombreUsuario { get; set; }
        public int? IdReservaGrupo { get; set; }

    }
}
