using GameAiApi.Data;
using GameAiApi.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameAiApi.Controllers;

[ApiController]
[Route("api/leaderboard")]
public sealed class LeaderboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public LeaderboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LeaderboardEntry>>> Get(CancellationToken cancellationToken)
    {
        var leaderboard = await _context.Leaderboard
            .AsNoTracking()
            .OrderByDescending(entry => entry.Points)
            .Take(10)
            .ToListAsync(cancellationToken);

        return Ok(leaderboard);
    }
}
