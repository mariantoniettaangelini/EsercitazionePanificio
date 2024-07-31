using Esercitazione.Models;
using Microsoft.EntityFrameworkCore;

namespace Esercitazione.Context
{
    public class DataContext : DbContext

    {
        public DataContext() { }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<Ingredient> Ingredients { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public DataContext(DbContextOptions<DataContext> opt) : base(opt) { }
    }
}
