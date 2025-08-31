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
        public async Task<PagedResult<SupplierDto>> GetSuppliersAsync(string userId, int? pageNumber = null, int? pageSize = null)
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
                    ProductType = s.ProductType,
                    Sector = s.Sector
                }).ToListAsync();

            
            foreach (var supplier in suppliers)
            {
                if (TryParseCoords(company.LocationLatitude, company.LocationLongitude,
                                   supplier.Latitude, supplier.Longitude,
                                   out double cLat, out double cLon, out double sLat, out double sLon))
                {
                    supplier.Distance = CalculateDistance(cLat, cLon, sLat, sLon);
                }
                else
                {
                    supplier.Distance = double.MaxValue;
                }
            }

            var sortedSuppliers = suppliers.OrderBy(s => s.Distance);

            // Paging
            if (pageNumber.HasValue && pageSize.HasValue && pageNumber > 0 && pageSize > 0)
            {
                var totalCount = sortedSuppliers.Count();
                var items = sortedSuppliers
                    .Skip(((int)pageNumber - 1) * (int)pageSize)
                    .Take((int)pageSize)
                    .ToList();

                return new PagedResult<SupplierDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = pageNumber.Value,
                    PageSize = pageSize.Value
                };
            }

            return new PagedResult<SupplierDto>
            {
                Items = sortedSuppliers.ToList(),
                TotalCount = sortedSuppliers.Count()
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
                ProductType = string.IsNullOrEmpty(dto.ProductType) ? "General" : dto.ProductType,
                Sector = string.IsNullOrEmpty(dto.Sector) ? "Other" : dto.Sector
            };

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            double distance = 0;
            if (TryParseCoords(company.LocationLatitude, company.LocationLongitude,
                               supplier.LocationLatitude, supplier.LocationLongitude,
                               out double cLat, out double cLon, out double sLat, out double sLon))
            {
                distance = CalculateDistance(cLat, cLon, sLat, sLon);
            }

            return new SupplierDto
            {
                Id = supplier.Id,
                Name = supplier.Name,
                Latitude = supplier.LocationLatitude,
                Longitude = supplier.LocationLongitude,
                CompanyId = supplier.CompanyId,
                FacilityType = supplier.FacilityType,
                ProductType = supplier.ProductType,
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
            supplier.ProductType = string.IsNullOrEmpty(dto.ProductType) ? supplier.ProductType : dto.ProductType;
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

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // km
            var dLat = (lat2 - lat1) * Math.PI / 180.0;
            var dLon = (lon2 - lon1) * Math.PI / 180.0;

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private bool TryParseCoords(string? lat1, string? lon1, string? lat2, string? lon2,
                              out double dLat1, out double dLon1, out double dLat2, out double dLon2)
        {
            bool ok1 = double.TryParse(lat1, out dLat1);
            bool ok2 = double.TryParse(lon1, out dLon1);
            bool ok3 = double.TryParse(lat2, out dLat2);
            bool ok4 = double.TryParse(lon2, out dLon2);

            return ok1 && ok2 && ok3 && ok4;
        }

    }
}
