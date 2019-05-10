using Ninject;

namespace DeenGames.AliTheAndroid.Infrastructure.Common
{
    public static class DependencyInjection
    {
        public static IKernel kernel = new StandardKernel();
    }
}