using Chainly.Application.DTOs;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Chainly.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chainly.Application.Services.Implementation
{
    public class SupplierService : ISupplierService
    {
        private readonly AppDbContext _context;

        public SupplierService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<SupplierDto>> GetSuppliersAsync(string userId, int? pageNumber = null,
            int? pageSize = null)
        {
            var user = await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

            if (user?.Company == null)
            {
                return new PagedResult<SupplierDto>
                {
                    Items = Enumerable.Empty<SupplierDto>(),
                    TotalCount = 0
                };
            }

            var company = user.Company;

            var suppliers = await _context.Suppliers
                .Where(s => s.CompanyId == null || s.CompanyId == company.Id)
                .Select(s => new SupplierDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Latitude = s.LocationLatitude,
                    Longitude = s.LocationLongitude,
                    CompanyId = s.CompanyId,
                    FacilityType = s.FacilityType,
                    Sector = s.Sector,
                    Distance = CalculateDistance(company.LocationLatitude, company.LocationLongitude, s.LocationLatitude, s.LocationLongitude)
                })
                .OrderBy(s => s.Distance)
                .ToListAsync();


            // Paging
            if (pageNumber.HasValue && pageSize.HasValue && pageNumber > 0 && pageSize > 0)
            {
                var skip = (pageNumber.Value - 1) * pageSize.Value;
                var pagedItems = suppliers.Skip(skip).Take(pageSize.Value).ToList();

                return new PagedResult<SupplierDto>
                {
                    Items = pagedItems,
                    TotalCount = suppliers.Count,
                    PageNumber = pageNumber.Value,
                    PageSize = pageSize.Value
                };
            }


            return new PagedResult<SupplierDto>
            {
                Items = suppliers,
                TotalCount = suppliers.Count
            };
        }

        public async Task<SupplierDto?> CreateSupplierAsync(SupplierDto dto, int companyId)
        {
            var company = await _context.Companies.FindAsync(companyId);
            if (company == null) return null;

            var supplier = new Supplier
            {
                Name = dto.Name,
                LocationLatitude = dto.Latitude,
                LocationLongitude = dto.Longitude,
                CompanyId = companyId,
                FacilityType = string.IsNullOrEmpty(dto.FacilityType) ? "Unknown" : dto.FacilityType,
                Sector = string.IsNullOrEmpty(dto.Sector) ? "Other" : dto.Sector
            };

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            decimal distance = 0;

            distance = CalculateDistance(company.LocationLatitude, company.LocationLongitude, supplier.LocationLatitude,
                supplier.LocationLongitude);


            return new SupplierDto
            {
                Id = supplier.Id,
                Name = supplier.Name,
                Latitude = supplier.LocationLatitude,
                Longitude = supplier.LocationLongitude,
                CompanyId = supplier.CompanyId,
                FacilityType = supplier.FacilityType,
                Sector = supplier.Sector,
                Distance = distance
            };
        }

        public async Task<bool> UpdateSupplierAsync(int id, SupplierDto dto, int companyId)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null || supplier.CompanyId != companyId) return false;

            supplier.Name = dto.Name;
            supplier.LocationLatitude = dto.Latitude;
            supplier.LocationLongitude = dto.Longitude;
            supplier.FacilityType = string.IsNullOrEmpty(dto.FacilityType) ? supplier.FacilityType : dto.FacilityType;
            supplier.Sector = string.IsNullOrEmpty(dto.Sector) ? supplier.Sector : dto.Sector;

            _context.Suppliers.Update(supplier);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteSupplierAsync(int id, int companyId)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null || supplier.CompanyId != companyId) return false;

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();
            return true;
        }

        private decimal CalculateDistance(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
        {
            const double R = 6371; // Radius of the earth in km
            var dLat = (double)(lat2 - lat1) * Math.PI / 180.0;
            var dLon = (double)(lon2 - lon1) * Math.PI / 180.0;

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos((double)lat1 * Math.PI / 180.0) *
                    Math.Cos((double)lat2 * Math.PI / 180.0) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return (decimal)(R * c);
        }
    }
}