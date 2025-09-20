using Chainly.Application.DTOs.Suppliers;
using Chainly.Application.Services.Implementation;
using Chainly.Data;
using Chainly.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;

namespace Chainly.Testing
{
    public class SupplierServiceTests
    {
        private AppDbContext GetDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new AppDbContext(options);
        }
        [Fact]
        public async Task GetSuppliersAsync_ReturnsSuppliersSortedByDistanceAsc()
        {
            // Arrange
            var context = GetDbContext(nameof(GetSuppliersAsync_ReturnsSuppliersSortedByDistanceAsc));

            var company = new Company
            {
                Id = 1,
                Name = "Test Company",
                LocationLatitude = 30,
                LocationLongitude = 30
            };
            context.Companies.Add(company);

            var supplierNear = new Supplier
            {
                Id = 1,
                Name = "Near Supplier",
                LocationLatitude = 30.01m,
                LocationLongitude = 30.01m,
                FacilityType = "Warehouse",
                Sector = "Electronics"
            };

            var supplierFar = new Supplier
            {
                Id = 2,
                Name = "Far Supplier",
                LocationLatitude = 35m,
                LocationLongitude = 35m,
                FacilityType = "Factory",
                Sector = "Textiles"
            };

            context.Suppliers.AddRange(supplierNear, supplierFar);
            context.SaveChanges();

            var service = new SupplierService(context, null);

            // Act
            var result = await service.GetSuppliersAsync(company.Id, 1, 10, "asc");
            var okResult = Assert.IsType<OkObjectResult>(result);

            // Serialize to JSON and parse
            var json = JsonSerializer.Serialize(okResult.Value);
            using var doc = JsonDocument.Parse(json);

            // note: keys are lowercase in the anonymous object
            var items = doc.RootElement.GetProperty("Items").EnumerateArray().ToList();


            // Assert
            Assert.Equal("Near Supplier", items[0].GetProperty("Name").GetString());
            Assert.Equal("Far Supplier", items[1].GetProperty("Name").GetString());
        }

        [Fact]
        public async Task GetSuppliersAsync_ReturnsSuppliersSortedByDistanceDesc()
        {
            // Arrange
            var context = GetDbContext(nameof(GetSuppliersAsync_ReturnsSuppliersSortedByDistanceDesc));

            var company = new Company
            {
                Id = 1,
                Name = "Test Company",
                LocationLatitude = 30,
                LocationLongitude = 30
            };
            context.Companies.Add(company);

            var supplierNear = new Supplier
            {
                Id = 1,
                Name = "Near Supplier",
                LocationLatitude = 30.01m,
                LocationLongitude = 30.01m,
                FacilityType = "Warehouse",
                Sector = "Electronics"
            };

            var supplierFar = new Supplier
            {
                Id = 2,
                Name = "Far Supplier",
                LocationLatitude = 35m,
                LocationLongitude = 35m,
                FacilityType = "Factory",
                Sector = "Textiles"
            };

            context.Suppliers.AddRange(supplierNear, supplierFar);
            context.SaveChanges();

            var service = new SupplierService(context, null);

            // Act
            var result = await service.GetSuppliersAsync(company.Id, 1, 10, "desc");
            var okResult = Assert.IsType<OkObjectResult>(result);

            var json = JsonSerializer.Serialize(okResult.Value);
            using var doc = JsonDocument.Parse(json);

            var items = doc.RootElement.GetProperty("Items").EnumerateArray().ToList();

            // Assert
            Assert.Equal("Far Supplier", items[0].GetProperty("Name").GetString());
            Assert.Equal("Near Supplier", items[1].GetProperty("Name").GetString());
        }

