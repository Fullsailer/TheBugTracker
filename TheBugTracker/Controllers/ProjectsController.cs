﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TheBugTracker.Data;
using TheBugTracker.Extensions;
using TheBugTracker.Models;
using TheBugTracker.Models.Enums;
using TheBugTracker.Models.ViewModels;
using TheBugTracker.Services.Interfaces;

namespace TheBugTracker.Controllers
{
    public class ProjectsController : Controller
    {
        #region Properties
        private readonly ApplicationDbContext _context;
        private readonly IBTRolesService _rolesService;
        private readonly IBTLookupService _lookupService;
        private readonly IBTFileService _fileService;
        private readonly IBTProjectService _projectService;
        private readonly IBTCompanyInfoService _companyInfoService;
        private readonly UserManager<BTUser> _userManager;
        #endregion

        #region Constructor
        public ProjectsController(ApplicationDbContext context,
                                    IBTRolesService rolesService,
                                    IBTLookupService lookupService,
                                    IBTFileService fileService,
                                    IBTProjectService projectService,
                                    IBTCompanyInfoService companyInfoService,
                                    UserManager<BTUser> userManager)
        {
            _context = context;
            _rolesService = rolesService;
            _lookupService = lookupService;
            _fileService = fileService;
            _projectService = projectService;
            _companyInfoService = companyInfoService;
            _userManager = userManager;
        }
        #endregion

        #region Index
        // GET: Projects
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Projects.Include(p => p.Company);
            return View(await applicationDbContext.ToListAsync());
        }
        #endregion

        #region My Projects
        public async Task<IActionResult> MyProjects()
        {
            string userId = _userManager.GetUserId(User);

            List<Project> projects = await _projectService.GetUserProjectsAsync(userId);

            return View(projects);
        }
        #endregion

        #region All Projects
        public async Task<IActionResult> AllProjects()
        {
            List<Project> projects = new();

            int companyId = User.Identity.GetCompanyId().Value;

            if (User.IsInRole(nameof(Roles.Admin)) || User.IsInRole(nameof(Roles.ProjectManager)))
            {
                projects = await _companyInfoService.GetAllProjectsAsync(companyId);
            }
            else
            {
                projects = await _projectService.GetAllProjectsByCompanyAsync(companyId);
            }
            return View(projects);
        }
        #endregion

        #region Get Archived Projects
        public async Task<IActionResult> ArchivedProjects()
        {
            int companyId = User.Identity.GetCompanyId().Value;

            List<Project> projects = await _projectService.GetArchivedProjectsByCompanyAsync(companyId);

            return View(projects);
        }
        #endregion

        #region Get Unassigned Projects
        public async Task<IActionResult> UnassignedProjects()
        {
            int companyId = User.Identity.GetCompanyId().Value;

            List<Project> projects = new();

            projects = await _projectService.GetUnassignedProjectsAsync(companyId);

            return View(projects);
        }
        #endregion

        #region Get Assign Project Manager
        [HttpGet]
        public async Task<IActionResult> AssignPM(int projectId)
        {
            int companyId = User.Identity.GetCompanyId().Value;

            AssignPMViewModel model = new();

            model.Project = await _projectService.GetProjectByIdAsync(projectId, companyId);
            model.PMList = new SelectList(await _rolesService.GetUserInRoleAsync(nameof(Roles.ProjectManager), companyId), "Id", "FullName");

            return View(model);
        }
        #endregion

