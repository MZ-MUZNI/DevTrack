using DevTrack.Core.Entities;
using DevTrack.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DevTrack.Web.Controllers;

public class ProjectsController : Controller
{
    private readonly IRepository<ProjectEntity> _projectRepository;

    // Dependency Injection in action: ASP.NET Core sees this constructor
    // needs an IRepository<ProjectEntity>, looks in the DI container
    // (registered in Program.cs), and hands us a Repository<ProjectEntity>
    // instance automatically. We never call "new" here.
    public ProjectsController(IRepository<ProjectEntity> projectRepository)
    {
        _projectRepository = projectRepository;
    }

    // GET: /Projects
    public async Task<IActionResult> Index()
    {
        var projects = await _projectRepository.GetAllAsync();
        return View(projects); // looks for Views/Projects/Index.cshtml
    }

    // GET: /Projects/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project is null) return NotFound();
        return View(project);
    }

    // GET: /Projects/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Projects/Create
    [HttpPost]
    [ValidateAntiForgeryToken] // CSRF protection - security-relevant, mention this in interviews
    public async Task<IActionResult> Create([Bind("Name,Description")] ProjectEntity project)
    {
        // ModelState.IsValid checks the [Required]/[MaxLength] etc. data
        // annotations on the model automatically - no manual if-checks needed.
        if (!ModelState.IsValid)
        {
            return View(project);
        }

        await _projectRepository.AddAsync(project);
        await _projectRepository.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Projects/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project is null) return NotFound();
        return View(project);
    }

    // POST: /Projects/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,CreatedAt")] ProjectEntity project)
    {
        if (id != project.Id) return BadRequest();
        if (!ModelState.IsValid) return View(project);

        _projectRepository.Update(project);
        await _projectRepository.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Projects/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project is null) return NotFound();
        return View(project);
    }

    // POST: /Projects/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project is null) return NotFound();

        _projectRepository.Delete(project);
        await _projectRepository.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
