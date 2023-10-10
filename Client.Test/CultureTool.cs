using System.Globalization;
using PayrollEngine.Client.Model;

namespace PayrollEngine.Client.Test
{
    internal static class CultureTool
    {
        internal static CultureInfo GetTenantCulture(Tenant tenant)
        {
            var culture = CultureInfo.DefaultThreadCurrentCulture ?? CultureInfo.InvariantCulture;
            if (!string.IsNullOrWhiteSpace(tenant.Culture) &&
                !string.Equals(culture.Name, tenant.Culture))
            {
                culture = new CultureInfo(tenant.Culture);
            }
            return culture;
        }
    }
}