        [Fact]
        public async Task GetSuppliersAsync_ReturnsNotFound_WhenCompanyDoesNotExist()
        {
            // Arrange
            var context = GetDbContext(nameof(GetSuppliersAsync_ReturnsNotFound_WhenCompanyDoesNotExist));
            var service = new SupplierService(context, null);

            // Act
            var result = await service.GetSuppliersAsync(999, 1, 10, "asc");

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetSupplierAsync_ReturnsSupplierAndMaterials()
        {
            // Arrange
            var context = GetDbContext(nameof(GetSupplierAsync_ReturnsSupplierAndMaterials));

            var company = new Company
            {
                Id = 1,
                Name = "Test Company",
                LocationLatitude = 30m,
                LocationLongitude = 30m
            };
            await context.Companies.AddAsync(company);

            var material1 = new Material { Id = 1, Name = "Cement", EmissionValue = 10m, Unit = "kg" };
            var material2 = new Material { Id = 2, Name = "Steel", EmissionValue = 20m, Unit = "kg" };
            await context.Materials.AddRangeAsync(material1, material2);

            var supplier = new Supplier
            {
                Id = 100,
                Name = "Sup A",
                LocationLatitude = 30.01m,
                LocationLongitude = 30.01m,
                FacilityType = "Warehouse",
                Sector = "Construction",
                CompanyId = null // available for all companies
            };

            await context.Suppliers.AddAsync(supplier);
            await context.SaveChangesAsync();

            // link supplier-materials
            var sm1 = new SupplierMaterial { SupplierId = supplier.Id, MaterialId = material1.Id };
            var sm2 = new SupplierMaterial { SupplierId = supplier.Id, MaterialId = material2.Id };
            await context.SupplierMaterials.AddRangeAsync(sm1, sm2);
            await context.SaveChangesAsync();

            var service = new SupplierService(context, null);

            // Act
            var result = await service.GetSupplierAsync(supplier.Id, company.Id, 1, 10, null);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);

            var supplierJson = doc.RootElement.GetProperty("Supplier");
            Assert.Equal("Sup A", supplierJson.GetProperty("Name").GetString());
            var materialsJson = doc.RootElement.GetProperty("Materials").GetProperty("Items").EnumerateArray().ToList();
            Assert.Equal(2, materialsJson.Count);
            // check one of the material names
            var names = materialsJson.Select(m => m.GetProperty("Name").GetString()).ToList();
            Assert.Contains("Cement", names);
            Assert.Contains("Steel", names);
        }

        [Fact]
        public async Task GetSupplierAsync_SupplierNotFound_ReturnsNotFound()
        {
            var context = GetDbContext(nameof(GetSupplierAsync_SupplierNotFound_ReturnsNotFound));
            var service = new SupplierService(context, null);

            var result = await service.GetSupplierAsync(9999, 1, 1, 10, null);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Supplier not found.", notFound.Value);
        }

        [Fact]
        public async Task GetSupplierAsync_CompanyNotFound_ReturnsNotFound()
        {
            // Arrange: create a supplier (available to all companies), but do NOT create the company
            var context = GetDbContext(nameof(GetSupplierAsync_CompanyNotFound_ReturnsNotFound));
            var supplier = new Supplier
            {
                Id = 200,
                Name = "Supplier Without Company",
                LocationLatitude = 30.0m,
                LocationLongitude = 30.0m,
                FacilityType = "Warehouse",
                Sector = "Misc"
            };
            await context.Suppliers.AddAsync(supplier);
            await context.SaveChangesAsync();

            var service = new SupplierService(context, null);

            // Act (companyId 999 does not exist)
            var result = await service.GetSupplierAsync(supplier.Id, 999, 1, 10, null);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Company not found.", notFound.Value);
        }

        [Fact]
        public async Task GetSupplierAsync_SearchFilter_FiltersMaterials()
        {
            // Arrange
            var context = GetDbContext(nameof(GetSupplierAsync_SearchFilter_FiltersMaterials));

            var company = new Company { Id = 1, Name = "C", LocationLatitude = 30m, LocationLongitude = 30m };
            await context.Companies.AddAsync(company);

            var m1 = new Material { Id = 1, Name = "Cement", EmissionValue = 1m, Unit = "kg" };
            var m2 = new Material { Id = 2, Name = "Steel Beam", EmissionValue = 2m, Unit = "kg" };
            var m3 = new Material { Id = 3, Name = "Plastic", EmissionValue = 0.5m, Unit = "kg" };
            await context.Materials.AddRangeAsync(m1, m2, m3);

            var supplier = new Supplier
            {
                Id = 300,
                Name = "Supplier M",
                LocationLatitude = 30.1m,
                LocationLongitude = 30.1m,
                FacilityType = "F",
                Sector = "S"
            };
            await context.Suppliers.AddAsync(supplier);
            await context.SaveChangesAsync();

            await context.SupplierMaterials.AddRangeAsync(
                new SupplierMaterial { SupplierId = supplier.Id, MaterialId = m1.Id },
                new SupplierMaterial { SupplierId = supplier.Id, MaterialId = m2.Id },
                new SupplierMaterial { SupplierId = supplier.Id, MaterialId = m3.Id }
            );
            await context.SaveChangesAsync();

            var service = new SupplierService(context, null);

            // Act: search for "steel" (case-insensitive)
            var result = await service.GetSupplierAsync(supplier.Id, company.Id, 1, 10, "steel");
            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);

