using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AttorneyJournal.Models;
using AttorneyJournal.Models.Domain;
using AttorneyJournal.Models.Domain.Storage;

namespace AttorneyJournal.Data
{
	public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options)
		{
		}

		public DbSet<Attorney> Attorneys { get; set; }
		//public DbSet<RegistrationCodeEntity> RegistrationCodes { get; set; }
		public DbSet<FileStorage> Files { get; set; }

#if DEBUG
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			base.OnConfiguring(optionsBuilder);
			optionsBuilder.EnableSensitiveDataLogging();
		}
#endif

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			builder.Entity<Attorney>()
				.HasMany(x => x.Users)
				.WithOne(x => x.AssignedToAttorney)
				.HasForeignKey(x => x.AssignedToAttorneyId);

			builder.Entity<ApplicationUser>()
				.Property(x => x.AssignedToAttorneyId).IsRequired(false);
				//.HasDefaultValue(CommonConstant.DefaultAttorneyId);

			builder.Entity<ApplicationUser>()
				.Property(x => x.DateOfAccident)
				.HasDefaultValue(null);

			//builder.Entity<RegistrationCodeEntity>()
			//	.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);

			// Customize the ASP.NET Identity model and override the defaults if needed.
			// For example, you can rename the ASP.NET Identity table names and more.
			// Add your customizations after calling base.OnModelCreating(builder);

		}

		public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
		{
			var changes = ChangeTracker.Entries<BaseEntity>().Where(p => p.State == EntityState.Modified).Select(u => u.Entity);
			foreach (var change in changes)
				Entry(change).Property(u => u.UpdatedAt).OriginalValue = DateTime.UtcNow;
			return base.SaveChangesAsync(cancellationToken);
		}
	}
}
