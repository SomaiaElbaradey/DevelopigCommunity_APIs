﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevelopigCommunityService.Context;
using DevelopigCommunityService.Models.Bassal;
using DevelopigCommunityService.Interfaces;
using System.Text;
using System.Security.Cryptography;
using DevelopigCommunityService.DTOs.Bassal;

namespace DevelopigCommunityService.Controllers.Bassal
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminsController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;

        public AdminsController(DataContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        // GET: api/Admins
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Admin>>> GetAdmins()
        {
            return await _context.Admins.ToListAsync();
        }

        // GET: api/Admins/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Admin>> GetAdmin(int id)
        {
            var admin = await _context.Admins.FindAsync(id);

            if (admin == null)
            {
                return NotFound();
            }

            return admin;
        }


        // POST: api/Individuals
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("Register")]
        public async Task<ActionResult<Individual>> Register(AdminRegisterDTOs AdminRegister)
        {

            if (await AdminExists(AdminRegister.UserName.ToLower())) return BadRequest("Admin name already exists");

            using var hmac = new HMACSHA512();

            var newAdmin = new Admin
            {
                UserName = AdminRegister.UserName.ToLower(),
                FirstName = AdminRegister.FirstName,
                LastName = AdminRegister.LastName,
                Age = AdminRegister.Age,
                Email = AdminRegister.Email,
                Phone = AdminRegister.Phone,
                PasswordHash = hmac.ComputeHash(Encoding.UTF32.GetBytes(AdminRegister.Password)),
                PasswordSalt = hmac.Key
            };

            await _context.Admins.AddAsync(newAdmin);

            await _context.SaveChangesAsync();

            return Ok("Created successfully");

            //_context.Individuals.Add(individual);
            //await _context.SaveChangesAsync();

            //return CreatedAtAction("GetIndividual", new { id = individual.Id }, individual);
        }

        [HttpPost("Login")]
        public async Task<ActionResult<IndividualDTOs>> Login(IndividualLoginDTO IndividualLogin)
        {

            var user = await _context.Individuals
                .SingleOrDefaultAsync(ww => ww.UserName == IndividualLogin.UserName.ToLower());

            if (user == null) return Unauthorized("Username or password is invalid");

            using var hmac = new HMACSHA512(user.GetPasswordSalt());

            var ComputeHash = hmac.ComputeHash(Encoding.UTF32.GetBytes(IndividualLogin.Password));

            byte[] passHasg = user.GetPasswordHash();
            
            for (int i = 0; i < ComputeHash.Length; i++)
            {
                if (ComputeHash[i] != passHasg[i]) return Unauthorized("Invalid Password");
            }

            return new IndividualDTOs
            {
                UserName = user.UserName,
                Token = _tokenService.CreateToken(user)
            };
        }




        // PUT: api/Admins/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAdmin(int id, Admin admin)
        {
            if (id != admin.Id)
            {
                return BadRequest();
            }

            _context.Entry(admin).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await AdminExists(id))
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

        //// POST: api/Admins
        //// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[HttpPost]
        //public async Task<ActionResult<Admin>> PostAdmin(Admin admin)
        //{
        //    _context.Admins.Add(admin);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction("GetAdmin", new { id = admin.Id }, admin);
        //}

        // DELETE: api/Admins/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            var admin = await _context.Admins.FindAsync(id);
            if (admin == null)
            {
                return NotFound();
            }

            _context.Admins.Remove(admin);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        //private bool AdminExists(int id)
        //{
        //    return _context.Admins.Any(e => e.Id == id);
        //}

        private async Task<bool> AdminExists(int id)
        {
            return await _context.Admins.AnyAsync(e => e.Id == id);
        }

        private async Task<bool> AdminExists(String UserNameRegistered)
        {

            return await _context.Admins.AnyAsync(e => e.UserName == UserNameRegistered);
        }

    }
}
