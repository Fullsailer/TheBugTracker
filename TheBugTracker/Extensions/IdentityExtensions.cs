using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;

namespace TheBugTracker.Extensions
{
    public static class IdentityExtensions
    {
        public static int? GetCompanyId(this IIdentity identity)
        {
            Claim claim = ((ClaimsIdentity)identity).FindFirst("CompanyId");
            //Ternary operator (if/else)
            return (claim != null) ? int.Parse(claim.Value) : null;

            //Above Ternary operator explanation

            //int result;
            //if (claim != null)
            //{
            //  result = int.Parse(claim.Value);
            //}
            //else
            //{
            //  result = 0;
            //}
            //return result;
        }
    }
}