        #region Post Assign Project Manager
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignPM(AssignPMViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (!string.IsNullOrEmpty(model.PMID))
                    {
                        //Fix this method it cause an infinate loop looking for empty PM MODEL
                        //Does this have to do with not haveing A company id and need to assign a user to a company to have a company id so it's not zero?
                        await _projectService.AddProjectManagerAsync(model.PMID, model.Project.Id);

                        return RedirectToAction(nameof(Details), new { id = model.Project.Id });
                    }
                }
                catch (Exception)
                {

                    throw;
                }
            }

            
            return RedirectToAction(nameof(AssignPM), new { projectId = model.Project.Id });
        }
        #endregion

        #region Get Assign Members
        [HttpGet]
        public async Task<IActionResult> AssignMembers(int id)
        {
            ProjectMembersViewModel model = new();

            int companyId = User.Identity.GetCompanyId().Value;

            model.Project = await _projectService.GetProjectByIdAsync(id, companyId);

            List<BTUser> developers = await _rolesService.GetUserInRoleAsync(nameof(Roles.Developer), companyId);
            List<BTUser> submitters = await _rolesService.GetUserInRoleAsync(nameof(Roles.Submitter), companyId);

            List<BTUser> companyMembers = developers.Concat(submitters).ToList();

            List<string> projectMembers = model.Project.Members.Select(m => m.Id).ToList();
            model.Users = new MultiSelectList(companyMembers, "Id", "FullName", projectMembers);

            return View(model);
        }
        #endregion

        #region Post Assign Members
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignMembers(ProjectMembersViewModel model)
        {
            if (model.SelectedUsers != null)
            {
                //Creat a list of our memeber Id's
                List<string> memberIds = (await _projectService.GetAllProjectMembersExceptPMAsync(model.Project.Id))
                                                                .Select(m => m.Id).ToList();

                //Remove current memebers
                foreach (string member in memberIds)
                {
                    await _projectService.RemoveUserFromProjectAsync(member, model.Project.Id);
                }

                //Add selected members
                foreach (string member in model.SelectedUsers)
                {
                    await _projectService.AddUserToProjectAsync(member, model.Project.Id);
                }

                //Go to projects details view
                return RedirectToAction("Details", "Projects", new { id = model.Project.Id });

            }
            return RedirectToAction(nameof(AssignMembers), new { id = model.Project.Id });
        } 
        #endregion

        #region Projects Details
        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            int companyId = User.Identity.GetCompanyId().Value;

            Project project = await _projectService.GetProjectByIdAsync(id.Value, companyId);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }
        #endregion

        #region Get Projects Create
        // GET: Projects/Create
        public async Task<IActionResult> Create()
        {
            int companyId = User.Identity.GetCompanyId().Value;

            // Add ViewModel instance "AddProjectWithPMViewModel"
            AddProjectWithPMViewModel model = new();

            //Load SelectLists with data ie. PMList & PriorityList
            model.PMList = new SelectList(await _rolesService.GetUserInRoleAsync(Roles.ProjectManager.ToString(), companyId), "Id", "FullName");
            model.PriorityList = new SelectList(await _lookupService.GetProjectPrioritiesAsync(), "Id", "Name");

            return View(model);
        }
        #endregion

        #region Post Projects Create
        // POST: Projects/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddProjectWithPMViewModel model)
        {
            if (model != null)
            {
                int companyId = User.Identity.GetCompanyId().Value;
                try
                {
                    if (model.Project.ImageFormFile != null)
                    {
                        model.Project.ImageFileData = await _fileService.ConvertFileToByteArrayAsync(model.Project.ImageFormFile);
                        model.Project.ImageFileName = model.Project.ImageFormFile.FileName;
                        model.Project.ImageFileContentType = model.Project.ImageFormFile.ContentType;
                    }
                    model.Project.CompanyId = companyId;

                    await _projectService.AddNewProjectAsync(model.Project);

                    //Add PM if one was chosen
                    if (!string.IsNullOrEmpty(model.PmId))
                    {
                        await _projectService.AddProjectManagerAsync(model.PmId, model.Project.Id);
                    }

                }
                catch (Exception)
                {

                    throw;
                }
                //TODO: Redirect to All Projects
                return RedirectToAction("Index");
            }
            return RedirectToAction("Create");
        }
        #endregion

        #region Get Projects Edit
        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            int companyId = User.Identity.GetCompanyId().Value;

            // Add ViewModel instance "AddProjectWithPMViewModel"
            AddProjectWithPMViewModel model = new();

            model.Project = await _projectService.GetProjectByIdAsync(id.Value, companyId);

            //Load SelectLists with data ie. PMList & PriorityList
            model.PMList = new SelectList(await _rolesService.GetUserInRoleAsync(Roles.ProjectManager.ToString(), companyId), "Id", "FullName");
            model.PriorityList = new SelectList(await _lookupService.GetProjectPrioritiesAsync(), "Id", "Name");

            return View(model);
        }
        #endregion

        #region Post Projects Edit
        // POST: Projects/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AddProjectWithPMViewModel model)
        {
            if (model != null)
            {
                int companyId = User.Identity.GetCompanyId().Value;
                try
                {
                    if (model.Project.ImageFormFile != null)
                    {
                        model.Project.ImageFileData = await _fileService.ConvertFileToByteArrayAsync(model.Project.ImageFormFile);
                        model.Project.ImageFileName = model.Project.ImageFormFile.FileName;
                        model.Project.ImageFileContentType = model.Project.ImageFormFile.ContentType;
                    }
                    //model.Project.CompanyId = companyId;

                    await _projectService.UpdateProjectAsync(model.Project);

                    //Add PM if one was chosen
                    if (!string.IsNullOrEmpty(model.PmId))
                    {
                        await _projectService.AddProjectManagerAsync(model.PmId, model.Project.Id);
                    }
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {

                    throw;
                }


            }
            return RedirectToAction("Edit");
        }
        #endregion

        #region Get Projects Archive
        // GET: Projects/Archive/5
        public async Task<IActionResult> Archive(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            int companyId = User.Identity.GetCompanyId().Value;
            var project = await _projectService.GetProjectByIdAsync(id.Value, companyId);


            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }
        #endregion

        #region Post Projects Archive
        // POST: Projects/Archive/5
        [HttpPost, ActionName("Archive")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveConfirmed(int id)
        {
            int companyId = User.Identity.GetCompanyId().Value;

            var project = await _projectService.GetProjectByIdAsync(id, companyId);

            await _projectService.ArchiveProjectAsync(project);

            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region Get Projects Restore
        // GET: Projects/Restore/5
        public async Task<IActionResult> Restore(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            int companyId = User.Identity.GetCompanyId().Value;
            var project = await _projectService.GetProjectByIdAsync(id.Value, companyId);


            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }
        #endregion

        #region Post Projects Restore
        // POST: Projects/Restore/5
        [HttpPost, ActionName("Restore")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreConfirmed(int id)
        {
            int companyId = User.Identity.GetCompanyId().Value;

            var project = await _projectService.GetProjectByIdAsync(id, companyId);

            await _projectService.RestoreProjectAsync(project);

            return RedirectToAction(nameof(Index));
        }
        #endregion


        #region Project Exists
        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.Id == id);
        }

        #endregion
    }
}
