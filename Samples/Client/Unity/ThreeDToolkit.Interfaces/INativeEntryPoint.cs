using ThreeDToolkit.Interfaces;

namespace ThreeDToolkit.Interfaces
{
    public interface INativeEntryPoint
    {
        ISignaller GetSignaller();

        IConductor GetConductor();
    }
}
