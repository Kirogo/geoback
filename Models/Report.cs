using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace geoback.Models;

public enum ReportStatus
{
    Draft,
    PendingQsReview,
    UnderReview,
    RevisionRequested,
    SiteVisitScheduled,
    Approved,
    Rejected,
    Archived
}

public class SiteVisitReport
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string ReportNumber { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ReportStatus Status { get; set; } = ReportStatus.Draft;

    public Guid ClientId { get; set; }
    public Client? Client { get; set; }

    public string RmId { get; set; } = string.Empty; // Relationship Manager ID (external auth)
    public string? QsId { get; set; } // Quantity Surveyor ID

    public DateTime VisitDate { get; set; }
    public string? SiteAddress { get; set; }

    // JSON or Owned Entity for simple coordinates
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public string? Weather { get; set; }
    public double? Temperature { get; set; }
    public string? ProjectName { get; set; }

    public string? LoanType { get; set; }
    public string? IbpsNo { get; set; }
    public string? DocumentsJson { get; set; } // Stores the structured document checklist as JSON

    public List<WorkProgress> WorkProgress { get; set; } = new();
    public List<Issue> Issues { get; set; } = new();
    public List<ReportPhoto> Photos { get; set; } = new();

    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class WorkProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Description { get; set; } = string.Empty;
    public double Percentage { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Notes { get; set; }
    
    // For simplicity, we might store photo URLs as a list of strings or a separate table if needed.
    // Here simplifying to just basic properties for the prototype.
}

public class Issue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "low"; // Enum could be better
    public string Status { get; set; } = "open";
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }
}

public class ReportPhoto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Url { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string UploadedBy { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public string? Category { get; set; }
}
