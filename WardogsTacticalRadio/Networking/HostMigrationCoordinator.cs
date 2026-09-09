using WardogsTacticalRadio.Models;

namespace WardogsTacticalRadio.Networking;

public static class HostMigrationCoordinator
{
    // Alpha1 establishes deterministic election rules. Automatic network handoff
    // will be wired into this coordinator in the host-migration milestone.
    public static RadioPeer? ElectReplacementHost(IEnumerable<RadioPeer> peers, Guid failedHostPeerId)
    {
        return peers
            .Where(p => p.PeerId != failedHostPeerId && p.HostEligible)
            .OrderByDescending(p => p.LastHeartbeatUtc)
            .ThenBy(p => p.PeerId)
            .FirstOrDefault();
    }
}
