using Huy_Final_0843.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Huy_Final_0843.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class AdminUserManagerController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public AdminUserManagerController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        // Xem danh sách Users
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRolesViewModel = new List<UserRolesViewModel>();
            foreach (var user in users)
            {
                var thisViewModel = new UserRolesViewModel();
                thisViewModel.UserId = user.Id;
                thisViewModel.Email = user.Email;
                thisViewModel.FullName = user.FullName;
                thisViewModel.Roles = await _userManager.GetRolesAsync(user);
                userRolesViewModel.Add(thisViewModel);
            }

            // Sắp xếp: Admin (1) > Staff (2) > User (3)
            var sortedList = userRolesViewModel.OrderBy(u => {
                if (u.Roles.Contains(SD.Role_Admin)) return 1;
                if (u.Roles.Contains(SD.Role_Staff)) return 2;
                return 3;
            }).ToList();

            return View(sortedList);
        }

        // Quản lý Roles của 1 User
        public async Task<IActionResult> ManageRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var roles = await _roleManager.Roles.ToListAsync();
            var model = new ManageUserRolesViewModel
            {
                UserId = userId,
                FullName = user.FullName
            };

            foreach (var role in roles)
            {
                var roleSelection = new RoleSelection
                {
                    RoleName = role.Name,
                    IsSelected = await _userManager.IsInRoleAsync(user, role.Name)
                };
                model.Roles.Add(roleSelection);
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ManageRoles(ManageUserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var result = await _userManager.RemoveFromRolesAsync(user, roles);
            if (!result.Succeeded) return View(model);

            result = await _userManager.AddToRolesAsync(user, model.Roles.Where(x => x.IsSelected).Select(y => y.RoleName));
            if (!result.Succeeded) return View(model);

            // Invalidate cookie cũ ngay lập tức — user bị kick ra khỏi session hiện tại
            await _userManager.UpdateSecurityStampAsync(user);

            TempData["Success"] = "Cập nhật quyền thành công!";
            return RedirectToAction(nameof(Index));
        }

        // Xóa User
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();
            
            await _userManager.DeleteAsync(user);
            TempData["Success"] = "Đã xóa tài khoản người dùng!";
            return RedirectToAction(nameof(Index));
        }
    }
}
