namespace Chainly.Domain.Constants;

public static class Permissions
{
    public static Dictionary<string, List<string>> GenerateAllPermissions()
    {
        return typeof(Permissions)
            .GetNestedTypes()
            .ToDictionary(
                t => t.Name,
                t => t.GetFields()
                    .Select(f => f.GetValue(null))
                    .OfType<string>()
                    .ToList()
            );
    }


    public static class Users
    {
        public const string View = "Users.View";
        public const string Create = "Users.Create";
        public const string Update = "Users.Update";
        public const string Delete = "Users.Delete";
        public const string Activate = "Users.Activate";
    }

    public static class Roles
    {
        public const string View = "Roles.View";
        public const string Create = "Roles.Create";
        public const string Update = "Roles.Update";
        public const string Delete = "Roles.Delete";
    }

    public static class Suppliers
    {
        public const string View = "Suppliers.View";
        public const string Create = "Suppliers.Create";
        public const string Update = "Suppliers.Update";
        public const string Delete = "Suppliers.Delete";
        public const string Import = "Suppliers.Import";
    }

    public static class Materials
    {
        public const string View = "Materials.View";
        public const string Create = "Materials.Create";
        public const string Update = "Materials.Update";
        public const string Delete = "Materials.Delete";
    }

    public static class ProductionLines
    {
        public const string View = "ProductionLines.View";
        public const string Create = "ProductionLines.Create";
        public const string Update = "ProductionLines.Update";
        public const string Delete = "ProductionLines.Delete";
    }

    public static class Reports
    {
        public const string View = "Reports.View";
        public const string Create = "Reports.Create";
        public const string Update = "Reports.Update";
        public const string Delete = "Reports.Delete";
    }

    public static class Products
    {
        public const string View = "Products.View";
        public const string Create = "Products.Create";
        public const string Update = "Products.Update";
        public const string Delete = "Products.Delete";
        public const string Import = "Products.Import";
    }

    public static class Orders
    {
        public const string View = "Orders.View";
        public const string Create = "Orders.Create";
        public const string Update = "Orders.Update";
        public const string Delete = "Orders.Delete";
        public const string Import = "Orders.Import";
    }

    public static class Cameras
    {
        public const string View = "Cameras.View";
        public const string Create = "Cameras.Create";
        public const string Update = "Cameras.Update";
        public const string Delete = "Cameras.Delete";
    }
}