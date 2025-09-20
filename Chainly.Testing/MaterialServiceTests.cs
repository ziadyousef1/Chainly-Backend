using Chainly.Application.Services.Implementation;
using Chainly.Data;
using Chainly.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Chainly.Testing
{
    public class MaterialServiceTests
    {
        private AppDbContext GetDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new AppDbContext(options);
        }
        [Fact]
        public async Task GetAllMaterialsAsync_ReturnsPagedMaterials()
        {
            // Arrange
            var context = GetDbContext(nameof(GetAllMaterialsAsync_ReturnsPagedMaterials));
            context.Materials.AddRange(
                new Material { Id = 1, Name = "Steel", EmissionValue = 10, Unit = "kg" },
                new Material { Id = 2, Name = "Cement", EmissionValue = 20, Unit = "kg" },
                new Material { Id = 3, Name = "Plastic", EmissionValue = 5, Unit = "kg" }
            );
            await context.SaveChangesAsync();

            var service = new MaterialService(context);

            // Act
            var result = await service.GetAllMaterialsAsync(companyId: 1, pageNumber: 1, pageSize: 2);
            var ok = Assert.IsType<OkObjectResult>(result);

            // Serialize to JSON → parse
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Assert
            Assert.Equal(3, root.GetProperty("TotalCount").GetInt32());
            Assert.Equal(2, root.GetProperty("PageSize").GetInt32());
            Assert.Equal(2, root.GetProperty("TotalPages").GetInt32());

            var items = root.GetProperty("Items").EnumerateArray().ToList();
            Assert.Equal(2, items.Count);
        }

        [Fact]
        public async Task GetAllMaterialsAsync_WithSearch_FiltersByName()
        {
            // Arrange
            var context = GetDbContext(nameof(GetAllMaterialsAsync_WithSearch_FiltersByName));
            context.Materials.AddRange(
                new Material { Id = 1, Name = "Steel", EmissionValue = 10, Unit = "kg" },
                new Material { Id = 2, Name = "Cement", EmissionValue = 20, Unit = "kg" }
            );
            await context.SaveChangesAsync();

            var service = new MaterialService(context);

            // Act
            var result = await service.GetAllMaterialsAsync(companyId: 1, search: "steel");
            var ok = Assert.IsType<OkObjectResult>(result);

            // Convert to JSON
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Assert
            var items = root.GetProperty("Items").EnumerateArray().ToList();
            Assert.Single(items);
            Assert.Equal("Steel", items[0].GetProperty("Name").GetString());
        }

        [Theory]
        [InlineData("asc", "Plastic", "Cement")]
        [InlineData("desc", "Cement", "Plastic")]
        public async Task GetAllMaterialsAsync_SortsByEmissionValue(string sortOrder, string expectedFirst, string expectedLast)
        {
            // Arrange
            var context = GetDbContext(nameof(GetAllMaterialsAsync_SortsByEmissionValue) + sortOrder);
            context.Materials.AddRange(
                new Material { Id = 1, Name = "Steel", EmissionValue = 10, Unit = "kg" },
                new Material { Id = 2, Name = "Cement", EmissionValue = 20, Unit = "kg" },
                new Material { Id = 3, Name = "Plastic", EmissionValue = 5, Unit = "kg" }
            );
            await context.SaveChangesAsync();

            var service = new MaterialService(context);

            // Act
            var result = await service.GetAllMaterialsAsync(companyId: 1, sortOrder: sortOrder);
            var ok = Assert.IsType<OkObjectResult>(result);

            // Convert to JSON
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            var items = doc.RootElement.GetProperty("Items").EnumerateArray().ToList();

            // Assert
            Assert.Equal(expectedFirst, items.First().GetProperty("Name").GetString());
            Assert.Equal(expectedLast, items.Last().GetProperty("Name").GetString());
        }

        [Fact]
        public async Task GetMaterialByIdAsync_MaterialNotFound_ReturnsNotFound()
        {
            // Arrange
            var context = GetDbContext(nameof(GetMaterialByIdAsync_MaterialNotFound_ReturnsNotFound));
            var service = new MaterialService(context);

            // Act
            var result = await service.GetMaterialByIdAsync(id: 99, companyId: 1);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);

            // Convert result to JSON and check message
            var json = JsonSerializer.Serialize(notFound.Value);
            using var doc = JsonDocument.Parse(json);
            var message = doc.RootElement.GetProperty("message").GetString();

            // Assert
            Assert.Equal("Material not found", message);
        }
        [Fact]
        public async Task GetMaterialByIdAsync_CompanyNotFound_ReturnsBadRequest()
        {
            // Arrange
            var context = GetDbContext(nameof(GetMaterialByIdAsync_CompanyNotFound_ReturnsBadRequest));
            context.Materials.Add(new Material { Id = 1, Name = "Steel", EmissionValue = 10, Unit = "kg" });
            await context.SaveChangesAsync();

            var service = new MaterialService(context);

            // Act
            var result = await service.GetMaterialByIdAsync(id: 1, companyId: 99);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);

            var json = JsonSerializer.Serialize(badRequest.Value);
            using var doc = JsonDocument.Parse(json);
            var message = doc.RootElement.GetProperty("message").GetString();

            // Assert
            Assert.Equal("Company not found", message);
        }
        [Fact]
        public async Task GetMaterialByIdAsync_ValidRequest_ReturnsMaterialWithSuppliers()
        {
            // Arrange
            var context = GetDbContext(nameof(GetMaterialByIdAsync_ValidRequest_ReturnsMaterialWithSuppliers));

            var company = new Company { Id = 1, Name = "Main Co", LocationLatitude = 30, LocationLongitude = 30 };
            var supplier = new Supplier { Id = 1, Name = "Supplier A", CompanyId = 1, LocationLatitude = 31, LocationLongitude = 31,
                FacilityType = "Factory",   
                Sector = "Manufacturing"
            };
            var material = new Material { Id = 1, Name = "Steel", EmissionValue = 10, Unit = "kg" };

            context.Companies.Add(company);
            context.Suppliers.Add(supplier);
            context.Materials.Add(material);
            context.SupplierMaterials.Add(new SupplierMaterial { SupplierId = 1, MaterialId = 1 });
            await context.SaveChangesAsync();

            var service = new MaterialService(context);

            // Act
            var result = await service.GetMaterialByIdAsync(id: 1, companyId: 1);
            var ok = Assert.IsType<OkObjectResult>(result);

            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            var item = doc.RootElement.GetProperty("Item");

            // Assert
            Assert.Equal("Steel", item.GetProperty("Name").GetString());
            Assert.NotEmpty(item.GetProperty("Suppliers").EnumerateArray());
        }
        [Fact]
        public async Task GetMaterialByIdAsync_WithSearch_FiltersSuppliers()
        {
            // Arrange
            var context = GetDbContext(nameof(GetMaterialByIdAsync_WithSearch_FiltersSuppliers));

            var company = new Company { Id = 1, Name = "Main Co", LocationLatitude = 30, LocationLongitude = 30 };
            var supplier1 = new Supplier { Id = 1, Name = "Supplier A", CompanyId = 1, LocationLatitude = 31, LocationLongitude = 31 ,
                FacilityType = "Factory",   
                Sector = "Manufacturing"
            };
            var supplier2 = new Supplier { Id = 2, Name = "Other Supplier", CompanyId = 1, LocationLatitude = 32, LocationLongitude = 32 ,
                FacilityType = "Factory",   
                Sector = "Manufacturing"
            };
            var material = new Material { Id = 1, Name = "Steel", EmissionValue = 10, Unit = "kg" };

            context.Companies.Add(company);
            context.Suppliers.AddRange(supplier1, supplier2);
            context.Materials.Add(material);
            context.SupplierMaterials.AddRange(
                new SupplierMaterial { SupplierId = 1, MaterialId = 1 },
                new SupplierMaterial { SupplierId = 2, MaterialId = 1 }
            );
            await context.SaveChangesAsync();

            var service = new MaterialService(context);

            // Act
            var result = await service.GetMaterialByIdAsync(id: 1, companyId: 1, search: "Supplier A");
            var ok = Assert.IsType<OkObjectResult>(result);

            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            var suppliers = doc.RootElement.GetProperty("Item").GetProperty("Suppliers").EnumerateArray().ToList();

            // Assert
            Assert.Single(suppliers);
            Assert.Equal("Supplier A", suppliers[0].GetProperty("Name").GetString());
        }

        [Theory]
        [InlineData("asc", "Near Supplier", "Far Supplier")]
        [InlineData("desc", "Far Supplier", "Near Supplier")]
        public async Task GetMaterialByIdAsync_SortsSuppliersByFootprintValue(string sortOrder, string expectedFirst, string expectedLast)
        {
            // Arrange
            var context = GetDbContext(nameof(GetMaterialByIdAsync_SortsSuppliersByFootprintValue) + sortOrder);

            var company = new Company
            {
                Id = 1,
                Name = "MainCo",
                LocationLatitude = 30,
                LocationLongitude = 30
            };
            await context.Companies.AddAsync(company);

            var material = new Material { Id = 10, Name = "Steel", EmissionValue = 2m, Unit = "kg" };
            await context.Materials.AddAsync(material);

            var nearSupplier = new Supplier
            {
                Id = 1,
                Name = "Near Supplier",
                LocationLatitude = 30.01m,
                LocationLongitude = 30.01m,
                CompanyId = company.Id,
                FacilityType = "Factory",
                Sector = "Manufacturing"
            };
            var farSupplier = new Supplier
            {
                Id = 2,
                Name = "Far Supplier",
                LocationLatitude = 40m,
                LocationLongitude = 40m,
                CompanyId = company.Id,
                FacilityType = "Factory",
                Sector = "Manufacturing"
            };
            await context.Suppliers.AddRangeAsync(nearSupplier, farSupplier);

            await context.SupplierMaterials.AddRangeAsync(
                new SupplierMaterial { SupplierId = nearSupplier.Id, MaterialId = material.Id },
                new SupplierMaterial { SupplierId = farSupplier.Id, MaterialId = material.Id }
            );

            await context.SaveChangesAsync();

            var service = new MaterialService(context);

            // Act
            var result = await service.GetMaterialByIdAsync(material.Id, company.Id, sortOrder: sortOrder);
            var ok = Assert.IsType<OkObjectResult>(result);

            // Parse result via JSON instead of dynamic
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            var item = doc.RootElement.GetProperty("Item");
            var suppliers = item.GetProperty("Suppliers").EnumerateArray().ToList();

            // Assert
            Assert.Equal(expectedFirst, suppliers.First().GetProperty("Name").GetString());
            Assert.Equal(expectedLast, suppliers.Last().GetProperty("Name").GetString());
        }

    }
}
