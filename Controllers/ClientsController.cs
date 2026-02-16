using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using geoback.Data;
using geoback.Models;

namespace geoback.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly GeoDbContext _context;

    public ClientsController(GeoDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<Client>>> GetClients(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        var query = _context.Clients.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c => 
                c.Name.Contains(search) || 
                c.CustomerNumber.Contains(search) ||
                (c.ProjectName != null && c.ProjectName.Contains(search)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<Client>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    [HttpGet("customer/{customerNumber}")]
    public async Task<ActionResult<Client>> GetByCustomerNumber(string customerNumber)
    {
        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.CustomerNumber == customerNumber);

        if (client == null)
        {
            return NotFound();
        }

        return client;
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<Client>>> Search([FromQuery] string q)
    {
        if (string.IsNullOrEmpty(q))
        {
            return Ok(new List<Client>());
        }

        var clients = await _context.Clients
            .Where(c => c.Name.Contains(q) || c.CustomerNumber.Contains(q) || (c.ProjectName != null && c.ProjectName.Contains(q)))
            .Take(10)
            .ToListAsync();

        return Ok(clients);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Client>> GetClient(Guid id)
    {
        var client = await _context.Clients.FindAsync(id);

        if (client == null)
        {
            return NotFound();
        }

        return client;
    }

    [HttpPost]
    public async Task<ActionResult<Client>> CreateClient(Client client)
    {
        try 
        {
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetClient), new { id = client.Id }, client);
        }
        catch (Exception ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, new { message = message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClient(Guid id, Client client)
    {
        if (id != client.Id)
        {
            return BadRequest();
        }

        _context.Entry(client).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ClientExists(id))
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
    public async Task<IActionResult> DeleteClient(Guid id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client == null)
        {
            return NotFound();
        }

        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ClientExists(Guid id)
    {
        return _context.Clients.Any(e => e.Id == id);
    }
}
