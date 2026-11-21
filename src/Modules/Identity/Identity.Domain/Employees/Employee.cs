using BuildingBlocks.Domain.Bases;
using Identity.Domain.Parties;
using System.Diagnostics.CodeAnalysis;

namespace Identity.Domain.Employees
{
    public class Employee : AggregateRoot<PartyId>
    {
        public string JobTitle { get; private set; } = string.Empty;

        public Party Party { get; private set; } = null!;

        private Employee() { }

        [SetsRequiredMembers]
        public Employee(PartyId partyId, string jobTitle)
        {
            if (partyId == default)
                throw new ArgumentException("PartyId cannot be empty.", nameof(partyId));

            if (string.IsNullOrWhiteSpace(jobTitle))
                throw new ArgumentException("Job title cannot be empty.", nameof(jobTitle));

            // The Employee's identity IS the Party's identity (shared primary key).
            Id = partyId;
            JobTitle = jobTitle.Trim();
        }

        public static Employee Create(PartyId partyId, string jobTitle)
        {
            return new Employee(partyId, jobTitle);
        }

        public void UpdateJobTitle(string newJobTitle)
        {
            if (string.IsNullOrWhiteSpace(newJobTitle))
                throw new ArgumentException("Job title cannot be empty.", nameof(newJobTitle));

            JobTitle = newJobTitle.Trim();
        }
    }
}
