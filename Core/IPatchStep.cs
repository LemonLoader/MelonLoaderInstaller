namespace MelonLoader.Installer.Core
{
    internal interface IPatchStep
    {
        bool Run(Patcher patcher);
    }
}
