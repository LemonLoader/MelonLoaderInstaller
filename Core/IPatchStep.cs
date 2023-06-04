namespace MelonLoaderInstaller.Core
{
    internal interface IPatchStep
    {
        bool Run(Patcher patcher);
    }
}
