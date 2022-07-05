﻿using System.ComponentModel.DataAnnotations;
//using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using eauction.Data;
using eauction.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Models.Domain.Identity;

namespace eauction.Areas.Identity.Pages.Account
{
    [Authorize(Roles = "System,SuperAdmin,Admin")]
    public class AdminRegisterModel : PageModel
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _db;

        public AdminRegisterModel(
            UserManager<User> userManager,
            IAuthorizationService authorizationService,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _authorizationService = authorizationService;
            _db = db;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        [ViewData]
        public bool Status { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(15)]
            [Display(Name = "Mobile Number")]
            public string PhoneNumber { get; set; }

            [Required]
            [StringLength(15)]
            [Display(Name = "CNIC")]
            public string CNIC { get; set; }

            [StringLength(25)]
            public string NTN { get; set; }

            [StringLength(100)]
            [Display(Name = "Company Name")]
            public string Company { get; set; }

            [Required]
            [StringLength(50)]
            public string Name { get; set; }

            [Required]
            [EmailAddress]
            [StringLength(100)]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm Password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        private async Task<bool> Authorize()
        {
            try
            {
                var user = await _userManager.FindByNameAsync(this.User.Identity.Name);
                var roles = await _userManager.GetRolesAsync(user);

                string[] validRoles = { "System", "SuperAdmin", "Admin" };

                if (roles.Any(x => validRoles.Contains(x)))
                {
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            //this._authorizationService.AuthorizeAsync(this.User, null, )
            //ReturnUrl = returnUrl;

            //if (await this.Authorize() == false)
            //{
            //    return Forbid();
            //}

            //return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            //if (await this.Authorize() == false)
            //{
            //    return Forbid();
            //}

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage);
                return Page();
            }

            Input.CNIC = RegexUtilities.Clean(Input.CNIC);  //  remove dashes
            Input.PhoneNumber = RegexUtilities.Clean(Input.PhoneNumber);  //  remove dashes

            if (Infrastructure.RegexUtilities.IsValidCNIC(Input.CNIC) == false)
            {
                ModelState.AddModelError(string.Empty, "CNIC should consists of valid 13 digits number.");
                return Page();
            }

            var parameters = new SqlParameter[3]
            {
                new SqlParameter("@CNIC", Input.CNIC),
                new SqlParameter("@Email", Input.Email),
                new SqlParameter("@PhoneNumber", Input.PhoneNumber),
            };

            //var parameters = new SqlParameter[3]
            //{
            //    new SqlParameter("@CNIC", SqlDbType.VarChar, 13) { Value = Input.CNIC },
            //new SqlParameter("@Email", Input.Email),
            //    new SqlParameter("@PhoneNumber", Input.PhoneNumber),
            //};

            var existingUsers = this._db.Users.FromSqlRaw("EXEC [Identity].GetExistingUsers @CNIC, @Email, @PhoneNumber", parameters).ToList();

            if (existingUsers.Count() > 0)
            {
                ModelState.AddModelError(string.Empty, "This CNIC / Email / Phone Number has already been taken.");
                return Page();
            }

            var user = new Models.Domain.Identity.User
            {
                UserName = Input.CNIC,
                FullName = Input.Name,
                CNIC = Input.CNIC,
                NTN = Input.NTN,
                Company = Input.Company,
                PhoneNumber = Input.PhoneNumber,
                Email = Input.Email,
                UserTypeId = 1
            };

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("FullName", Input.Name));

                this.Status = true;

                return Page();
            }
            else 
            {
                result.Errors.Select(x => x.Description).ToList().ForEach(error => 
                {
                    ModelState.AddModelError(string.Empty, error);
                });
            }

            return Page();
        }
    }
}
