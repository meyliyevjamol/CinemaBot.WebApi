using CinemaBot.DataLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace CinemaBot.DataLayer;

public partial class TelegramBotContext : DbContext
{
    public TelegramBotContext()
    {
    }

    public TelegramBotContext(DbContextOptions<TelegramBotContext> options)
        : base(options)
    {
    }

    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<UserActionState> UserActionStates { get; set; }
    public virtual DbSet<Channel> Channels { get; set; }
    public virtual DbSet<Film> Films { get; set; }
    public virtual DbSet<UserFilm> UserFilms { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=telegram_bot;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //modelBuilder.Entity<Course>(entity =>
        //{
        //    entity.HasKey(e => e.Id).HasName("course_pkey");

        //    entity.Property(e => e.Id).ValueGeneratedNever();
        //    entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

        //    entity.HasOne(d => d.Organization).WithMany(p => p.Courses)
        //        .OnDelete(DeleteBehavior.ClientSetNull)
        //        .HasConstraintName("fk_organization_id");

        //    entity.HasOne(d => d.State).WithMany(p => p.Courses)
        //        .OnDelete(DeleteBehavior.ClientSetNull)
        //        .HasConstraintName("foreign_state_id");
        //});

        //modelBuilder.Entity<Organization>(entity =>
        //{
        //    entity.HasKey(e => e.Id).HasName("organization_pkey");

        //    entity.Property(e => e.CreatedDate).HasDefaultValueSql("now()");

        //    entity.HasOne(d => d.State).WithMany(p => p.Organizations)
        //        .OnDelete(DeleteBehavior.ClientSetNull)
        //        .HasConstraintName("fk_state_id");
        //});

        //modelBuilder.Entity<Role>(entity =>
        //{
        //    entity.HasKey(e => e.Id).HasName("role_pkey");

        //    entity.Property(e => e.CreatedDate).HasDefaultValueSql("now()");

        //    entity.HasOne(d => d.State).WithMany(p => p.Roles)
        //        .OnDelete(DeleteBehavior.ClientSetNull)
        //        .HasConstraintName("fk_state_id");
        //});

        //modelBuilder.Entity<State>(entity =>
        //{
        //    entity.HasKey(e => e.Id).HasName("state_pkey");

        //    entity.Property(e => e.Id).ValueGeneratedNever();
        //    entity.Property(e => e.CreatedDate).HasDefaultValueSql("now()");
        //});

        //modelBuilder.Entity<SysUser>(entity =>
        //{
        //    entity.HasKey(e => e.Id).HasName("sys_user_pkey");

        //    entity.Property(e => e.CreatedDate).HasDefaultValueSql("now()");

        //    entity.HasOne(d => d.Organization).WithMany(p => p.SysUsers)
        //        .OnDelete(DeleteBehavior.ClientSetNull)
        //        .HasConstraintName("fk_organization_id");

        //    entity.HasOne(d => d.Role).WithMany(p => p.SysUsers).HasConstraintName("sys_user_role_id_fkey");
        //});

        //modelBuilder.Entity<UserCourse>(entity =>
        //{
        //    entity.HasKey(e => e.Id).HasName("user_course_pkey");

        //    entity.Property(e => e.CreatedDate).HasDefaultValueSql("now()");
        //    entity.Property(e => e.IsTeacher).HasDefaultValue(false);

        //    entity.HasOne(d => d.Course).WithMany(p => p.UserCourses)
        //        .OnDelete(DeleteBehavior.ClientSetNull)
        //        .HasConstraintName("fk_role_id");

        //    entity.HasOne(d => d.State).WithMany(p => p.UserCourses)
        //        .OnDelete(DeleteBehavior.ClientSetNull)
        //        .HasConstraintName("fk_state_id");

        //    entity.HasOne(d => d.User).WithMany(p => p.UserCourses)
        //        .OnDelete(DeleteBehavior.ClientSetNull)
        //        .HasConstraintName("fk_user_id");
        //});

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
