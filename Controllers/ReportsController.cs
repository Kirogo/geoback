using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using geoback.Data;
using geoback.Models;

namespace geoback.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly GeoDbContext _context;

    public ReportsController(GeoDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<SiteVisitReport>>> GetReports(
        [FromQuery] string? rmId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        var query = _context.Reports.Include(r => r.Client).AsQueryable();

        if (!string.IsNullOrEmpty(rmId))
        {
            query = query.Where(r => r.RmId == rmId);
        }

        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<ReportStatus>(status, true, out var statusEnum))
            {
                query = query.Where(r => r.Status == statusEnum);
            }
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(r => 
                r.Title.Contains(search) || 
                r.ReportNumber.Contains(search) ||
                (r.Client != null && r.Client.Name.Contains(search)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<SiteVisitReport>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    [HttpGet("my-reports")]
    public async Task<ActionResult<PaginatedResponse<SiteVisitReport>>> GetMyReports(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        // For now, we expect the frontend to pass the user ID as RmId or we extract it from claims
        // Simplifying to use the search-based GetReports logic but forced to the current user's ID
        // In a real app, this would use User.FindFirst(ClaimTypes.NameIdentifier).Value
        
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        return await GetReports(userId, status, search, page, pageSize);
    }

    [HttpGet("my-pending-reports")]
    public async Task<ActionResult<List<SiteVisitReport>>> GetMyPendingReports()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var reports = await _context.Reports
            .Include(r => r.Client)
            .Where(r => r.RmId == userId && 
                        (r.Status == ReportStatus.Draft || r.Status == ReportStatus.PendingQsReview))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(reports);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SiteVisitReport>> GetReport(Guid id)
    {
        var report = await _context.Reports
            .Include(r => r.Client)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null)
        {
            return NotFound();
        }

        return report;
    }

    [HttpPost]
    public async Task<ActionResult<SiteVisitReport>> CreateReport(SiteVisitReport report)
    {
        try 
        {
            Console.WriteLine($"Attempting to create report: {report.Title} for Client: {report.ClientId}");
            
            // Auto-generate report number if missing
            if (string.IsNullOrEmpty(report.ReportNumber))
            {
                report.ReportNumber = $"RPT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
            }

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetReport), new { id = report.Id }, report);
        }
        catch (Exception ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;
            Console.WriteLine($"Error creating report: {message}");
            return StatusCode(500, new { message = message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateReport(Guid id, SiteVisitReport report)
    {
        if (id != report.Id)
        {
            return BadRequest();
        }

        _context.Entry(report).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ReportExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReport(Guid id)
    {
        var report = await _context.Reports.FindAsync(id);
        if (report == null)
        {
            return NotFound();
        }

        _context.Reports.Remove(report);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ReportExists(Guid id)
    {
        return _context.Reports.Any(e => e.Id == id);
    }
}