            var items = doc.RootElement.GetProperty("Materials").GetProperty("Items").EnumerateArray().ToList();
            Assert.Single(items);
            Assert.Equal("Steel Beam", items[0].GetProperty("Name").GetString());
        }

        [Fact]
        public async Task GetSupplierAsync_Pagination_Works()
        {
            // Arrange
            var context = GetDbContext(nameof(GetSupplierAsync_Pagination_Works));

            var company = new Company { Id = 1, Name = "C", LocationLatitude = 30m, LocationLongitude = 30m };
            await context.Companies.AddAsync(company);

            // create 3 materials
            var materials = new[]
            {
                new Material { Id = 10, Name = "M1", EmissionValue = 1m, Unit = "u" },
                new Material { Id = 11, Name = "M2", EmissionValue = 1m, Unit = "u" },
                new Material { Id = 12, Name = "M3", EmissionValue = 1m, Unit = "u" }
            };
            await context.Materials.AddRangeAsync(materials);

            var supplier = new Supplier
            {
                Id = 400,
                Name = "Supplier P",
                LocationLatitude = 30.1m,
                LocationLongitude = 30.1m,
                FacilityType = "F",
                Sector = "S"
            };
            await context.Suppliers.AddAsync(supplier);
            await context.SaveChangesAsync();

            await context.SupplierMaterials.AddRangeAsync(
                new SupplierMaterial { SupplierId = supplier.Id, MaterialId = 10 },
                new SupplierMaterial { SupplierId = supplier.Id, MaterialId = 11 },
                new SupplierMaterial { SupplierId = supplier.Id, MaterialId = 12 }
            );
            await context.SaveChangesAsync();

            var service = new SupplierService(context, null);

            // Act page 1 (pageSize=2)
            var result1 = await service.GetSupplierAsync(supplier.Id, company.Id, 1, 2, null);
            var ok1 = Assert.IsType<OkObjectResult>(result1);
            var json1 = JsonSerializer.Serialize(ok1.Value);
            using var doc1 = JsonDocument.Parse(json1);
            var items1 = doc1.RootElement.GetProperty("Materials").GetProperty("Items").EnumerateArray().ToList();
            Assert.Equal(2, items1.Count);

            // Act page 2
            var result2 = await service.GetSupplierAsync(supplier.Id, company.Id, 2, 2, null);
            var ok2 = Assert.IsType<OkObjectResult>(result2);
            var json2 = JsonSerializer.Serialize(ok2.Value);
            using var doc2 = JsonDocument.Parse(json2);
            var items2 = doc2.RootElement.GetProperty("Materials").GetProperty("Items").EnumerateArray().ToList();
            Assert.Single(items2);
        }


        // --------------------
        //  CreateSupplierAsync tests
        // --------------------
        [Fact]
        public async Task CreateSupplierAsync_ValidCreate_AddsSupplier()
        {
            // Arrange
            var context = GetDbContext(nameof(CreateSupplierAsync_ValidCreate_AddsSupplier));
            var service = new SupplierService(context, null);

            var company = new Company { Id = 1, Name = "Test Company" };
            context.Companies.Add(company);
            await context.SaveChangesAsync();

            var dto = new SupplierCreateDto
            {
                Name = "Supplier1",
                Latitude = 10,
                Longitude = 20,
                FacilityType = "Factory",
                Sector = "IT",
                MaterialIds = null
            };

            // Act
            var result = await service.CreateSupplierAsync(dto, company.Id);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);

            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);

            var message = doc.RootElement.GetProperty("Message").GetString();
            Assert.Equal("Supplier created successfully", message);

            var supplierId = doc.RootElement.GetProperty("SupplierId").GetInt32();
            Assert.True(supplierId > 0);

            var supplier = await context.Suppliers.FindAsync(supplierId);
            Assert.NotNull(supplier);
            Assert.Equal("Supplier1", supplier.Name);
        }



        [Fact]
        public async Task CreateSupplierAsync_CompanyNotFound_ReturnsNotFound()
        {
            // Arrange
            var context = GetDbContext(nameof(CreateSupplierAsync_CompanyNotFound_ReturnsNotFound));
            var service = new SupplierService(context, null);

            var dto = new SupplierCreateDto
            {
                Name = "Invalid",
                Latitude = 1,
                Longitude = 1,
                FacilityType = "F",
                Sector = "S", 
                MaterialIds = null
            };

            // Act
            var result = await service.CreateSupplierAsync(dto , 999);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Company not found.", notFound.Value);
        }

        [Fact]
        public async Task CreateSupplierAsync_WithMaterials_AddsSupplierMaterials()
        {
            // Arrange
            var context = GetDbContext(nameof(CreateSupplierAsync_WithMaterials_AddsSupplierMaterials));

            var company = new Company { Id = 1, Name = "TestCo" };
            await context.Companies.AddAsync(company);

            var mat1 = new Material { Id = 101, Name = "Steel", EmissionValue = 1m, Unit = "kg" };
            var mat2 = new Material { Id = 102, Name = "Cement", EmissionValue = 2m, Unit = "kg" };
            await context.Materials.AddRangeAsync(mat1, mat2);

            await context.SaveChangesAsync();

            var service = new SupplierService(context, null);

            var dto = new SupplierCreateDto
            {
                Name = "Supplier With Mats",
                Latitude = 11,
                Longitude = 22,
                FacilityType = "Warehouse",
                Sector = "Building",
                MaterialIds = new List<int> { mat1.Id, mat2.Id }
            };

            // Act
            var result = await service.CreateSupplierAsync(dto, company.Id);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);

            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);

            var message = doc.RootElement.GetProperty("Message").GetString();
            Assert.Equal("Supplier created successfully", message);

            var supplierId = doc.RootElement.GetProperty("SupplierId").GetInt32();
            Assert.True(supplierId > 0);

            var supplierInDb = await context.Suppliers
                .Include(s => s.SupplierMaterials)
                .FirstOrDefaultAsync();

            Assert.NotNull(supplierInDb);
            Assert.Equal(2, supplierInDb.SupplierMaterials.Count);
            Assert.Contains(supplierInDb.SupplierMaterials, sm => sm.MaterialId == mat1.Id);
            Assert.Contains(supplierInDb.SupplierMaterials, sm => sm.MaterialId == mat2.Id);
        }


        // --------------------
        //  UpdateSupplierAsync tests
        // --------------------

        [Fact]
        public async Task UpdateSupplierAsync_ValidUpdate_ChangesSupplierAndMaterials()
        {
            // Arrange
            var context = GetDbContext(nameof(UpdateSupplierAsync_ValidUpdate_ChangesSupplierAndMaterials));

            var company = new Company { Id = 1, Name = "C", LocationLatitude = 30m, LocationLongitude = 30m };
            await context.Companies.AddAsync(company);

            var initialMaterial = new Material { Id = 21, Name = "OldMat", EmissionValue = 1m, Unit = "u" };
            var newMaterial = new Material { Id = 22, Name = "NewMat", EmissionValue = 2m, Unit = "u" };
            await context.Materials.AddRangeAsync(initialMaterial, newMaterial);

            var supplier = new Supplier
            {
                Id = 500,
                Name = "ToUpdate",
                LocationLatitude = 30m,
                LocationLongitude = 30m,
                FacilityType = "OldF",
                Sector = "OldS",
                CompanyId = company.Id
            };
            await context.Suppliers.AddAsync(supplier);
            await context.SaveChangesAsync();

            await context.SupplierMaterials.AddAsync(new SupplierMaterial { SupplierId = supplier.Id, MaterialId = initialMaterial.Id });
            await context.SaveChangesAsync();

            var service = new SupplierService(context, null);

            var dto = new SupplierUpdateDto
            {
                Name = "UpdatedName",
                Latitude = 31,
                Longitude = 31,
                FacilityType = "NewF",
                Sector = "NewS",
                MaterialIds = new List<int> { newMaterial.Id } 
            };

            // Act
            var result = await service.UpdateSupplierAsync(supplier.Id, dto, company.Id);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("Supplier updated successfully", doc.RootElement.GetProperty("Message").GetString());

            var updated = await context.Suppliers.Include(s => s.SupplierMaterials).FirstAsync(s => s.Id == supplier.Id);
            Assert.Equal("UpdatedName", updated.Name);
            Assert.Equal("NewF", updated.FacilityType);
            // materials replaced
            Assert.Single(updated.SupplierMaterials);
            Assert.Equal(newMaterial.Id, updated.SupplierMaterials.First().MaterialId);
        }

        [Fact]
        public async Task UpdateSupplierAsync_SupplierNotFound_ReturnsNotFound()
        {
            var context = GetDbContext(nameof(UpdateSupplierAsync_SupplierNotFound_ReturnsNotFound));
            var service = new SupplierService(context, null);

            var dto = new SupplierUpdateDto { Name = "X", Latitude = 1, Longitude = 1, MaterialIds = null };
            var result = await service.UpdateSupplierAsync(9999, dto, 1);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateSupplierAsync_CompanyMismatch_ReturnsNotFound()
        {
            var context = GetDbContext(nameof(UpdateSupplierAsync_CompanyMismatch_ReturnsNotFound));
            var otherCompany = new Company { Id = 2, Name = "Other", LocationLatitude = 0m, LocationLongitude = 0m };
            await context.Companies.AddAsync(otherCompany);

            var supplier = new Supplier
            {
                Id = 600,
                Name = "S",
                LocationLatitude = 0m,
                LocationLongitude = 0m,
                FacilityType = "F",
                Sector = "S",
                CompanyId = otherCompany.Id 
            };
            await context.Suppliers.AddAsync(supplier);
            await context.SaveChangesAsync();

            var service = new SupplierService(context, null);
            var dto = new SupplierUpdateDto { Name = "X", Latitude = 1, Longitude = 1, MaterialIds = null };

            var result = await service.UpdateSupplierAsync(supplier.Id, dto, companyId: 1); 
            Assert.IsType<NotFoundObjectResult>(result);
        }

        // --------------------
        //  DeleteSupplierAsync tests
        // --------------------

        [Fact]
        public async Task DeleteSupplierAsync_ValidDelete_RemovesSupplier()
        {
            var context = GetDbContext(nameof(DeleteSupplierAsync_ValidDelete_RemovesSupplier));
            var company = new Company { Id = 1, Name = "C" };
            await context.Companies.AddAsync(company);

            var supplier = new Supplier
            {
                Id = 700,
                Name = "ToDelete",
                LocationLatitude = 0m,
                LocationLongitude = 0m,
                FacilityType = "F",
                Sector = "S",
                CompanyId = company.Id
            };
            await context.Suppliers.AddAsync(supplier);
            await context.SaveChangesAsync();

            var service = new SupplierService(context, null);
            var result = await service.DeleteSupplierAsync(supplier.Id, company.Id);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Supplier deleted successfully", ok.Value);

            var inDb = await context.Suppliers.FindAsync(supplier.Id);
            Assert.Null(inDb);
        }

        [Fact]
        public async Task DeleteSupplierAsync_SupplierNotFound_ReturnsNotFound()
        {
            var context = GetDbContext(nameof(DeleteSupplierAsync_SupplierNotFound_ReturnsNotFound));
            var service = new SupplierService(context, null);

            var result = await service.DeleteSupplierAsync(9999, 1);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteSupplierAsync_CompanyMismatch_ReturnsNotFound()
        {
            var context = GetDbContext(nameof(DeleteSupplierAsync_CompanyMismatch_ReturnsNotFound));
            var company = new Company { Id = 1, Name = "C" };
            var otherCompany = new Company { Id = 2, Name = "Other" };
            await context.Companies.AddRangeAsync(company, otherCompany);

            var supplier = new Supplier
            {
                Id = 800,
                Name = "S",
                LocationLatitude = 0m,
                LocationLongitude = 0m,
                FacilityType = "F",
                Sector = "S",
                CompanyId = otherCompany.Id 
            };
            await context.Suppliers.AddAsync(supplier);
            await context.SaveChangesAsync();

            var service = new SupplierService(context, null);
            var result = await service.DeleteSupplierAsync(supplier.Id, company.Id); 
            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
