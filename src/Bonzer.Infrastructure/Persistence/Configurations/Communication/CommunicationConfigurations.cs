using Bonzer.Infrastructure.Persistence.Configurations.Organization;

namespace Bonzer.Infrastructure.Persistence.Configurations.Communication;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("notifications");
        b.Property(x => x.Title).HasMaxLength(200);
        b.Property(x => x.Channels).HasMaxLength(100);
        b.Property(x => x.AudienceFilter).HasColumnType("jsonb");
        b.Property(x => x.AttachmentUrl).HasMaxLength(1000);
        b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId);
        b.HasIndex(x => new { x.TenantId, x.CreatedAt });
    }
}

internal sealed class NotificationRecipientConfiguration : IEntityTypeConfiguration<NotificationRecipient>
{
    public void Configure(EntityTypeBuilder<NotificationRecipient> b)
    {
        b.ToTable("notification_recipients");
        b.Property(x => x.Channel).HasMaxLength(100);
        b.Property(x => x.Error).HasMaxLength(500);
        b.HasOne(x => x.Notification).WithMany(n => n.Recipients).HasForeignKey(x => x.NotificationId);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId);
        b.HasIndex(x => new { x.NotificationId, x.UserId, x.Channel }).IsUnique().HasFilter(Sql.NotDeleted);
        b.HasIndex(x => new { x.UserId, x.ReadAt });
    }
}

internal sealed class HomeworkConfiguration : IEntityTypeConfiguration<Homework>
{
    public void Configure(EntityTypeBuilder<Homework> b)
    {
        b.ToTable("homeworks");
        b.Property(x => x.Title).HasMaxLength(200);
        b.Property(x => x.AttachmentUrl).HasMaxLength(1000);
        b.Property(x => x.MaxMarks).HasPrecision(6, 2);
        b.HasIndex(x => new { x.SectionId, x.AssignedOn });
    }
}

internal sealed class HomeworkSubmissionConfiguration : IEntityTypeConfiguration<HomeworkSubmission>
{
    public void Configure(EntityTypeBuilder<HomeworkSubmission> b)
    {
        b.ToTable("homework_submissions");
        b.Property(x => x.AttachmentUrl).HasMaxLength(1000);
        b.Property(x => x.Marks).HasPrecision(6, 2);
        b.HasOne(x => x.Homework).WithMany(h => h.Submissions).HasForeignKey(x => x.HomeworkId);
        b.HasIndex(x => new { x.HomeworkId, x.StudentId }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}
