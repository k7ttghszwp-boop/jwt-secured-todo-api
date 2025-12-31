using Microsoft.EntityFrameworkCore;

namespace TodoApp.Data
{
    // Program.cs içerisindeki record kullanımıyla uyumlu model
    // Not: Program.cs'de 'record' kullanıyorsanız, DBContext içindeki model de buna uygun olmalıdır.
    // Ancak EF Core ile en sağlıklı kullanım için class yapısını koruyup 
    // Program.cs'deki karmaşayı önlemek adına namespace'i oraya dahil etmeliyiz.
    public class TodoItem
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public bool IsCompleted { get; set; }
    }

    // Entity Framework Core Context sınıfı
    public class TodoDbContext : DbContext
    {
        public TodoDbContext(DbContextOptions<TodoDbContext> options)
            : base(options)
        {
        }

        /* * DbSet tanımı: EF Core 7+ ile 'Set<T>()' kullanımı property'nin 
         * null referans uyarısı vermesini engeller.
         */
        public DbSet<TodoItem> Todos => Set<TodoItem>();
    }
}