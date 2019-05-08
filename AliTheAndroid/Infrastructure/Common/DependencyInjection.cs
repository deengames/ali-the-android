using Ninject;

namespace DeenGames.AliTheAndroid.Infrastructure.Common
{
    public static class DependencyInjection
    {
        public static readonly IKernel kernel = new StandardKernel();
    }
}