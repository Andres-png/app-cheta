namespace Api.Data;



using Microsoft.EntityFrameworkCore;
using Api.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<User> Users => Set<User>();
    public DbSet<FormData> FormData => Set<FormData>();
}
