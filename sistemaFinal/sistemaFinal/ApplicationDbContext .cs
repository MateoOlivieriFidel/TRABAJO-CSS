using Microsoft.EntityFrameworkCore;
using sistemaFinal.modelos;

namespace sistemaFinal
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext (DbContextOptions<ApplicationDbContext> options): base(options)
            {
            }
        public DbSet<Usuario> usuarios { get; set; }
    }
}