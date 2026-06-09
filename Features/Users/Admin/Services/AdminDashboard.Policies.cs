using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VenueBookingSystem.Shared;

namespace VenueBookingSystem.Features.Admin
{
    public partial class AdminDashboard
    {
        private async Task ShowPolicyManagementAsync()
        {
            bool back = false;
            while (!back)
            {
                Console.Clear();
                ConsoleHelper.PrintHeader("Manage Policies");

                string[] options =
                {
                    "View All Policies",
                    "Add New Policy",
                    "Edit Policy",
                    "Delete Policy"
                };

                ConsoleHelper.PrintMenu("Policy Options", options);
                int choice = ValidationHelper.ReadMenuChoice(4);

                switch (choice)
                {
                    case 0:
                        back = true;
                        break;
                    case 1:
                        await ViewAllPoliciesAsync();
                        break;
                    case 2:
                        await AddNewPolicyAsync();
                        break;
                    case 3:
                        await EditPolicyDirectAsync();
                        break;
                    case 4:
                        await DeletePolicyDirectAsync();
                        break;
                }
            }
        }

        private async Task ViewAllPoliciesAsync()
        {
            Console.Clear();
            ConsoleHelper.PrintHeader("All System Policies");
            var policies = await PolicyService.GetPoliciesFromDbAsync();
            if (policies.Count == 0)
            {
                ConsoleHelper.PrintWarning("No policies found in the database.");
            }
            else
            {
                int selection = ConsoleHelper.ShowPaginatedTable(policies, PolicyService.TableColumns, "System Policies");
                if (selection > 0)
                {
                    var policy = policies[selection - 1];
                    Console.Clear();
                    ConsoleHelper.PrintHeader($"Policy Details — #{policy.PolicyId}");
                    ConsoleHelper.PrintNonEditableField("Policy Type", policy.PolicyType);
                    ConsoleHelper.PrintNonEditableField("Sequence", policy.SequenceOrder.ToString());
                    ConsoleHelper.PrintNonEditableField("Policy Rule", policy.PolicyRule);
                    ConsoleHelper.PressAnyKey();
                }
            }
        }

        private async Task AddNewPolicyAsync()
        {
            Console.Clear();
            ConsoleHelper.PrintHeader("Add New Policy");

            ConsoleHelper.PrintInfo("Select Policy Type:");
            string[] types = { "Booking", "Refund", "Cancellation", "Create Custom Type" };
            ConsoleHelper.PrintMenu("Policy Type", types, showBack: true);
            int choice = ValidationHelper.ReadMenuChoice(4);
            if (choice == 0) return;

            string type;
            if (choice == 4)
            {
                type = ConsoleHelper.ReadInput("Custom Policy Type Name", "Enter custom type (e.g. General, Security)", required: true);
                if (type == ConsoleHelper.BACK_COMMAND || string.IsNullOrWhiteSpace(type)) return;
            }
            else
            {
                type = types[choice - 1];
            }

            string rule = ConsoleHelper.ReadInput("Policy Rule Text", "Enter the rule text", required: true);
            if (rule == ConsoleHelper.BACK_COMMAND) return;

            int seq = ValidationHelper.ReadInt("Sequence Order", 1, 100, "1");
            if (seq == int.MinValue) return;

            if (await PolicyService.AddPolicyAsync(type, rule, seq))
            {
                ConsoleHelper.PrintSuccess("Policy added successfully.");
            }
            else
            {
                ConsoleHelper.PrintError("Failed to add policy.");
            }
            ConsoleHelper.PressAnyKey();
        }

        private async Task EditPolicyDirectAsync()
        {
            Console.Clear();
            ConsoleHelper.PrintHeader("Edit Policy");

            var policies = await PolicyService.GetPoliciesFromDbAsync();
            if (policies.Count == 0)
            {
                ConsoleHelper.PrintWarning("No policies available to edit.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            int selection = ConsoleHelper.ShowPaginatedTable(policies, PolicyService.TableColumns, "Select Policy to Edit");
            if (selection == 0) return;

            var policy = policies[selection - 1];

            Console.Clear();
            ConsoleHelper.PrintHeader($"Edit Policy — #{policy.PolicyId}");
            ConsoleHelper.PrintNonEditableField("Type", policy.PolicyType);
            ConsoleHelper.PrintNonEditableField("Current Rule", policy.PolicyRule);
            ConsoleHelper.PrintNonEditableField("Current Sequence", policy.SequenceOrder.ToString());
            Console.WriteLine();

            string newRule = ConsoleHelper.ReadInput("New Policy Rule Text", "Enter new text (press Enter to keep current)", required: false);
            if (newRule == ConsoleHelper.BACK_COMMAND) return;
            if (string.IsNullOrWhiteSpace(newRule)) newRule = policy.PolicyRule;

            int newSeq = ValidationHelper.ReadInt("New Sequence Order", 1, 100, policy.SequenceOrder.ToString());
            if (newSeq == int.MinValue) return;

            if (await PolicyService.EditPolicyAsync(policy.PolicyId, newRule, newSeq))
            {
                ConsoleHelper.PrintSuccess("Policy updated successfully.");
            }
            else
            {
                ConsoleHelper.PrintError("Failed to update policy.");
            }
            ConsoleHelper.PressAnyKey();
        }

        private async Task DeletePolicyDirectAsync()
        {
            Console.Clear();
            ConsoleHelper.PrintHeader("Delete Policy");

            var policies = await PolicyService.GetPoliciesFromDbAsync();
            if (policies.Count == 0)
            {
                ConsoleHelper.PrintWarning("No policies available to delete.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            int selection = ConsoleHelper.ShowPaginatedTable(policies, PolicyService.TableColumns, "Select Policy to Delete");
            if (selection == 0) return;

            var policy = policies[selection - 1];

            Console.Clear();
            ConsoleHelper.PrintHeader($"Delete Policy — #{policy.PolicyId}");
            ConsoleHelper.PrintNonEditableField("Type", policy.PolicyType);
            ConsoleHelper.PrintNonEditableField("Rule", policy.PolicyRule);
            Console.WriteLine();

            if (ConsoleHelper.Confirm($"Are you sure you want to permanently delete policy #{policy.PolicyId}?"))
            {
                if (await PolicyService.DeletePolicyAsync(policy.PolicyId))
                {
                    ConsoleHelper.PrintSuccess("Policy deleted successfully.");
                }
                else
                {
                    ConsoleHelper.PrintError("Failed to delete policy.");
                }
                ConsoleHelper.PressAnyKey();
            }
        }
    }
}
