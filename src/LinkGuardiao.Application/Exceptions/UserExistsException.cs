namespace LinkGuardiao.Application.Exceptions
{
    public class UserExistsException : Exception
    {
        public UserExistsException() : base("E-mail já cadastrado") { }
    }
}
