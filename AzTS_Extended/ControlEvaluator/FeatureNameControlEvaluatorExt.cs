namespace AzTS_Extended.ControlEvaluator
{
    using Microsoft.AzSK.ATS.Extensions.Authorization;
    using Microsoft.AzSK.ATS.Extensions.Models;
    using Microsoft.AzSK.ATS.ProcessSubscriptions.Models;
    using Microsoft.AzSK.ATS.ProcessSubscriptions.Processors.ControlProcessors.ServiceControlEvaluators;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    class SubscriptionCoreEvaluatorExt : SubscriptionCoreEvaluator
    {
        /// <summary>
        /// Checks the count of classic administrators at subscription scope.
        ///     .Passed
        ///         The count of classic administrators does not exceed 2.
        ///     .Failed
        ///         More than 2 classic administrators accounts found.
        ///     .Verify
        ///         RBAC result not found (sufficient data is not available for evaluation).
        /// </summary>
        /// <param name="cr">Control result object.</param>
        /// <returns name="ControlResult"> return evaluated control result.</returns>
        public ControlResult CheckCoAdminCount(ControlResult cr)
        {
            int noOfClassicAdminsLimit = cr.ControlDetails.ControlSettings?["NoOfClassicAdminsLimit"]?.Value<int>() ?? 2;
            string classicAdminAccountsString = "No classic admin accounts found.";
            int classicAdminAccountsCount = 0;

            // NOTE: While fetching RBAC result, we make three API calls - PIM, ARM, Classic. We are *not* handling partial result scenario if error occurred while fetching any of these RBAC result.
            // If no RBAC is found, mark status as Verify because sufficient data is not available for evaluation.
            if (this.RBACList?.Any() == false)
            {
                cr.VerificationResult = VerificationResultStatus.Verify;
                cr.StatusReason = "[Trial] No RBAC result found for this subscription.";
                cr.ConsiderForCompliance = false;
                return cr;
            }
            else
            {
                List<RBAC> classicAdminAccounts = new List<RBAC>();
                classicAdminAccounts = RBACList.AsParallel().Where(rbacItem => rbacItem.RoleName.ToLower().Contains("coadministrator") || rbacItem.RoleName.ToLower().Contains("serviceadministrator")).ToList();

                // First start with default value, override this if classic admin account is found.
                if (classicAdminAccounts != null && classicAdminAccounts.Any())
                {
                    classicAdminAccountsCount = classicAdminAccounts.Count;
                    classicAdminAccountsString = string.Join(",", classicAdminAccounts.Select(a => a.ToStringClassicAssignment()).ToList());
                }

                // Start with failed state, mark control as Passed if all required conditions are met
                cr.StatusReason = $"[Trial] No. of classic administrators found: [{classicAdminAccountsCount}]. Principal name results based on RBAC inv: [{String.Join(", ", classicAdminAccounts.Select(a => a.PrincipalName))}]";
                cr.VerificationResult = VerificationResultStatus.Failed;

                // Classic admin accounts count does not exceed the limit.
                if (classicAdminAccountsCount <= noOfClassicAdminsLimit)
                {
                    cr.VerificationResult = VerificationResultStatus.Passed;
                }
            }

            return cr;
        }

    }
}
