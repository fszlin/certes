namespace Certes.Acme
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Certes.Acme.IContextFactory" />
    public class ContextFactory : IContextFactory
    {
        /// <summary>
        /// Creates the account context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public virtual IAccountContext CreateAccountContext(IAcmeContext context)
        {
            return new AccountContext(context);
        }
    }
}
