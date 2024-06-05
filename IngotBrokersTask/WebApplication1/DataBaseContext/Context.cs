using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.DataBaseContext
{
    
    public class Context : DbContext
    {
        public DbSet<User> Users { get; set; }

        public Context(DbContextOptions<Context> options) : base(options)
        {

        }

    }
}
