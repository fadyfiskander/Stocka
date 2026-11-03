namespace Organization.Domain.Organizations
{
    public readonly record struct OrganizationId(Guid Value)
    {
        public static OrganizationId New() => new(Guid.NewGuid());

        public static OrganizationId From(Guid value) => new(value);

        public static OrganizationId From(string value) => new(Guid.Parse(value));

        public override string ToString() => Value.ToString();
    }
}
