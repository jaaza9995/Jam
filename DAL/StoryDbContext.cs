using Microsoft.EntityFrameworkCore;
using Jam.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Jam.DAL;

public class StoryDbContext : IdentityDbContext<ApplicationUser>
{
    public StoryDbContext(DbContextOptions<StoryDbContext> options) : base(options)
    {
        // Database.EnsureCreated();
    }

    public DbSet<Story> Stories { get; set; }
    public DbSet<IntroScene> IntroScenes { get; set; }
    public DbSet<QuestionScene> QuestionScenes { get; set; }
    public DbSet<AnswerOption> AnswerOptions { get; set; }
    public DbSet<EndingScene> EndingScenes { get; set; }
    public DbSet<PlayingSession> PlayingSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ==========================================================================
        // 1. USER RELATIONSHIPS (Preserve content, break link by setting FK to NULL)
        //    Requires Story.UserId and PlayingSession.UserId to be nullable (int?)
        // ==========================================================================

        // User -> Story: If a User is deleted, their Stories remain, but the Story.UserId is set to NULL
        modelBuilder.Entity<Story>()
            .HasOne(s => s.User)
            .WithMany(u => u.Stories)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // User -> PlayingSession: If a User is deleted, the sessions remain, but PlayingSession.UserId is set to NULL
        modelBuilder.Entity<PlayingSession>()
            .HasOne(ps => ps.User)
            .WithMany(u => u.PlayingSessions)
            .HasForeignKey(ps => ps.UserId)
            .OnDelete(DeleteBehavior.SetNull);



        // ==========================================================================
        // 2. STORY STRUCTURE RELATIONSHIPS (Cascade Delete for Dependent Content)
        //    If the principal entity is deleted, all dependent entities are deleted
        // ==========================================================================

        // Story -> IntroScene (1-to-1): 
        // Deleting a Story automatically deletes its IntroScene
        modelBuilder.Entity<Story>()
            .HasOne(s => s.IntroScene)
            .WithOne(i => i.Story)
            .HasForeignKey<IntroScene>(i => i.StoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Story -> QuestionScene (1-to-many): 
        // Deleting a Story automatically deletes all its QuestionScenes
        modelBuilder.Entity<Story>()
            .HasMany(s => s.QuestionScenes)
            .WithOne(qs => qs.Story)
            .HasForeignKey(qs => qs.StoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Story -> EndingScene (1-to-many): 
        // Deleting a Story automatically deletes all its EndingScenes
        modelBuilder.Entity<Story>()
            .HasMany(s => s.EndingScenes)
            .WithOne(es => es.Story)
            .HasForeignKey(es => es.StoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Story -> PlayingSession (1-to-many): 
        // Deleting a Story automatically deletes all associated PlayingSession records (as they are now meaningless)
        modelBuilder.Entity<Story>()
            .HasMany(s => s.PlayingSessions)
            .WithOne(ps => ps.Story)
            .HasForeignKey(ps => ps.StoryId)
            .OnDelete(DeleteBehavior.Cascade);



        // ==========================================================================
        // 3. Question SCENE NAVIGATION RELATIONSHIPS (Restrict Deletion to Preserve Integrity)
        //    If a QuestionScene being referenced is deleted, the referencing Scene's 
        //    NextQuestionSceneId must be manually set to NULL beforehand
        // ==========================================================================

        // QuestionScene -> NextQuestionScene (Self-referencing One-to-One/Zero Relationship):
        modelBuilder.Entity<QuestionScene>()
            .HasOne(qs => qs.NextQuestionScene)
            .WithMany() // no inverse navigation
            .HasForeignKey(qs => qs.NextQuestionSceneId)
            .OnDelete(DeleteBehavior.SetNull);

        // Enforce that NextQuestionSceneId is unique across all Scenes
        modelBuilder.Entity<QuestionScene>()
            .HasIndex(s => s.NextQuestionSceneId)
            .IsUnique();

        // QuestionScene -> AnswerOptions (1-to-many)
        // Deleting a QuestionScene automatically deletes all associated AnswerOptions
        modelBuilder.Entity<QuestionScene>()
            .HasMany(qs => qs.AnswerOptions)
            .WithOne(ao => ao.QuestionScene)
            .HasForeignKey(ao => ao.QuestionSceneId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}