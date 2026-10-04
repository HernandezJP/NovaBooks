using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using NovaBooks.Infrastructure.Data.Identity;
using NovaBooks.Infrastructure.Security.Permissions;

namespace NovaBooks.Infrastructure.Data.Seed;

public static class IdentitySeeder
{
    private const string AdministratorRole = "Administrador";
    private const string SupervisorRole = "Supervisor";
    private const string CashierRole = "Cajero";
    private const string InventoryRole = "Inventario";
    private const string PurchasingRole = "Compras";

    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration)
    {
        await CreateRolesAsync(roleManager);

        await AssignRolePermissionsAsync(
            roleManager,
            AdministratorRole,
            SystemPermissions.GetAll());

        await AssignRolePermissionsAsync(
            roleManager,
            SupervisorRole,
            GetSupervisorPermissions());

        await AssignRolePermissionsAsync(
            roleManager,
            CashierRole,
            GetCashierPermissions());

        await AssignRolePermissionsAsync(
            roleManager,
            InventoryRole,
            GetInventoryPermissions());

        await AssignRolePermissionsAsync(
            roleManager,
            PurchasingRole,
            GetPurchasingPermissions());

        await CreateAdministratorAsync(
            userManager,
            configuration);
    }

    private static async Task CreateRolesAsync(
        RoleManager<ApplicationRole> roleManager)
    {
        await CreateRoleAsync(
            roleManager,
            AdministratorRole,
            "Acceso completo al sistema.",
            true);

        await CreateRoleAsync(
            roleManager,
            SupervisorRole,
            "Supervisa las operaciones de ventas, caja e inventario.",
            true);

        await CreateRoleAsync(
            roleManager,
            CashierRole,
            "Realiza ventas y operaciones de caja.",
            true);

        await CreateRoleAsync(
            roleManager,
            InventoryRole,
            "Administra existencias y movimientos de inventario.",
            true);

        await CreateRoleAsync(
            roleManager,
            PurchasingRole,
            "Administra proveedores, compras y recepciones.",
            true);
    }

    private static async Task CreateRoleAsync(
        RoleManager<ApplicationRole> roleManager,
        string roleName,
        string description,
        bool isSystem)
    {
        ApplicationRole? existingRole =
            await roleManager.FindByNameAsync(roleName);

        if (existingRole is not null)
        {
            bool requiresUpdate =
                existingRole.ROL_Descripcion != description ||
                existingRole.ROL_EsSistema != isSystem ||
                !existingRole.ROL_Activo;

            if (!requiresUpdate)
            {
                return;
            }

            existingRole.ROL_Descripcion = description;
            existingRole.ROL_EsSistema = isSystem;
            existingRole.ROL_Activo = true;
            existingRole.ROL_FechaModificacion = DateTime.UtcNow;

            IdentityResult updateResult =
                await roleManager.UpdateAsync(existingRole);

            ThrowIfFailed(
                updateResult,
                $"actualizar el rol {roleName}");

            return;
        }

        var role = new ApplicationRole
        {
            Name = roleName,
            ROL_Descripcion = description,
            ROL_EsSistema = isSystem,
            ROL_Activo = true,
            ROL_FechaCreacion = DateTime.UtcNow
        };

        IdentityResult createResult =
            await roleManager.CreateAsync(role);

        ThrowIfFailed(
            createResult,
            $"crear el rol {roleName}");
    }

    private static async Task AssignRolePermissionsAsync(
        RoleManager<ApplicationRole> roleManager,
        string roleName,
        IEnumerable<string> permissions)
    {
        ApplicationRole? role =
            await roleManager.FindByNameAsync(roleName);

        if (role is null)
        {
            throw new InvalidOperationException(
                $"No se encontró el rol '{roleName}'.");
        }

        IList<Claim> currentClaims =
            await roleManager.GetClaimsAsync(role);

        HashSet<string> expectedPermissions =
            permissions.ToHashSet(
                StringComparer.OrdinalIgnoreCase);

        List<Claim> permissionClaims = currentClaims
            .Where(x =>
                x.Type == CustomClaimTypes.Permission)
            .ToList();

        foreach (Claim claim in permissionClaims)
        {
            if (expectedPermissions.Contains(claim.Value))
            {
                continue;
            }

            IdentityResult removeResult =
                await roleManager.RemoveClaimAsync(role, claim);

            ThrowIfFailed(
                removeResult,
                $"eliminar el permiso {claim.Value} " +
                $"del rol {roleName}");
        }

        HashSet<string> currentPermissions =
            permissionClaims
                .Select(x => x.Value)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        foreach (string permission in expectedPermissions)
        {
            if (currentPermissions.Contains(permission))
            {
                continue;
            }

            var claim = new Claim(
                CustomClaimTypes.Permission,
                permission);

            IdentityResult addResult =
                await roleManager.AddClaimAsync(role, claim);

            ThrowIfFailed(
                addResult,
                $"agregar el permiso {permission} " +
                $"al rol {roleName}");
        }
    }

    private static async Task CreateAdministratorAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        string email =
            configuration["IdentitySeed:Administrator:Email"]
            ?? "admin@novabooks.local";

        string userName =
            configuration["IdentitySeed:Administrator:UserName"]
            ?? "admin";

        string? password =
            configuration[
                "IdentitySeed:Administrator:Password"];

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "No se configuró la contraseña inicial del " +
                "administrador. Configure " +
                "'IdentitySeed:Administrator:Password'.");
        }

        ApplicationUser? administrator =
            await userManager.FindByNameAsync(userName);

        administrator ??=
            await userManager.FindByEmailAsync(email);

        if (administrator is null)
        {
            administrator = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                PhoneNumberConfirmed = false,
                LockoutEnabled = true,
                USU_Activo = true,
                USU_DebeCambiarPassword = true,
                USU_FechaCreacion = DateTime.UtcNow
            };

            IdentityResult createResult =
                await userManager.CreateAsync(
                    administrator,
                    password);

            ThrowIfFailed(
                createResult,
                "crear el usuario administrador");
        }
        else
        {
            bool requiresUpdate = false;

            if (administrator.UserName != userName)
            {
                administrator.UserName = userName;
                requiresUpdate = true;
            }

            if (administrator.Email != email)
            {
                administrator.Email = email;
                administrator.EmailConfirmed = true;
                requiresUpdate = true;
            }

            if (!administrator.USU_Activo)
            {
                administrator.USU_Activo = true;
                requiresUpdate = true;
            }

            if (requiresUpdate)
            {
                administrator.USU_FechaModificacion =
                    DateTime.UtcNow;

                IdentityResult updateResult =
                    await userManager.UpdateAsync(
                        administrator);

                ThrowIfFailed(
                    updateResult,
                    "actualizar el usuario administrador");
            }
        }

        if (!await userManager.IsInRoleAsync(
                administrator,
                AdministratorRole))
        {
            IdentityResult addRoleResult =
                await userManager.AddToRoleAsync(
                    administrator,
                    AdministratorRole);

            ThrowIfFailed(
                addRoleResult,
                "asignar el rol Administrador");
        }
    }

    private static IReadOnlyCollection<string>
        GetSupervisorPermissions()
    {
        return
        [
            UserPermissions.View,
            RolePermissions.View,

            CustomerPermissions.View,
            CustomerPermissions.Create,
            CustomerPermissions.Update,

            SupplierPermissions.View,

            CatalogPermissions.View,
            CatalogPermissions.Create,
            CatalogPermissions.Update,
            CatalogPermissions.ManagePrices,

            PurchasingPermissions.View,
            PurchasingPermissions.Approve,
            PurchasingPermissions.Receive,

            InventoryPermissions.View,
            InventoryPermissions.ViewCosts,
            InventoryPermissions.Transfer,
            InventoryPermissions.Adjust,
            InventoryPermissions.ViewMovements,

            SalesPermissions.View,
            SalesPermissions.Create,
            SalesPermissions.Cancel,
            SalesPermissions.ApplyDiscount,
            SalesPermissions.SellOnCredit,
            SalesPermissions.Return,

            CashPermissions.Open,
            CashPermissions.Close,
            CashPermissions.View,
            CashPermissions.CreateIncome,
            CashPermissions.CreateExpense,

            .. ReportPermissions.All
        ];
    }

    private static IReadOnlyCollection<string>
        GetCashierPermissions()
    {
        return
        [
            CustomerPermissions.View,
            CustomerPermissions.Create,
            CustomerPermissions.Update,

            CatalogPermissions.View,

            InventoryPermissions.View,

            SalesPermissions.View,
            SalesPermissions.Create,
            SalesPermissions.Return,

            CashPermissions.Open,
            CashPermissions.Close,
            CashPermissions.View
        ];
    }

    private static IReadOnlyCollection<string>
        GetInventoryPermissions()
    {
        return
        [
            CatalogPermissions.View,

            InventoryPermissions.View,
            InventoryPermissions.ViewCosts,
            InventoryPermissions.Transfer,
            InventoryPermissions.Adjust,
            InventoryPermissions.ViewMovements,

            ReportPermissions.Inventory
        ];
    }

    private static IReadOnlyCollection<string>
        GetPurchasingPermissions()
    {
        return
        [
            SupplierPermissions.View,
            SupplierPermissions.Create,
            SupplierPermissions.Update,

            CatalogPermissions.View,

            PurchasingPermissions.View,
            PurchasingPermissions.Create,
            PurchasingPermissions.Update,
            PurchasingPermissions.Approve,
            PurchasingPermissions.Receive,
            PurchasingPermissions.Cancel,

            InventoryPermissions.View,
            InventoryPermissions.ViewCosts,
            InventoryPermissions.ViewMovements,

            ReportPermissions.Purchases
        ];
    }

    private static void ThrowIfFailed(
        IdentityResult result,
        string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        string errors = string.Join(
            Environment.NewLine,
            result.Errors.Select(
                x => $"{x.Code}: {x.Description}"));

        throw new InvalidOperationException(
            $"No fue posible {operation}.{Environment.NewLine}" +
            errors);
    }
}