using Unity.Netcode;
namespace OctopusSurvivors
{
    public sealed partial class GameSession
    {
        // Explicit developer-only trigger. Production contact checks remain strict.
        public void RequestDevSpecialConvergence()
        {
            if(!IsSpawned)return;
            if(IsServer)DevSpecialConvergence();else DevSpecialConvergenceRpc();
        }
        [Rpc(SendTo.Server, InvokePermission=RpcInvokePermission.Everyone)]
        void DevSpecialConvergenceRpc(RpcParams rpc=default)
        {
            foreach(var player in OctopusPlayer.All)
                if(player!=null && player.IsSpawned && player.OwnerClientId==rpc.Receive.SenderClientId)
                { CastSpecialConvergence(player.Pos);return; }
        }
        void DevSpecialConvergence()
        {
            var player=OctopusPlayer.Local;
            if(player!=null && player.IsSpawned)CastSpecialConvergence(player.Pos);
        }
    }
}
