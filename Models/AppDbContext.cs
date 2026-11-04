using Microsoft.EntityFrameworkCore;

namespace PhoAmThuc.Models
{
    // Cầu nối giữa code C# và cơ sở dữ liệu SQL Server.
    // Mỗi DbSet tương ứng một bảng cùng tên trong file WebsiteAmThuc2.sql
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<ProviderProfile> ProviderProfiles => Set<ProviderProfile>();
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<Label> Labels => Set<Label>();
        public DbSet<PostLabel> PostLabels => Set<PostLabel>();
        public DbSet<Recipe> Recipes => Set<Recipe>();
        public DbSet<Ingredient> Ingredients => Set<Ingredient>();
        public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Visit> Visits => Set<Visit>();
        public DbSet<InfoPage> InfoPages => Set<InfoPage>();
        public DbSet<Setting> Settings => Set<Setting>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureKeys(modelBuilder);
            ConfigurePostRelations(modelBuilder);
            ConfigureRecipeRelations(modelBuilder);
            ConfigureCommentRelations(modelBuilder);
        }

        // Khóa chính ghép và kiểu dữ liệu đặc biệt
        private void ConfigureKeys(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PostLabel>().HasKey(x => new { x.PostId, x.LabelId });
            modelBuilder.Entity<Ingredient>().Property(x => x.Amount).HasPrecision(12, 3);
        }

        // Quan hệ giữa bài đăng với tác giả, nhãn, hồ sơ nhà cung cấp
        private void ConfigurePostRelations(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProviderProfile>()
                .HasOne(x => x.Account)
                .WithOne()
                .HasForeignKey<ProviderProfile>(x => x.AccountId);

            modelBuilder.Entity<Post>()
                .HasOne(x => x.Author)
                .WithMany()
                .HasForeignKey(x => x.AccountId);

            modelBuilder.Entity<PostLabel>()
                .HasOne(x => x.Post)
                .WithMany(x => x.PostLabels)
                .HasForeignKey(x => x.PostId);

            modelBuilder.Entity<PostLabel>()
                .HasOne(x => x.Label)
                .WithMany(x => x.PostLabels)
                .HasForeignKey(x => x.LabelId);
        }

        // Quan hệ giữa công thức với bài đăng, nguyên liệu, các bước
        private void ConfigureRecipeRelations(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Recipe>()
                .HasOne(x => x.Post)
                .WithOne(x => x.Recipe)
                .HasForeignKey<Recipe>(x => x.PostId);

            modelBuilder.Entity<Ingredient>()
                .HasOne(x => x.Recipe)
                .WithMany(x => x.Ingredients)
                .HasForeignKey(x => x.RecipeId);

            modelBuilder.Entity<RecipeStep>()
                .HasOne(x => x.Recipe)
                .WithMany(x => x.Steps)
                .HasForeignKey(x => x.RecipeId);
        }

        // Bình luận có hai khóa ngoại tới Accounts (người viết, hồ sơ được đánh giá) nên phải khai báo rõ
        private void ConfigureCommentRelations(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Comment>()
                .HasOne(x => x.Author)
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(x => x.Post)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(x => x.TargetAccount)
                .WithMany()
                .HasForeignKey(x => x.TargetAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(x => x.Parent)
                .WithMany(x => x.Replies)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
