namespace LinkGuardiao.Application.Interfaces
{
    public interface ILinkAccessGrantService
    {
        string Generate(string shortCode);
        bool TryValidate(string shortCode, string accessGrant);
    }
}
