using KpiSys.Web.Models;
using KpiSys.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace KpiSys.Web.Controllers;

[Route("[controller]")]
public class CodesController : Controller
{
    private readonly ICodeService _codeService;

    public CodesController(ICodeService codeService)
    {
        _codeService = codeService;
    }

    [HttpGet]
    [Route("")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("CodeSets")]
    public IActionResult GetCodeSets()
    {
        var sets = _codeService.GetCodeSets();
        return Ok(sets);
    }

    [HttpGet("List")]
    public IActionResult List([FromQuery] string codeSet)
    {
        var codes = _codeService.GetCodes(codeSet);
        return Ok(codes);
    }

    [HttpPost("Create")]
    public IActionResult Create([FromBody] CodeItem item)
    {
        var (success, error) = _codeService.AddCode(item);
        if (!success)
        {
            return BadRequest(new { message = error });
        }

        return Ok();
    }

    [HttpPut("Edit")]
    public IActionResult Edit([FromQuery] string codeSet, [FromQuery] string code, [FromBody] CodeItem item)
    {
        var (success, error) = _codeService.UpdateCode(codeSet, code, item);
        if (!success)
        {
            return BadRequest(new { message = error });
        }

        return Ok();
    }

    [HttpDelete("Delete")]
    public IActionResult Delete([FromQuery] string codeSet, [FromQuery] string code)
    {
        var success = _codeService.DeleteCode(codeSet, code);
        if (!success)
        {
            return NotFound();
        }

        return Ok();
    }
}
