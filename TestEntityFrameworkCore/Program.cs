using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TestEntityFrameworkCore
{
    public class User
    {
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public IReadOnlyList<Role> Roles => _roles.AsReadOnly();
        private readonly List<Role> _roles = new List<Role>();

        public void SetRoles(IList<Role> roles)
        {
            if (_roles.Count == roles.Count &&
                !_roles.Except(roles).Any())
                return;

            _roles.Clear();
            _roles.AddRange(roles.Where(x => x != null).Distinct());
        }
    }

    public class Role : IEquatable<Role>
    {
        public string Value { get; set; }
        public bool Equals(Role other)
            => Value == other.Value;
    }

    public class SqlDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseSqlServer(@"Server=.;Database=core;Trusted_Connection=True;");

        protected override void OnModelCreating(ModelBuilder builder) =>
            builder.Entity<User>(m =>
            {
                m.ToTable("User", "user");
                m.HasKey(x => x.UserId);
                m.OwnsMany(x => x.Roles, nav =>
                {
                    nav.ToTable("UserRole", "user");
                    nav.Property<Guid>("RoleAssignmentId");
                    nav.HasKey("RoleAssignmentId");
                    nav.Property(x => x.Value)
                        .HasColumnName("Role");
                    nav.Property<Guid>("UserId");

                    nav.HasForeignKey("UserId");
                }).UsePropertyAccessMode(PropertyAccessMode.Field);
            });
    }

    public class Program
    {
        public static async Task Main()
        {
            using (var context = new SqlDbContext())
            {
                var user = await context.Set<User>()
                    .Where(u => u.Email == "hapica@gmail.com")
                    .FirstOrDefaultAsync(CancellationToken.None);

                // At this point user.Roles have 3 roles

                user.SetRoles(new List<Role>
                {
                    new Role
                    {
                        Value = "Basic"
                    }
                });

                // At this point user.Roles have 1 "Basic" role
                await context.SaveChangesAsync(CancellationToken.None);

                var role = user.Roles; // At this point user.Roles have no item
                // But we expect user.Roles still have 1 "Basic" role for us to do some further handlings
            }
        }
    }
}
